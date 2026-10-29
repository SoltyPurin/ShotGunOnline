using System;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// 開始ムービーにおける「枠番号 → clientId」の対応表。
/// ホストが作成し RPC で全クライアントへ配布することで、全端末で同じ枠に同じプレイヤーを割り当てる (SRP)
/// </summary>
public struct PlayerSlotAssignment : INetworkSerializable
{
    private ulong[] _clientIdsBySlot;

    /// <summary>参加プレイヤー数（= 使用する枠の数）</summary>
    public int PlayerCount => _clientIdsBySlot?.Length ?? 0;

    /// <summary>対応表が有効か（1人以上割り当てられているか）</summary>
    public bool IsValid => PlayerCount > 0;

    public IReadOnlyList<ulong> ClientIdsBySlot => _clientIdsBySlot ?? Array.Empty<ulong>();

    public PlayerSlotAssignment(ulong[] clientIdsBySlot)
    {
        _clientIdsBySlot = clientIdsBySlot;
    }

    /// <summary>
    /// clientId の昇順で枠番号を割り当てた対応表を作成する（どの端末で作っても同じ結果になる）
    /// </summary>
    public static PlayerSlotAssignment CreateSorted(IEnumerable<ulong> clientIds)
    {
        var ids = new List<ulong>(clientIds);
        ids.Sort();
        return new PlayerSlotAssignment(ids.ToArray());
    }

    /// <summary>
    /// 指定した clientId の枠番号を取得する。見つからない場合は -1 を返す
    /// </summary>
    public int GetSlotIndex(ulong clientId)
    {
        if (TryGetSlot(clientId, out int slot))
        {
            return slot;
        }
        return -1;
    }

    /// <summary>
    /// 指定した clientId の枠番号を取得する
    /// </summary>
    public bool TryGetSlot(ulong clientId, out int slot)
    {
        if (_clientIdsBySlot != null)
        {
            for (int i = 0; i < _clientIdsBySlot.Length; i++)
            {
                if (_clientIdsBySlot[i] == clientId)
                {
                    slot = i;
                    return true;
                }
            }
        }

        slot = -1;
        return false;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = PlayerCount;
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            _clientIdsBySlot = new ulong[length];
        }

        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref _clientIdsBySlot[i]);
        }
    }

    public override string ToString()
    {
        return $"PlayerSlotAssignment(Count={PlayerCount}, Slots=[{string.Join(", ", ClientIdsBySlot)}])";
    }
}
