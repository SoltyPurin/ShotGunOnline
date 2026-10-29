using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 開始ムービー前の同期を担当するクラス (SRP)。MP_GameSyncManager と同じ NetworkObject に付ける。
/// ホスト: プレイヤー生成 → 枠の対応表を作成して配布 → 全員の準備完了（全プレイヤーの出現）を待つ → 完了を通知
/// クライアント: 対応表を受け取り、全プレイヤーが自端末に出現したらホストへ報告
/// </summary>
public class MP_OpeningSyncManager : NetworkBehaviour
{
    public static MP_OpeningSyncManager Instance { get; private set; }

    [Tooltip("ホストが全クライアントの準備完了を待つ最大時間（秒）。超えた場合は揃った分だけで開始します")]
    [SerializeField] private float _readyTimeoutSeconds = 10f;

    [Tooltip("クライアントが全プレイヤーの出現を待つ最大時間（秒）。超えた場合はそのまま準備完了を報告します")]
    [SerializeField] private float _clientSpawnWaitTimeoutSeconds = 5f;

    private PlayerSlotAssignment _currentAssignment;
    private readonly HashSet<ulong> _pendingClients = new();
    private Action _onAllReady;
    private Coroutine _serverTimeoutCoroutine;
    private Coroutine _clientWaitCoroutine;
    private bool _openingBegun;

    /// <summary>ホストから受け取った枠の対応表があるか</summary>
    public bool HasAssignment => _currentAssignment.IsValid;

    /// <summary>ホストから受け取った枠の対応表</summary>
    public PlayerSlotAssignment CurrentAssignment => _currentAssignment;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// [ホストのみ] プレイヤーを生成し、全クライアントの準備が整ったら onAllReady を呼ぶ
    /// </summary>
    public void BeginOpeningOnServer(Action onAllReady)
    {
        if (!IsServer) return;

        if (_openingBegun)
        {
            Debug.Log("[MP_OpeningSyncManager] 開始シーケンスは既に開始済みのためスキップします。");
            return;
        }
        _openingBegun = true;
        _onAllReady = onAllReady;

        // ムービー前にプレイヤーを生成（StageSpawnPoints の位置。ムービー開始時に Timeline の先頭位置へ移動する）
        if (MP_GameManager.Instance != null)
        {
            MP_GameManager.Instance.SpawnAllPlayers();
        }
        else
        {
            Debug.LogWarning("[MP_OpeningSyncManager] MP_GameManager が見つからないため、プレイヤーを生成できません。");
        }

        var assignment = PlayerSlotAssignment.CreateSorted(NetworkManager.ConnectedClientsIds);
        Debug.Log($"[MP_OpeningSyncManager] {assignment} を配布し、全員の準備完了を待ちます。");

        _pendingClients.Clear();
        foreach (var clientId in assignment.ClientIdsBySlot)
        {
            _pendingClients.Add(clientId);
        }

        _serverTimeoutCoroutine = StartCoroutine(ServerReadyTimeoutRoutine());
        PrepareOpeningRpc(assignment);
    }

    /// <summary>
    /// [全クライアント] 対応表を受け取り、全プレイヤーの出現を待ってから準備完了を報告する
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void PrepareOpeningRpc(PlayerSlotAssignment assignment)
    {
        _currentAssignment = assignment;

        if (_clientWaitCoroutine != null)
        {
            StopCoroutine(_clientWaitCoroutine);
        }
        _clientWaitCoroutine = StartCoroutine(WaitForPlayersThenReportRoutine(assignment));
    }

    [Rpc(SendTo.Server)]
    private void ReportReadyRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (_pendingClients.Remove(senderId))
        {
            Debug.Log($"[MP_OpeningSyncManager] ClientId {senderId} の準備が完了しました（残り {_pendingClients.Count} 人）。");
        }
        TryCompleteReady();
    }

    private IEnumerator WaitForPlayersThenReportRoutine(PlayerSlotAssignment assignment)
    {
        float elapsed = 0f;
        while (!AreAllPlayersSpawnedLocally(assignment))
        {
            if (elapsed >= _clientSpawnWaitTimeoutSeconds)
            {
                Debug.LogWarning("[MP_OpeningSyncManager] 全プレイヤーの出現を待ちきれなかったため、準備完了を報告します。");
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _clientWaitCoroutine = null;
        ReportReadyRpc();
    }

    private bool AreAllPlayersSpawnedLocally(PlayerSlotAssignment assignment)
    {
        var spawned = NetworkPlayerSpawner.CollectSpawnedPlayerObjects(NetworkManager);
        foreach (var clientId in assignment.ClientIdsBySlot)
        {
            if (!spawned.Exists(p => p.OwnerClientId == clientId))
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator ServerReadyTimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(_readyTimeoutSeconds);

        _serverTimeoutCoroutine = null;
        if (_pendingClients.Count > 0)
        {
            Debug.LogWarning($"[MP_OpeningSyncManager] 準備完了待ちがタイムアウトしました。未完了: [{string.Join(", ", _pendingClients)}]");
            _pendingClients.Clear();
            TryCompleteReady();
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (_pendingClients.Remove(clientId))
        {
            Debug.Log($"[MP_OpeningSyncManager] 準備待ち中に ClientId {clientId} が切断されました。");
            TryCompleteReady();
        }
    }

    private void TryCompleteReady()
    {
        if (_pendingClients.Count > 0 || _onAllReady == null) return;

        if (_serverTimeoutCoroutine != null)
        {
            StopCoroutine(_serverTimeoutCoroutine);
            _serverTimeoutCoroutine = null;
        }

        Debug.Log("[MP_OpeningSyncManager] 全員の準備が完了しました。開始ムービーを再生します。");
        var callback = _onAllReady;
        _onAllReady = null;
        callback.Invoke();
    }
}
