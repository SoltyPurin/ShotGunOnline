using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ゲーム開始時の演出を実行するスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class GameStarter : MonoBehaviour
{
    [SerializeField, Header("取得系")]
    private WaveManager _waveManager = default;
    [SerializeField]
    private PlayerStateManager _playerStateManager = default;
    [SerializeField]
    private GameObject _canvas = default;
    [SerializeField]
    private GameObject _StarterCanvas = default;

    [SerializeField, Header("タイムライン")]
    private PlayableDirector _playerDirector = default;

    private void GetPlayer()
    {
        // ネットワーク上の全プレイヤーオブジェクトを探す
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _playerStateManager.MovieState();
                _canvas.SetActive(false);

                break;
            }
        }

    }

    private void FixedUpdate()
    {
        if (_playerStateManager == null)
        {
            GetPlayer();
            return;
        }
    }
    /// <summary>
    /// TimeLine内で実行するメソッド、ムービー終了時ウェーブを開始する。
    /// </summary>
    public void TimelineEnd()
    {
        _canvas.SetActive(true);
        _StarterCanvas.SetActive(false);
        _waveManager.StartWave(0);
        _playerStateManager.NormalState();
    }
}
