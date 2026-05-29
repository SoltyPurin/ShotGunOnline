using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveTransitionAnim : MonoBehaviour
{
    #region 【変数】
    [SerializeField, Header("ウェーブの数を取得するためのスクリプト")]
    private WaveManager _waveManager = default;

    [SerializeField, Header("ウェーブ遷移のステートを監視するスクリプト")]
    private WaveTransition _waveTransition = default;

    [SerializeField, Header("アニメーター")]
    private Animator _animator = default;

    [SerializeField, Header("マップのオブジェクト")]
    private GameObject _mapObj = default;

    [SerializeField, Header("垂れ幕")]
    private GameObject _blackLine = default;

    [SerializeField, Header("暗闇のイメージ")]
    private GameObject _blackImage = default;

    [SerializeField, Header("カメラ")]
    private GameObject _camera = default;

    [SerializeField,Header("プレイヤーのステート管理")]
    private PlayerStateManager _stateManager = default;
    [SerializeField]
    private InputChangeState _inputChangeState = default;

    //一回のウェーブ遷移で一回だけコルーチンを走らせる
    private bool _isTransitionRunning = false;

    // 立ち上がり検出用
    private bool _wasWaveChangeMoment = default;

    private Coroutine _runningRoutine;    // 実行中ハンドル

    private bool _checkOnce = false;

    #endregion

    #region【定数】
    private readonly string FIRST = "First";

    private readonly string SECOND = "Second";

    private readonly string THIRD = "Third";

    private readonly string FOURTH = "Fourth";

    private readonly string FIFTH = "Fifth";

    private readonly string SIXTH = "Sixth";
    #endregion

    private void FixedUpdate()
    {
        //this.transform.position = Vector2.zero;
        bool now = _waveTransition.IsWaveChangeMoment;
        if (!_wasWaveChangeMoment && now)
        {
            int idx = _waveManager.WaveIndex;
            TransitionAnim(idx);
        }

        if (_waveTransition.EndTransition() && !_checkOnce)
        {
            _checkOnce = true;
            DeActiveBalls();
        }

            _wasWaveChangeMoment = now;
    }

    /// <summary>
    /// 白玉達を非表示にする
    /// </summary>
    private void DeActiveBalls()
    {
        _mapObj.SetActive(false);
        _blackImage.SetActive(false);
        _blackLine.SetActive(false);
    }

    private void TransitionAnim(int newIndex)
    {
        if (_isTransitionRunning)
        {
            return;
        }
        _stateManager.MovieState();

        _runningRoutine = StartCoroutine(TriggerReset(newIndex));

        }

    private IEnumerator TriggerReset(int curIndex)
    {
        _isTransitionRunning = true;

        yield return new WaitForSeconds(0.5f);

        _blackImage.SetActive(true);
        _mapObj.SetActive(true);
        _blackLine.SetActive(true);

        try
        {
            yield return null;
            switch(curIndex)
            {
                case 1:
                    _animator.SetTrigger(FIRST);
                    break;

                case 2:
                    _animator.SetTrigger(SECOND);
                    break;

                case 3:
                    _animator.SetTrigger(THIRD);
                    break;

                case 4:
                    _animator.SetTrigger(FOURTH);
                    break;

                case 5:
                    _animator.SetTrigger(FIFTH);
                    break;

                case 6:
                    _animator.SetTrigger(SIXTH);
                    break;

            }
        }
        finally
        {
            _isTransitionRunning = false;
            _runningRoutine = null;
        }

        _checkOnce = false;

        StartCoroutine(PlayerCanMove());
    }

    private IEnumerator PlayerCanMove()
    {
        yield return new WaitForSeconds(2.5f);
        _stateManager.NormalState();
        _inputChangeState.ToNormalAnimation();
    }
}
