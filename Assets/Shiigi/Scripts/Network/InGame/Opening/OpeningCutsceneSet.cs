using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// プレイヤー人数に応じて再生する開始ムービー（PlayableDirector）を選ぶコンポーネント (SRP)。
/// Timeline を1本だけ使う場合はエントリを1つ登録すれば、全人数でそれが使われる。
/// </summary>
public class OpeningCutsceneSet : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("この Timeline を使うプレイヤー人数")]
        [Min(1)] public int playerCount;

        [Tooltip("プレイヤー枠トラック（PlayerSlot0〜）を含む PlayableDirector。Play On Awake は OFF にしてください")]
        public PlayableDirector director;
    }

    [SerializeField] private Entry[] _entries;

    /// <summary>
    /// 人数に合う Director を返す。
    /// 人数が一致するものが無ければ「人数以上で最小」のもの、それも無ければ人数が最大のものを返す。
    /// </summary>
    public PlayableDirector Select(int playerCount)
    {
        if (_entries == null || _entries.Length == 0) return null;

        PlayableDirector exact = null;
        PlayableDirector smallestAbove = null;
        int smallestAboveCount = int.MaxValue;
        PlayableDirector largest = null;
        int largestCount = int.MinValue;

        foreach (var entry in _entries)
        {
            if (entry.director == null) continue;

            if (entry.playerCount == playerCount)
            {
                exact = entry.director;
                break;
            }

            if (entry.playerCount > playerCount && entry.playerCount < smallestAboveCount)
            {
                smallestAbove = entry.director;
                smallestAboveCount = entry.playerCount;
            }

            if (entry.playerCount > largestCount)
            {
                largest = entry.director;
                largestCount = entry.playerCount;
            }
        }

        return exact ?? smallestAbove ?? largest;
    }
}
