using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ゲーム開始時のTimeline（PlayableDirector）の初期フレーム評価と再生待機のみを担当するクラス (SRP)
/// </summary>
public class OpeningCutsceneController
{
    private readonly PlayableDirector[] _startDirectors;

    public OpeningCutsceneController(PlayableDirector[] startDirectors)
    {
        _startDirectors = startDirectors;
    }

    /// <summary>
    /// Play On Awake をOFFにした Timeline の初期状態(ローディング表示など)を、再生せずに反映する
    /// </summary>
    public void EvaluateInitialState()
    {
        if (_startDirectors == null) return;

        foreach (var director in _startDirectors)
        {
            if (director == null) continue;
            director.time = 0;
            director.Evaluate();
        }
    }

    /// <summary>
    /// すべての開始ムービーを再生し、最長の再生時間が経過するまで待機してからコールバックを実行する
    /// </summary>
    public IEnumerator PlaySequenceRoutine(Action onComplete)
    {
        float maxDuration = 0f;
        if (_startDirectors != null && _startDirectors.Length > 0)
        {
            foreach (var director in _startDirectors)
            {
                if (director == null) continue;
                director.time = 0;
                director.Play();
                if (director.duration > maxDuration)
                {
                    maxDuration = (float)director.duration;
                }
            }
        }

        // ムービーの長さ分待機（ムービーがない場合は即座に進行）
        if (maxDuration > 0f)
        {
            yield return new WaitForSeconds(maxDuration);
        }

        onComplete?.Invoke();
    }
}

