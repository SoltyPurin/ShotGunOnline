using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// StageController のカスタムインスペクターと、Prefab保存時の自動クリーンアップ処理。
/// 敵プレハブをそのままドラッグ＆ドロップで配置しても競合やNGOエラーが起きないように対策
/// </summary>
[CustomEditor(typeof(StageController))]
public class StageControllerEditor : Editor
{
    private const string NgoCheckForNetworkObjectPrefKey = "NetworkBehaviour-Check-For-NetworkObject";
    private const string NgoAutoAddNetworkObjectPrefKey = "AutoAdd-NetworkObject-When-None-Exist";

    [InitializeOnLoadMethod]
    private static void RegisterPrefabSavingHook()
    {
        SuppressNgoNetworkObjectPopup();

        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;

        PrefabStage.prefabSaving -= OnStagePrefabSaving;
        PrefabStage.prefabSaving += OnStagePrefabSaving;
    }

    /// <summary>
    /// NGO の NetworkBehaviourEditor がダミー敵選択時に毎回出す
    /// 「NetworkBehaviours require a NetworkObject」ポップアップを自動で抑止します。
    /// （Unity の仕様上、ダイアログのチェックボックスは Yes 押下時しか保存されないため EditorPrefs 側を直接 OFF にします）
    /// </summary>
    private static void SuppressNgoNetworkObjectPopup()
    {
        if (EditorPrefs.GetBool(NgoCheckForNetworkObjectPrefKey, true))
        {
            EditorPrefs.SetBool(NgoCheckForNetworkObjectPrefKey, false);
        }
        if (EditorPrefs.GetBool(NgoAutoAddNetworkObjectPrefKey, false))
        {
            EditorPrefs.SetBool(NgoAutoAddNetworkObjectPrefKey, false);
        }
    }

    private static void OnPrefabStageOpened(PrefabStage stage)
    {
        SuppressNgoNetworkObjectPopup();
    }

    private static void OnStagePrefabSaving(GameObject prefabContentsRoot)
    {
        if (prefabContentsRoot == null) return;

        StageController stage = prefabContentsRoot.GetComponent<StageController>();
        if (stage == null) return;

        stage.AutoResolveReferencesIfNeeded();

        // Wave配下のダミー敵などに NetworkObject 等が残っていると NGO のネスト警告が出るため自動除去
        if (stage.StageWave != null)
        {
            int removed = StripNestedNetcodeComponents(stage.StageWave.gameObject);
            if (removed > 0)
            {
                Debug.Log($"[StageControllerEditor] '{prefabContentsRoot.name}' 保存時にWave配下の不要なNetcodeコンポーネント {removed} 個を自動整理しました。");
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StageController stage = (StageController)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ステージ編集ユーティリティ", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "1. 地形は Terrain (Grid) 配下の Tilemap で編集します（原点 (0,0,0) 基準）。\n" +
            "2. 敵やウェーブギミックは Wave 配下にプレハブを配置し、下のボタンでリストへ自動登録できます。\n" +
            "3. リスポーン位置は SpawnPoints/RespawnPoints で調整できます。",
            MessageType.Info
        );

        if (GUILayout.Button("参照の自動リンク ＆ Wave内の敵・ギミックを自動収集", GUILayout.Height(28)))
        {
            Undo.RecordObject(stage, "Auto Link Stage References");
            stage.AutoResolveReferencesIfNeeded();

            if (stage.StageWave != null)
            {
                Undo.RecordObject(stage.StageWave, "Collect Wave Children");
                stage.StageWave.CollectChildrenToLists();
                StripNestedNetcodeComponents(stage.StageWave.gameObject);
                EditorUtility.SetDirty(stage.StageWave);
            }

            EditorUtility.SetDirty(stage);
            Debug.Log($"[StageControllerEditor] '{stage.name}' の参照リンクとWaveリストを更新しました。");
        }
    }

    /// <summary>
    /// Wave配下に配置されたダミー敵などから NetworkTransform / NetworkRigidbody2D / NetworkObjectを除去
    /// </summary>
    public static int StripNestedNetcodeComponents(GameObject root)
    {
        if (root == null || EditorUtility.IsPersistent(root)) return 0;
        int removedCount = 0;

        foreach (var netRb in root.GetComponentsInChildren<NetworkRigidbody2D>(true))
        {
            if (netRb == null) continue;
            SafeDestroyComponent(netRb);
            removedCount++;
        }

        foreach (var netAnim in root.GetComponentsInChildren<NetworkAnimator>(true))
        {
            if (netAnim == null) continue;
            SafeDestroyComponent(netAnim);
            removedCount++;
        }

        foreach (var netTrans in root.GetComponentsInChildren<NetworkTransform>(true))
        {
            if (netTrans == null) continue;
            SafeDestroyComponent(netTrans);
            removedCount++;
        }

        foreach (var netObj in root.GetComponentsInChildren<NetworkObject>(true))
        {
            if (netObj == null) continue;
            SafeDestroyComponent(netObj);
            removedCount++;
        }

        return removedCount;
    }

    private static void SafeDestroyComponent(Component comp)
    {
        if (comp == null || EditorUtility.IsPersistent(comp)) return;
        GameObject go = comp.gameObject;
        Object.DestroyImmediate(comp);

        // 万が一 PrefabInstance 制約で残った場合は1段階Unpackしてから除去
        if (comp != null && PrefabUtility.IsPartOfPrefabInstance(go))
        {
            GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (instanceRoot != null)
            {
                PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(comp);
            }
        }
    }
}
