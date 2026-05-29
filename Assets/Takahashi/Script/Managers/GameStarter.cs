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

    private void Start()
    {
        //ゲーム開始時プレイヤーを動かなくし、UIを非表示にする。
        _playerStateManager.MovieState();
        _canvas.SetActive(false);

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
