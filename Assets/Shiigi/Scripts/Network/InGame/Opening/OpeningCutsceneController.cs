using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 開始ムービーの Timeline 再生と全プレイヤーの演出同期を制御する。
/// </summary>
public class OpeningCutsceneController
{
    private readonly PlayableDirector[] _startDirectors;
    private readonly List<CutsceneControlledPlayer> _cutscenePlayers = new();

    public OpeningCutsceneController(PlayableDirector[] startDirectors)
    {
        _startDirectors = startDirectors;
    }

    /// <summary>
    /// 出現中の実機プレイヤーを収集してスロットを割り当て、ムービー制御状態へ移行する
    /// </summary>
    public void PreparePlayers(NetworkManager networkManager, MP_OpeningSyncManager openingSync)
    {
        _cutscenePlayers.Clear();

        var players = NetworkPlayerSpawner.CollectSpawnedPlayerObjects(networkManager);
        if (players.Count == 0)
        {
            Debug.LogWarning("[OpeningCutsceneController] 割り当てるプレイヤーが見つかりません。");
            return;
        }

        PlayerSlotAssignment assignment;
        if (openingSync != null && openingSync.HasAssignment)
        {
            assignment = openingSync.CurrentAssignment;
        }
        else
        {
            var clientIds = new ulong[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                clientIds[i] = players[i].OwnerClientId;
            }
            assignment = PlayerSlotAssignment.CreateSorted(clientIds);
        }

        Debug.Log($"[OpeningCutsceneController] 開始ムービーに {players.Count} 人のプレイヤーを同期します。{assignment}");

        for (int i = 0; i < players.Count; i++)
        {
            var netObj = players[i];
            if (netObj == null) continue;

            if (!netObj.TryGetComponent<CutsceneControlledPlayer>(out var cutsceneComp))
            {
                cutsceneComp = netObj.gameObject.AddComponent<CutsceneControlledPlayer>();
            }

            if (!assignment.TryGetSlot(netObj.OwnerClientId, out int slot))
            {
                slot = i;
            }

            cutsceneComp.EnterCutscene(slot);
            _cutscenePlayers.Add(cutsceneComp);
        }
    }

    /// <summary>
    /// 開始ムービー終了時、登録済みプレイヤーの制御を解放して通常操作へ移行する
    /// </summary>
    public void ReleasePlayers()
    {
        for (int i = 0; i < _cutscenePlayers.Count; i++)
        {
            if (_cutscenePlayers[i] != null)
            {
                _cutscenePlayers[i].ExitCutscene();
            }
        }
        _cutscenePlayers.Clear();
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
        if (mainDirector != null)
        {
            mainDirector.Stop();
        }

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
