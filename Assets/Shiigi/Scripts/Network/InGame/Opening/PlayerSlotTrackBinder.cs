using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline のプレイヤー枠トラック（"PlayerSlot0"〜）に、実際のプレイヤーの Animator を割り当てる責務クラス (SRP)。
/// 使わない枠は割り当てを外す（TimelineAsset 自体を書き換える track.muted は使用しない）。
/// </summary>
public class PlayerSlotTrackBinder
{
    public const string SlotTrackPrefix = "PlayerSlot";

    /// <summary>
    /// 枠番号に対応するトラック名を返す（例: 0 → "PlayerSlot0"）
    /// </summary>
    public static string GetSlotTrackName(int slot) => SlotTrackPrefix + slot;

    /// <summary>
    /// 対応表に従ってプレイヤーを枠トラックに割り当てる
    /// </summary>
    /// <returns>割り当てに成功したプレイヤー</returns>
    public List<CutsceneControlledPlayer> Bind(
        PlayableDirector director,
        PlayerSlotAssignment assignment,
        IReadOnlyList<NetworkObject> players)
    {
        var boundPlayers = new List<CutsceneControlledPlayer>();
        if (director == null) return boundPlayers;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null)
        {
            Debug.LogWarning($"[PlayerSlotTrackBinder] {director.name} に TimelineAsset が設定されていません。");
            return boundPlayers;
        }

        // 枠番号 → トラック の一覧を作成
        var slotTracks = new Dictionary<int, TrackAsset>();
        foreach (var track in timeline.GetOutputTracks())
        {
            if (TryParseSlot(track.name, out int slot))
            {
                slotTracks[slot] = track;
            }
        }

        // 一旦すべての枠の割り当てを外す（人数が少ない場合、使わない枠は何も動かさない）。
        // 編集時のプレビュー用にシーン上のダミーが割り当てられている場合は、ダミーを非表示にする
        foreach (var track in slotTracks.Values)
        {
            HidePreviewDummy(director.GetGenericBinding(track));
            director.SetGenericBinding(track, null);
        }

        if (players == null) return boundPlayers;

        foreach (var player in players)
        {
            if (player == null) continue;

            if (!assignment.TryGetSlot(player.OwnerClientId, out int slot))
            {
                Debug.LogWarning($"[PlayerSlotTrackBinder] ClientId {player.OwnerClientId} は対応表に含まれていません。");
                continue;
            }

            if (!slotTracks.TryGetValue(slot, out var slotTrack))
            {
                Debug.LogWarning($"[PlayerSlotTrackBinder] {director.name} にトラック \"{GetSlotTrackName(slot)}\" がありません。");
                continue;
            }

            var controlled = player.GetComponent<CutsceneControlledPlayer>();
            if (controlled == null)
            {
                Debug.LogWarning($"[PlayerSlotTrackBinder] {player.name} に CutsceneControlledPlayer が付いていません。");
                continue;
            }

            director.SetGenericBinding(slotTrack, controlled.CutsceneAnimator);
            boundPlayers.Add(controlled);
        }

        return boundPlayers;
    }

    private static void HidePreviewDummy(Object binding)
    {
        GameObject dummy = binding switch
        {
            Component component => component.gameObject,
            GameObject go => go,
            _ => null
        };

        // ネットワークのプレイヤー（前回割り当てたもの）は非表示にしない
        if (dummy == null || dummy.GetComponentInParent<NetworkObject>() != null) return;

        dummy.SetActive(false);
    }

    private static bool TryParseSlot(string trackName, out int slot)
    {
        slot = -1;
        if (string.IsNullOrEmpty(trackName) || !trackName.StartsWith(SlotTrackPrefix)) return false;
        return int.TryParse(trackName.Substring(SlotTrackPrefix.Length), out slot) && slot >= 0;
    }
}
