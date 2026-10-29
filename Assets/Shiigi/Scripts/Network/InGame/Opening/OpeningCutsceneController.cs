using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ゲーム開始時の単一Timeline（PlayableDirector）の初期フレーム評価と時間同期再生を担当するクラス (SRP: パターンB)
/// ・PlayableDirectorは1本のみで動作します
/// ・再生時間（director.time）に合わせて、登録されたすべての実プレイヤー（1〜4人）の演出ポーズを同時に更新します
/// </summary>
public class OpeningCutsceneController
{
    private readonly PlayableDirector[] _startDirectors;
    private readonly List<CutsceneControlledPlayer> _cutscenePlayers = new();

    public OpeningCutsceneController(PlayableDirector[] startDirectors)
    {
        _startDirectors = startDirectors;
    }

    public OpeningCutsceneController(PlayableDirector director)
    {
        _startDirectors = director != null ? new[] { director } : Array.Empty<PlayableDirector>();
    }

    /// <summary>
    /// ムービー制御対象の実機プレイヤー一覧を設定する
    /// </summary>
    public void SetCutscenePlayers(IEnumerable<CutsceneControlledPlayer> players)
    {
        _cutscenePlayers.Clear();
        if (players != null)
        {
            _cutscenePlayers.AddRange(players);
        }
    }

    /// <summary>
    /// Play On Awake をOFFにした Timeline の初期状態を再生せずに反映する
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
    /// 単一の開始ムービーを再生し、再生時間に合わせて全プレイヤーのアニメーションを同期更新する
    /// </summary>
    public IEnumerator PlaySequenceRoutine(Action onComplete)
    {
        PlayableDirector mainDirector = null;
        if (_startDirectors != null && _startDirectors.Length > 0)
        {
            foreach (var d in _startDirectors)
            {
                if (d != null)
                {
                    mainDirector = d;
                    break;
                }
            }
        }

        if (mainDirector == null)
        {
            Debug.LogWarning("[OpeningCutsceneController] 再生対象の PlayableDirector が設定されていません。即時開始します。");
            TickPlayers(0f);
            onComplete?.Invoke();
            yield break;
        }

        // 初期ポーズ（倒れ状態）を全プレイヤーに即時適用
        TickPlayers(0f);

        // ムービー再生開始
        mainDirector.time = 0;
        mainDirector.Play();
        float duration = (float)mainDirector.duration;

        while (mainDirector != null && mainDirector.state == PlayState.Playing && mainDirector.time < duration)
        {
            TickPlayers((float)mainDirector.time);
            yield return null;
        }

        // 最終フレーム評価
        TickPlayers(duration);
        mainDirector.Stop();

        onComplete?.Invoke();
    }

    private void TickPlayers(float time)
    {
        for (int i = 0; i < _cutscenePlayers.Count; i++)
        {
            var player = _cutscenePlayers[i];
            if (player != null && player.IsInCutscene)
            {
                player.UpdateCutscenePose(time);
            }
        }
    }
}
