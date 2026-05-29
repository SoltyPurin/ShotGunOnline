using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ScoreCircle : MonoBehaviour
{
    #region 変数
    [SerializeField, Header("回転体のイメージ")]
    private Image _progressRing = default;
    [SerializeField,Header("待機時間")]
    private float _waitTime = 2.0f;
    [SerializeField,Header("終了時間のディレイ")]
    public float _finishDelay = 1.0f;

    /// ゲージの上がり幅、1が最大で0が最低
    [SerializeField,Header("上がり幅")]
    private float _rankGage = 1;

    private bool progress;

    public Action OnFinish;

    #endregion

    private void Start()
    {
    }
    public void Initialize(float maxValue,float totalScore)
    {
        _rankGage = totalScore/maxValue;
        CancelTimer();
        StartTimer();
    }

    async Task Update()
    {
        if (progress)
        {
            _progressRing.fillAmount += 1 / _waitTime * Time.deltaTime;
            if (_rankGage <= _progressRing.fillAmount)
            {
                progress = false;
                await Task.Delay((int)(_finishDelay * 1000));
                //_progressRing.fillAmount = 0;
                OnFinish?.Invoke();
            }
        }
    }

    public void StartTimer()
    {
        progress = true;
    }

    public void CancelTimer()
    {
        _progressRing.fillAmount = 0;
        progress = false;
    }
}
