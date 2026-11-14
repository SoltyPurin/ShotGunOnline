using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// エディタ単体実行時、ロビーを経由せずに即座にゲームシーンをテストするための自動起動スクリプト
/// </summary>
public class DebugGameAutoStarter : MonoBehaviour
{
    [Header("デバッグ設定")]
    [Tooltip("エディタ再生時に自動でローカルホスト起動するか")]
    [SerializeField] private bool _autoStartInEditor = true;

#if UNITY_EDITOR
    private void Start()
    {
        if (!_autoStartInEditor) return;

        // すでにロビーやタイトル等から接続済みの場合は何もしない（通常プレイを阻害しない）
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[DebugGameAutoStarter] シーン内に NetworkManager が見つかりません！");
            return;
        }

        // ローカルループバック(127.0.0.1)に通信設定を初期化
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777);
        }

        Debug.Log("[DebugGameAutoStarter] エディタ単体再生を検知。ローカルHostとして即座にゲームを開始します。");

        // ローカルHostとしてサーバー兼クライアントを起動
        NetworkManager.Singleton.StartHost();

        // MP_GameSyncManager が存在する場合はフェーズ変数経由で整合性を保ったまま開始する
        if (MP_GameSyncManager.Instance != null && MP_GameSyncManager.Instance.IsServer)
        {
            MP_GameSyncManager.Instance.CurrentPhase.Value = MP_GameSyncManager.GamePhase.Playing;
        }
        else if (MP_GameManager.Instance != null)
        {
            MP_GameManager.Instance.OnGameStart();
        }
        else
        {
            Debug.LogWarning("[DebugGameAutoStarter] MP_GameManager が見つかりませんでした。");
        }
    }
#endif
}
