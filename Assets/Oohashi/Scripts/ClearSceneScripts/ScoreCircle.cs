using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCircle : MonoBehaviour
{
    #region 変数
    [SerializeField, Header("回転体のイメージ")]
    private Image _progressRing = default;
    [SerializeField, Header("待機時間")]
    private float _waitTime = 2.0f;
    [SerializeField, Header("終了時間のディレイ")]
    public float _finishDelay = 1.0f;

    /// ゲージの上がり幅、1が最大で0が最低
    [SerializeField, Header("上がり幅")]
    private float _rankGage = 1;

    private bool progress;
    private Coroutine _delayCoroutine;

    public Action OnFinish;

    #endregion

    private void Start()
    {
    }

    public void Initialize(float maxValue, float totalScore)
    {
        if (maxValue <= 0)
        {
            _rankGage = 0;
        }
        else
        {
            _rankGage = Mathf.Clamp01(totalScore / maxValue);
        }

        CancelTimer();
        StartTimer();
    }

    private void Update()
    {
        if (progress && _progressRing != null)
        {
            float step = _waitTime > 0 ? (1f / _waitTime) * Time.deltaTime : 1f;
            _progressRing.fillAmount += step;

            if (_progressRing.fillAmount >= _rankGage)
            {
                progress = false;
                _progressRing.fillAmount = _rankGage;

                if (_delayCoroutine != null)
                {
                    StopCoroutine(_delayCoroutine);
                }
                _delayCoroutine = StartCoroutine(DelayFinishCoroutine());
            }
        }
    }

    private IEnumerator DelayFinishCoroutine()
    {
        if (_finishDelay > 0)
        {
            yield return new WaitForSeconds(_finishDelay);
        }
        _delayCoroutine = null;
        OnFinish?.Invoke();
    }

    public void StartTimer()
    {
        progress = true;
    }

    public void CancelTimer()
    {
        if (_delayCoroutine != null)
        {
            StopCoroutine(_delayCoroutine);
            _delayCoroutine = null;
        }

        if (_progressRing != null)
        {
            _progressRing.fillAmount = 0;
        }
        progress = false;
    }

    private void OnDisable()
    {
        CancelTimer();
    }
}
