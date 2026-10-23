using Unity.Netcode;
using UnityEngine;

/// <summary>
/// タイトルシーンの初期化およびマルチプレイセッション・ネットワーク状態のクリーンアップを担当するクラス。
/// </summary>
public class TitleManager : MonoBehaviour
{
    private void Start()
    {
        // ポーズ画面などから遷移した際に時間停止状態が残らないよう復帰
        Time.timeScale = 1.0f;

        CleanupNetworkSession();
        InitializeScreen();
    }

    /// <summary>
    /// タイトル画面遷移時に残存しているルームセッションやNetworkManagerがあれば切断・破棄を要求する
    /// </summary>
    private void CleanupNetworkSession()
    {
        if (MultiplayerSessionManager.Instance != null)
        {
            _ = MultiplayerSessionManager.Instance.DisconnectAndCleanupAsync();
        }
        else if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }

    /// <summary>
    /// 解像度を設定
    /// </summary>
    private void InitializeScreen()
    {
        Screen.SetResolution(1920, 1080, true);
    }
}
