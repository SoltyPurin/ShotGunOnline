using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームコース（通常モード・チュートリアル等）のステージ進行順序を管理するScriptableObject。
/// シーンを直接編集せずに、ステージプレハブの追加・並び替え・差し替えを行う。
/// </summary>
[CreateAssetMenu(fileName = "StageSequenceData", menuName = "ShotGunOnline/Stage Sequence Data")]
public class StageSequenceSO : ScriptableObject
{
    [Serializable]
    public class StageEntry
    {
        [Tooltip("ステージの管理名（インスペクター整理・デバッグ表示用）")]
        public string StageName = "New Stage";

        [Tooltip("地形・スポーン地点・ギミック・Wave設定を含むステージプレハブ（原点(0,0,0)基準）")]
        public StageController StagePrefab;

        [Tooltip("（任意）同じ地形プレハブを使い回して敵配置だけ別Waveプレハブに差し替えたい場合に指定。未指定ならStagePrefab内のWaveを使用します")]
        public Wave WavePrefabOverride;

        [Tooltip("（任意）ウェーブ開始時に表示するテキストの上書き。空欄ならWave側の設定を使用します")]
        public string WaveStartTextOverride;

        /// <summary>
        /// このステージで使用するWaveテンプレート（オーバーライド優先）を取得。
        /// </summary>
        public Wave ResolveWaveTemplate()
        {
            if (WavePrefabOverride != null)
            {
                return WavePrefabOverride;
            }

            return StagePrefab != null ? StagePrefab.StageWave : null;
        }
    }

    [SerializeField, Header("ステージ進行リスト（上から順番にプレイ）")]
    private List<StageEntry> _stages = new List<StageEntry>();

    [SerializeField, Header("全ステージクリア後の遷移先シーン名（空欄ならWaveManager側の設定を使用）")]
    private string _nextSceneNameOverride = "";

    public IReadOnlyList<StageEntry> Stages => _stages;
    public List<StageEntry> EditableStages => _stages;
    public string NextSceneNameOverride => _nextSceneNameOverride;
}
