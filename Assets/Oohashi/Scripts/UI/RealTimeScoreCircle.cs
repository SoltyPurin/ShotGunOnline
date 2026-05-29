using DG.Tweening;
//using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RealTimeScoreCircle : MonoBehaviour
{
    private enum RankState
    {
        S,
        A,
        B,
    }
    [SerializeField, Header("回転体のイメージ")]
    private Image _progressRing = default;
    [SerializeField, Header("終了時間のディレイ")]
    public float _finishDelay = 1.0f;
    [SerializeField, Header("ランク表示のテキスト")]
    private Text _rankText = default;
    [SerializeField, Header("時間生成のスクリプト")]
    private EnemyCountAndCreateTime _createTime = default;

    private float _decSpeed = 1;

    private RankState _rankState = default;

    /// ゲージの上がり幅、1が最大で0が最低
    [SerializeField, Header("上がり幅")]
    private float _rankGage = 1;

    private bool progress;

    public Action OnFinish;

    private float _sRankTime = 0;
    private float _aRankTime = 0;

    private float _initDamageRank = 0;

    private float _initTimeRank = 3;

    private float _penalty = 0;

    public void SetValue(float sRankRatio, float aRankRatio, int difficulty, float penalty)
    {
        _rankText.text = "S";
        _sRankTime = sRankRatio;
        _aRankTime = aRankRatio - sRankRatio;
        _decSpeed = _sRankTime;
        _penalty = penalty;
        _initTimeRank = 3;
        _rankState = RankState.S;
        CancelTimer();
        StartTimer();
    }


    private void FixedUpdate()
    {
        if (!progress) return;

        float currentTime = _createTime.InitWaveTime;

        switch (_rankState)
        {
            case RankState.S:
                _progressRing.fillAmount = Mathf.Clamp01(1f - (currentTime / _sRankTime));
                if (currentTime >= _sRankTime)
                {
                    HandleRankTransition();
                }
                break;

            case RankState.A:
                _progressRing.fillAmount = Mathf.Clamp01(1f - ((currentTime - _sRankTime) / _aRankTime));
                if (currentTime >= _sRankTime + _aRankTime)
                {
                    HandleRankTransition();
                }
                break;

            case RankState.B:
                _progressRing.fillAmount = 0f;
                break;
        }
    }

    private async void HandleRankTransition()
    {
        switch (_rankState)
        {
            case RankState.S:
                _initTimeRank--;
                _progressRing.fillAmount = 1;
                _rankState = RankState.A;
                _decSpeed = _aRankTime;
                UpdateRankByScore((int)_initTimeRank, (int)_initDamageRank);
                await Task.Delay((int)(_finishDelay * 1000));
                break;

            case RankState.A:
                _progressRing.fillAmount = 0;
                _initTimeRank--;
                _rankState = RankState.B;
                _decSpeed = 1;
                UpdateRankByScore((int)_initTimeRank, (int)_initDamageRank);
                await Task.Delay((int)(_finishDelay * 1000));
                progress = false;
                OnFinish?.Invoke();
                break;
        }
    }

    public void UpdateRankByScore(int timeRank, int damageRank)
    {
        int totalScore = timeRank + damageRank;
        if (totalScore >= 3)
        {
            _rankText.text = "S";
        }
        else if (totalScore >= 2)
        {
            _rankText.text = "A";
        }
        else
        {
            _rankText.text = "B";
        }
    }

    public void StartTimer()
    {
        progress = true;
    }

    public void CancelTimer()
    {
        _progressRing.fillAmount = 1;
        progress = false;
    }

    public void Trenble()
    {
        transform.DOShakePosition(duration: 1,
        strength: 50,
        vibrato: 50,
        randomness: 90,
        snapping: false,
        fadeOut: true);
    }
}
