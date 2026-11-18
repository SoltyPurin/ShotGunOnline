using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// 各クライアントが自機の移動権限を持つようにする(Unity公式サンプル)
/// URL: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/5294b15d98f934ba4cca11d3d269d9d74229ab99/com.unity.netcode.gameobjects/Components/NetworkTransform.cs#L3182
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override void Awake()
    {
        // パケットロス時のHead-of-Lineブロッキング（移動カクつき）を防ぐためUnreliableデルタ同期を有効化
        UseUnreliableDeltas = true;

        // ルートNetworkObject以外のネストされた子オブジェクト（銃身Burrel等）に付与されている場合、
        // PlayerAimingとの回転競合・ワールド座標での位置ズレ・二重パケット送信を防止する
        if (GetComponent<Unity.Netcode.NetworkObject>() == null)
        {
            InLocalSpace = true;
            SyncPositionX = false;
            SyncPositionY = false;
            SyncPositionZ = false;
            SyncRotAngleX = false;
            SyncRotAngleY = false;
            SyncRotAngleZ = false;
            SyncScaleX = false;
            SyncScaleY = false;
            SyncScaleZ = false;
        }

        base.Awake();
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
