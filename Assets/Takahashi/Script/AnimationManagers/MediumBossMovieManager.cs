using UnityEngine;
using UnityEngine.Playables;

public class MediumBossMovieManager : MonoBehaviour
{
    [Header("プレイアブルディレクター")]
    [SerializeField, Tooltip("中ボスムービー")]
    private PlayableDirector _playableDirector = default;

    [Header("中ボス")]
    [SerializeField, Tooltip("中ボスのステート管理")]
    private EnemyMove _enemyMove = default;

    //メインのキャンバスを取得
    private Animator _mainCanvasAnim = default;

    private void Start()
    {
        GameObject mainCanvas = GameObject.FindWithTag("MainCanvas");
        _mainCanvasAnim = mainCanvas.GetComponent<Animator>();

        _mainCanvasAnim.SetTrigger("Hide");
        _playableDirector.Play();
    }

    public void ShowCanvas()
    {
        _mainCanvasAnim.SetTrigger("Show");
    }
}
