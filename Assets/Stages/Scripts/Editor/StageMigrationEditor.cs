using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 既存のメインシーン（MP_GameScene + MapData.prefab + 各Waveプレハブ）から
/// 個別 Stage Prefab + StageSequenceSO（ScriptableObject）方式へ一括移行するエディタツール。
/// 新規ステージテンプレートの作成機能も提供します。
/// </summary>
[InitializeOnLoad]
public static class StageMigrationEditor
{
    private const string StagesRootFolder = "Assets/Stages";
    private const string StagePrefabsFolder = "Assets/Stages/Prefabs";
    private const string StageDataFolder = "Assets/Stages/Data";
    private const string MainStageSequenceAssetPath = "Assets/Stages/Data/MainStageSequence.asset";

    private const string MpGameScenePath = "Assets/Shiigi/Scenes/MP_GameScene.unity";
    private const string MapDataPrefabPath = "Assets/Masaka/Prefab/MapData.prefab";
    private const string NavBuildManPrefabPath = "Assets/Masaka/Prefab/NavBuildMan.prefab";
    private const string SoulHealFloorPrefabPath = "Assets/A MAIN/Prefab/Gimmick/SoulHealFloor.prefab";

    private const string AutoPromptSessionKey = "ShotGunOnline_StageMigration_Prompted";

    private struct NamedLocalPoint
    {
        public string Name;
        public Vector3 LocalPosition;
        public bool IsBossReturnPoint;
    }

    private class StageBuildSpec
    {
        public string AssetFileName;
        public string DisplayName;
        public float OriginY;
        public string TerrainChildName;
        public string WavePrefabPath;
        public Vector3 InitialSpawnCenter;
        public Vector3[] RespawnPositions;
        public NamedLocalPoint[] SpecialPoints;
        public bool IncludeOasisManager;
        public bool AddToMainSequence = true;
    }

    static StageMigrationEditor()
    {
        EditorApplication.delayCall += TryPromptMigrationOnLoad;
    }

    private static void TryPromptMigrationOnLoad()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // 既に MainStageSequence.asset が存在するなら移行完了済みなので何もしない
        if (AssetDatabase.LoadAssetAtPath<StageSequenceSO>(MainStageSequenceAssetPath) != null)
        {
            return;
        }

        // 同一エディタセッション内で既にダイアログを出した場合は再表示しない
        if (SessionState.GetBool(AutoPromptSessionKey, false))
        {
            return;
        }
        SessionState.SetBool(AutoPromptSessionKey, true);

        if (!File.Exists(MapDataPrefabPath) || !File.Exists(MpGameScenePath))
        {
            return;
        }

        bool execute = EditorUtility.DisplayDialog(
            "ステージシステム移行ツール",
            "新ステージシステム（個別 Stage Prefab + ScriptableObject）のスクリプトが読み込まれました。\n\n" +
            "現在の MP_GameScene の全ステージ（Wave 1〜Boss）を Assets/Stages/ 配下の個別プレハブ＋StageSequenceデータへ自動移行しますか？\n" +
            "（元の MapData.prefab や Wave プレハブは削除せず保持されます）",
            "今すぐ移行を実行",
            "後でメニューから実行"
        );

        if (execute)
        {
            RunMigrationInteractive();
        }
    }

    [MenuItem("Tools/Stage System/現在のステージをPrefab + ScriptableObjectへ移行 (MP_GameScene)", priority = 10)]
    public static void RunMigrationInteractive()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("実行不可", "再生モードを終了してから実行してください。", "OK");
            return;
        }

        RunMigration();

        EditorUtility.DisplayDialog(
            "ステージ移行完了",
            "全ステージの個別プレハブ化と MP_GameScene の移行が完了しました！\n\n" +
            $"・ステージ進行データ: {MainStageSequenceAssetPath}\n" +
            $"・各ステージプレハブ: {StagePrefabsFolder}/\n" +
            $"・更新済みシーン: {MpGameScenePath}",
            "OK"
        );
    }

    [MenuItem("Tools/Stage System/新規ステージPrefabを作成...", priority = 20)]
    public static void CreateNewStageTemplateInteractive()
    {
        EnsureFoldersExist();

        string savePath = EditorUtility.SaveFilePanelInProject(
            "新規ステージPrefabの作成",
            "Stage_New",
            "prefab",
            "新しいステージPrefabの保存先を指定してください",
            StagePrefabsFolder
        );

        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        GameObject stageRoot = CreateBlankStageHierarchy(Path.GetFileNameWithoutExtension(savePath));
        try
        {
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(stageRoot, savePath);
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"[StageMigrationEditor] 新規ステージテンプレートを作成しました: {savePath}");
        }
        finally
        {
            Object.DestroyImmediate(stageRoot);
        }
    }

    /// <summary>
    /// バッチ実行およびメニュー実行共通のステージ一括移行処理。
    /// </summary>
    public static void RunMigration()
    {
        EnsureFoldersExist();

        GameObject mapDataAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MapDataPrefabPath);
        if (mapDataAsset == null)
        {
            Debug.LogError($"[StageMigrationEditor] {MapDataPrefabPath} が見つかりません。");
            return;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene mpScene = SceneManager.GetSceneByPath(MpGameScenePath);
        bool openedMpAdditively = false;

        if (!mpScene.IsValid() || !mpScene.isLoaded)
        {
            mpScene = EditorSceneManager.OpenScene(MpGameScenePath, OpenSceneMode.Additive);
            openedMpAdditively = true;
        }

        SceneManager.SetActiveScene(mpScene);

        try
        {
            List<StageBuildSpec> specs = GetCurrentStageSpecs();
            var sequenceEntries = new List<StageSequenceSO.StageEntry>();

            for (int i = 0; i < specs.Count; i++)
            {
                StageBuildSpec spec = specs[i];
                EditorUtility.DisplayProgressBar(
                    "ステージプレハブ生成中",
                    $"{spec.DisplayName} ({i + 1}/{specs.Count}) を出力しています...",
                    (float)i / specs.Count
                );

                StageController savedStagePrefab = BuildAndSaveStagePrefab(spec, mapDataAsset, mpScene);
                if (savedStagePrefab != null && spec.AddToMainSequence)
                {
                    sequenceEntries.Add(new StageSequenceSO.StageEntry
                    {
                        StageName = spec.DisplayName,
                        StagePrefab = savedStagePrefab,
                        WavePrefabOverride = null,
                        WaveStartTextOverride = string.Empty
                    });
                }
            }

            // StageSequenceSO アセットを作成または更新
            StageSequenceSO sequenceAsset = CreateOrUpdateSequenceAsset(sequenceEntries);

            // MP_GameScene を更新して保存
            UpdateMpGameScene(mpScene, sequenceAsset);
            EditorSceneManager.MarkSceneDirty(mpScene);
            EditorSceneManager.SaveScene(mpScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StageMigrationEditor] ステージ移行が完了しました。メインシーケンス登録数: {sequenceEntries.Count}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            if (openedMpAdditively && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
                EditorSceneManager.CloseScene(mpScene, true);
            }
        }
    }

    private static List<StageBuildSpec> GetCurrentStageSpecs()
    {
        return new List<StageBuildSpec>
        {
            // 1. Wave 1 (originY = 0)
            new StageBuildSpec
            {
                AssetFileName = "Stage_01_Wave1",
                DisplayName = "Stage 01 - Wave 1",
                OriginY = 0f,
                TerrainChildName = "Map1",
                WavePrefabPath = "Assets/A MAIN/Waves/TestWave1mmmm.prefab",
                InitialSpawnCenter = new Vector3(-18f, 0f, 0f),
                RespawnPositions = new[] { new Vector3(-24.3f, 0f, 0f) },
                AddToMainSequence = true
            },
            // 2. Wave 2 (originY = 100)
            new StageBuildSpec
            {
                AssetFileName = "Stage_02_Wave2",
                DisplayName = "Stage 02 - Wave 2",
                OriginY = 100f,
                TerrainChildName = "Map2",
                WavePrefabPath = "Assets/A MAIN/Waves/TestWave2mmmm.prefab",
                InitialSpawnCenter = new Vector3(-41.3f, -4.2f, 0f),
                RespawnPositions = new[] { new Vector3(-41.3f, -4.2f, 0f) },
                AddToMainSequence = true
            },
            // 3. Wave 3 - Medium Boss (originY = 300)
            new StageBuildSpec
            {
                AssetFileName = "Stage_03_MediumBoss",
                DisplayName = "Stage 03 - Medium Boss",
                OriginY = 300f,
                TerrainChildName = "Map4",
                WavePrefabPath = "Assets/A MAIN/Waves/MediumTestWave4mmmm.prefab",
                InitialSpawnCenter = new Vector3(-11.85f, -2f, 0f),
                RespawnPositions = new[] { new Vector3(-11.85f, -2f, 0f) },
                SpecialPoints = new[]
                {
                    new NamedLocalPoint { Name = "MediumSpawnPoint", LocalPosition = new Vector3(-1f, -2.1f, 0f), IsBossReturnPoint = false }
                },
                AddToMainSequence = true
            },
            // 4. Wave 4 (originY = 400)
            new StageBuildSpec
            {
                AssetFileName = "Stage_04_Wave4",
                DisplayName = "Stage 04 - Wave 4",
                OriginY = 400f,
                TerrainChildName = "Map5",
                WavePrefabPath = "Assets/A MAIN/Waves/TestWave5mmmm.prefab",
                InitialSpawnCenter = new Vector3(0f, -10.01f, 0f),
                RespawnPositions = new[] { new Vector3(0f, -10.01f, 0f) },
                AddToMainSequence = true
            },
            // 5. Wave 5 (originY = 500)
            new StageBuildSpec
            {
                AssetFileName = "Stage_05_Wave5",
                DisplayName = "Stage 05 - Wave 5",
                OriginY = 500f,
                TerrainChildName = "Map6",
                WavePrefabPath = "Assets/A MAIN/Waves/TestWave6mmmm.prefab",
                InitialSpawnCenter = new Vector3(0f, 0f, 0f),
                RespawnPositions = new[] { new Vector3(0f, 0f, 0f) },
                AddToMainSequence = true
            },
            // 6. Wave 6 - Boss (originY = 655.9)
            new StageBuildSpec
            {
                AssetFileName = "Stage_06_Boss",
                DisplayName = "Stage 06 - Boss",
                OriginY = 655.9f,
                TerrainChildName = "Map7",
                WavePrefabPath = "Assets/A MAIN/Waves/NetworkBoss.prefab",
                InitialSpawnCenter = new Vector3(0f, -58f, 0f),
                RespawnPositions = new[]
                {
                    new Vector3(0f, -58f, 0f),    // Sevens (ボス通路入口)
                    new Vector3(0f, -15.82f, 0f)  // Eights (ボスアリーナ内)
                },
                SpecialPoints = new[]
                {
                    new NamedLocalPoint { Name = "SpawonPoint2", LocalPosition = new Vector3(0.05f, 0.04f, 0f), IsBossReturnPoint = true }
                },
                IncludeOasisManager = true,
                AddToMainSequence = true
            },
            // 予備ステージアーカイブ: Map3 / TestWave3mmmm (originY = 200)
            new StageBuildSpec
            {
                AssetFileName = "Stage_Extra_Wave3",
                DisplayName = "Stage Extra - Wave 3 (Archived)",
                OriginY = 200f,
                TerrainChildName = "Map3",
                WavePrefabPath = "Assets/A MAIN/Waves/TestWave3mmmm.prefab",
                InitialSpawnCenter = new Vector3(-35.8f, 1.7f, 0f),
                RespawnPositions = new[] { new Vector3(-35.8f, 1.7f, 0f) },
                AddToMainSequence = false
            }
        };
    }

    private static StageController BuildAndSaveStagePrefab(StageBuildSpec spec, GameObject mapDataAsset, Scene workScene)
    {
        GameObject stageRoot = new GameObject(spec.AssetFileName);
        SceneManager.MoveGameObjectToScene(stageRoot, workScene);
        stageRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        StageController stageController = stageRoot.AddComponent<StageController>();

        // --- 1. Terrain (Grid + Tilemaps + Lights + Torches) 抽出 ---
        GameObject tempMapData = (GameObject)PrefabUtility.InstantiatePrefab(mapDataAsset, workScene);
        PrefabUtility.UnpackPrefabInstance(tempMapData, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

        Grid srcGrid = tempMapData.GetComponentInChildren<Grid>(true);
        Grid dstGrid = null;

        if (srcGrid != null)
        {
            GameObject srcGridGo = srcGrid.gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(srcGridGo))
            {
                GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(srcGridGo);
                if (outermost != null)
                {
                    PrefabUtility.UnpackPrefabInstance(outermost, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                }
            }

            GameObject terrainGo = new GameObject("Terrain");
            terrainGo.transform.SetParent(stageRoot.transform, false);
            terrainGo.transform.localPosition = srcGridGo.transform.localPosition;
            terrainGo.transform.localRotation = srcGridGo.transform.localRotation;
            terrainGo.transform.localScale = srcGridGo.transform.localScale;

            dstGrid = terrainGo.AddComponent<Grid>();
            dstGrid.cellSize = srcGrid.cellSize;
            dstGrid.cellGap = srcGrid.cellGap;
            dstGrid.cellLayout = srcGrid.cellLayout;
            dstGrid.cellSwizzle = srcGrid.cellSwizzle;

            // 子オブジェクト配列のスナップショットから該当マップを移動し、OriginY を差し引いて原点基準に正規化
            var gridChildren = new List<Transform>();
            for (int c = 0; c < srcGridGo.transform.childCount; c++)
            {
                gridChildren.Add(srcGridGo.transform.GetChild(c));
            }

            foreach (Transform child in gridChildren)
            {
                if (child == null || child.name != spec.TerrainChildName) continue;

                Vector3 origLocalPos = child.localPosition;
                Quaternion origLocalRot = child.localRotation;
                Vector3 origLocalScale = child.localScale;

                child.SetParent(terrainGo.transform, false);
                child.localPosition = origLocalPos - new Vector3(0f, spec.OriginY, 0f);
                child.localRotation = origLocalRot;
                child.localScale = origLocalScale;
                child.gameObject.SetActive(true);

                // MP_GameScene 側でオーバーライドされていた FallToCliff._groundLayer = TileMap (1 << 3 = 8) を反映
                foreach (FallToCliff fallToCliff in child.GetComponentsInChildren<FallToCliff>(true))
                {
                    SerializedObject ftcSo = new SerializedObject(fallToCliff);
                    SerializedProperty glProp = ftcSo.FindProperty("_groundLayer");
                    if (glProp != null && glProp.intValue == 0)
                    {
                        glProp.intValue = 1 << 3;
                        ftcSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        Object.DestroyImmediate(tempMapData);

        // --- 2. SpawnPoints (InitialPlayerSpawns / RespawnPoints / SpecialPoints) ---
        GameObject spawnGroup = new GameObject("SpawnPoints");
        spawnGroup.transform.SetParent(stageRoot.transform, false);

        StageSpawnPoints initialSpawns = CreateInitialPlayerSpawns(spawnGroup.transform, spec.InitialSpawnCenter);

        GameObject respawnGroup = new GameObject("RespawnPoints");
        respawnGroup.transform.SetParent(spawnGroup.transform, false);
        var respawnList = new List<Transform>();
        if (spec.RespawnPositions != null)
        {
            for (int r = 0; r < spec.RespawnPositions.Length; r++)
            {
                GameObject rGo = new GameObject($"Respawn_{r}");
                rGo.transform.SetParent(respawnGroup.transform, false);
                rGo.transform.localPosition = spec.RespawnPositions[r];
                respawnList.Add(rGo.transform);
            }
        }

        Transform bossReturnTransform = null;
        if (spec.SpecialPoints != null && spec.SpecialPoints.Length > 0)
        {
            GameObject specialGroup = new GameObject("SpecialPoints");
            specialGroup.transform.SetParent(spawnGroup.transform, false);
            foreach (var sp in spec.SpecialPoints)
            {
                GameObject spGo = new GameObject(sp.Name);
                spGo.transform.SetParent(specialGroup.transform, false);
                spGo.transform.localPosition = sp.LocalPosition;
                if (sp.IsBossReturnPoint)
                {
                    bossReturnTransform = spGo.transform;
                }
            }
        }

        // --- 3. StageGimmicks (Bossステージの OasisManager 等) ---
        if (spec.IncludeOasisManager)
        {
            GameObject gimmicksGroup = new GameObject("StageGimmicks");
            gimmicksGroup.transform.SetParent(stageRoot.transform, false);

            GameObject oasisGo = new GameObject("OasisManager");
            oasisGo.transform.SetParent(gimmicksGroup.transform, false);
            oasisGo.transform.localPosition = new Vector3(0f, 0.1f, -0.029694997f);

            FloorOasis floorOasis = oasisGo.AddComponent<FloorOasis>();
            SerializedObject oasisSo = new SerializedObject(floorOasis);
            oasisSo.FindProperty("_destroyTime").floatValue = 30f;
            oasisSo.FindProperty("_floorSpawnValue").intValue = 4;
            oasisSo.FindProperty("_useRelativeOffset").boolValue = true;
            oasisSo.FindProperty("_soulFloorObject").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(SoulHealFloorPrefabPath);

            SerializedProperty posArray = oasisSo.FindProperty("_floorSpawnPos");
            posArray.arraySize = 4;
            posArray.GetArrayElementAtIndex(0).vector2Value = new Vector2(-10f, 10f);
            posArray.GetArrayElementAtIndex(1).vector2Value = new Vector2(-10f, -10f);
            posArray.GetArrayElementAtIndex(2).vector2Value = new Vector2(10f, -10f);
            posArray.GetArrayElementAtIndex(3).vector2Value = new Vector2(10f, 10f);
            oasisSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- 4. Wave（敵配置・ウェーブギミック・中ボスムービーUI）統合 ---
        Wave stageWave = null;
        GameObject wavePrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.WavePrefabPath);
        if (wavePrefabAsset != null)
        {
            GameObject waveInstance = (GameObject)PrefabUtility.InstantiatePrefab(wavePrefabAsset, workScene);
            PrefabUtility.UnpackPrefabInstance(waveInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            Vector3 waveOrigPos = waveInstance.transform.localPosition;
            waveInstance.name = "Wave";
            waveInstance.transform.SetParent(stageRoot.transform, false);
            waveInstance.transform.localPosition = waveOrigPos - new Vector3(0f, spec.OriginY, 0f);
            waveInstance.transform.localRotation = Quaternion.identity;
            waveInstance.transform.localScale = Vector3.one;
            waveInstance.SetActive(true);

            // ダミー配置上の不要な NetworkObject 等を除去し、Prefab Mode で安全に編集可能にする
            StageControllerEditor.StripNestedNetcodeComponents(waveInstance);

            stageWave = waveInstance.GetComponent<Wave>();

            // Wave内に BossReturnPoint があれば自動的にリンク（中ボス用）
            if (bossReturnTransform == null)
            {
                Transform innerReturn = waveInstance.transform.Find("BossReturnPoint");
                if (innerReturn != null)
                {
                    bossReturnTransform = innerReturn;
                }
            }
        }

        // --- 5. StageController のシリアライズ参照を配線 ---
        SerializedObject stageSo = new SerializedObject(stageController);
        stageSo.FindProperty("_terrainGrid").objectReferenceValue = dstGrid;
        stageSo.FindProperty("_initialPlayerSpawns").objectReferenceValue = initialSpawns;
        stageSo.FindProperty("_stageWave").objectReferenceValue = stageWave;
        stageSo.FindProperty("_bossReturnPoint").objectReferenceValue = bossReturnTransform;

        SerializedProperty respawnsProp = stageSo.FindProperty("_respawnPoints");
        respawnsProp.arraySize = respawnList.Count;
        for (int i = 0; i < respawnList.Count; i++)
        {
            respawnsProp.GetArrayElementAtIndex(i).objectReferenceValue = respawnList[i];
        }
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        // --- 6. Prefab アセットとして保存 ---
        string saveAssetPath = $"{StagePrefabsFolder}/{spec.AssetFileName}.prefab";
        bool savedSuccess;
        GameObject savedGo = PrefabUtility.SaveAsPrefabAsset(stageRoot, saveAssetPath, out savedSuccess);
        Object.DestroyImmediate(stageRoot);

        if (!savedSuccess || savedGo == null)
        {
            Debug.LogError($"[StageMigrationEditor] ステージプレハブの保存に失敗しました: {saveAssetPath}");
            return null;
        }

        return savedGo.GetComponent<StageController>();
    }

    private static StageSpawnPoints CreateInitialPlayerSpawns(Transform parent, Vector3 centerLocalPos)
    {
        GameObject go = new GameObject("InitialPlayerSpawns");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centerLocalPos;

        Vector3[] offsets =
        {
            new Vector3(0f, 1.5f, 0f),
            new Vector3(-1.5f, 0f, 0f),
            new Vector3(1.5f, 0f, 0f),
            new Vector3(0f, -1.5f, 0f)
        };

        var pointTransforms = new Transform[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject p = new GameObject($"p{i + 1}");
            p.transform.SetParent(go.transform, false);
            p.transform.localPosition = offsets[i];
            pointTransforms[i] = p.transform;
        }

        StageSpawnPoints spawnPoints = go.AddComponent<StageSpawnPoints>();
        SerializedObject so = new SerializedObject(spawnPoints);
        SerializedProperty arr = so.FindProperty("_spawnPoints");
        arr.arraySize = pointTransforms.Length;
        for (int i = 0; i < pointTransforms.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = pointTransforms[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return spawnPoints;
    }

    private static StageSequenceSO CreateOrUpdateSequenceAsset(List<StageSequenceSO.StageEntry> entries)
    {
        StageSequenceSO asset = AssetDatabase.LoadAssetAtPath<StageSequenceSO>(MainStageSequenceAssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<StageSequenceSO>();
            AssetDatabase.CreateAsset(asset, MainStageSequenceAssetPath);
        }

        asset.EditableStages.Clear();
        asset.EditableStages.AddRange(entries);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void UpdateMpGameScene(Scene mpScene, StageSequenceSO sequenceAsset)
    {
        // 1. WaveManager に MainStageSequence.asset を設定
        WaveManager waveManager = null;
        foreach (GameObject root in mpScene.GetRootGameObjects())
        {
            waveManager = root.GetComponentInChildren<WaveManager>(true);
            if (waveManager != null) break;
        }

        if (waveManager != null)
        {
            SerializedObject wmSo = new SerializedObject(waveManager);
            wmSo.FindProperty("_stageSequence").objectReferenceValue = sequenceAsset;
            wmSo.FindProperty("_waveList").ClearArray();
            wmSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waveManager);
        }
        else
        {
            Debug.LogWarning("[StageMigrationEditor] MP_GameScene 内に WaveManager が見つかりませんでした。");
        }

        // 2. NavBuildMan（2D NavMesh ビルダー）がシーン直下に無ければ配置（旧 MapData 内にあったため）
        bool hasStandaloneNavBuilder = false;
        foreach (GameObject root in mpScene.GetRootGameObjects())
        {
            if (root.name == "MapData") continue;
            if (root.GetComponentInChildren<NavMeshBuilder2D>(true) != null)
            {
                hasStandaloneNavBuilder = true;
                break;
            }
        }

        if (!hasStandaloneNavBuilder)
        {
            GameObject navPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NavBuildManPrefabPath);
            if (navPrefab != null)
            {
                GameObject navInst = (GameObject)PrefabUtility.InstantiatePrefab(navPrefab, mpScene);
                navInst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                GameObject navObj = new GameObject("NavBuildMan");
                SceneManager.MoveGameObjectToScene(navObj, mpScene);
                NavMeshBuilder2D builder = navObj.AddComponent<NavMeshBuilder2D>();
                builder.bakeOnEnable = true;
                builder.updateMethod = NavMeshBuilder2D.UpdateMethod.Manual;
            }
        }

        // 3. シーン直下にベタ置きされていた旧ステージオブジェクト群を除去（FogEffectFrontは原点用の1つだけ残す）
        var namesToRemove = new HashSet<string>
        {
            "MapData",
            "MP_PlayerSpawnPoints",
            "SpawonPoint2",
            "MediumSpawnPoint",
            "OasisManager",
            "FogEffectFront2"
        };

        foreach (GameObject root in mpScene.GetRootGameObjects())
        {
            if (root == null) continue;
            if (namesToRemove.Contains(root.name))
            {
                Object.DestroyImmediate(root);
            }
        }

        // 4. MapWarpPoint (PlayerRespawn) 本体は残しつつ、レガシー座標リストをクリアして新ステージシステムへ委譲
        foreach (GameObject root in mpScene.GetRootGameObjects())
        {
            if (root == null) continue;
            PlayerRespawn respawn = root.GetComponentInChildren<PlayerRespawn>(true);
            if (respawn != null)
            {
                SerializedObject prSo = new SerializedObject(respawn);
                prSo.FindProperty("_respawnPoints").ClearArray();
                prSo.ApplyModifiedPropertiesWithoutUndo();

                // MapWarpPoint 直下の旧ワープポイント子オブジェクト（First〜Eights）を非表示・整理
                for (int i = respawn.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = respawn.transform.GetChild(i);
                    if (PrefabUtility.IsAddedGameObjectOverride(child.gameObject))
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                EditorUtility.SetDirty(respawn);
                break;
            }
        }
    }

    private static GameObject CreateBlankStageHierarchy(string stageName)
    {
        GameObject stageRoot = new GameObject(stageName);
        StageController controller = stageRoot.AddComponent<StageController>();

        // Terrain (Grid + Ground / Wall / Hole Tilemaps)
        GameObject terrainGo = new GameObject("Terrain");
        terrainGo.transform.SetParent(stageRoot.transform, false);
        Grid grid = terrainGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject mapContainer = new GameObject("Map");
        mapContainer.transform.SetParent(terrainGo.transform, false);
        mapContainer.layer = 3;

        // Ground Tilemap (NavMesh歩行面)
        GameObject groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(mapContainer.transform, false);
        groundGo.layer = 3;
        groundGo.AddComponent<Tilemap>();
        TilemapRenderer groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = -2;
        Rigidbody2D groundRb = groundGo.AddComponent<Rigidbody2D>();
        groundRb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D groundTileCol = groundGo.AddComponent<TilemapCollider2D>();
        groundTileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        CompositeCollider2D groundCompCol = groundGo.AddComponent<CompositeCollider2D>();
        groundCompCol.geometryType = CompositeCollider2D.GeometryType.Polygons;
        groundCompCol.isTrigger = true;
        FallToCliff groundFall = groundGo.AddComponent<FallToCliff>();
        SerializedObject ftcSo = new SerializedObject(groundFall);
        ftcSo.FindProperty("_groundLayer").intValue = 1 << 3;
        ftcSo.ApplyModifiedPropertiesWithoutUndo();
        NavMeshSourceTag2D groundNav = groundGo.AddComponent<NavMeshSourceTag2D>();
        groundNav.area = 0;

        // Wall Tilemap (壁・障害物)
        GameObject wallGo = new GameObject("Wall");
        wallGo.transform.SetParent(mapContainer.transform, false);
        wallGo.tag = "Wall";
        wallGo.AddComponent<Tilemap>();
        TilemapRenderer wallRenderer = wallGo.AddComponent<TilemapRenderer>();
        wallRenderer.sortingOrder = 0;
        Rigidbody2D wallRb = wallGo.AddComponent<Rigidbody2D>();
        wallRb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D wallTileCol = wallGo.AddComponent<TilemapCollider2D>();
        wallTileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        wallGo.AddComponent<CompositeCollider2D>();

        // Hole / OutOfStage Tilemap (NavMesh非歩行エリア)
        GameObject holeGo = new GameObject("OutOfStage");
        holeGo.transform.SetParent(mapContainer.transform, false);
        holeGo.AddComponent<Tilemap>();
        TilemapRenderer holeRenderer = holeGo.AddComponent<TilemapRenderer>();
        holeRenderer.sortingOrder = -3;
        Rigidbody2D holeRb = holeGo.AddComponent<Rigidbody2D>();
        holeRb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D holeTileCol = holeGo.AddComponent<TilemapCollider2D>();
        holeTileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        CompositeCollider2D holeCompCol = holeGo.AddComponent<CompositeCollider2D>();
        holeCompCol.geometryType = CompositeCollider2D.GeometryType.Polygons;
        holeCompCol.isTrigger = true;
        NavMeshSourceTag2D holeNav = holeGo.AddComponent<NavMeshSourceTag2D>();
        holeNav.area = 1; // Not Walkable

        // SpawnPoints
        GameObject spawnGroup = new GameObject("SpawnPoints");
        spawnGroup.transform.SetParent(stageRoot.transform, false);
        StageSpawnPoints initialSpawns = CreateInitialPlayerSpawns(spawnGroup.transform, new Vector3(-10f, 0f, 0f));

        GameObject respawnGroup = new GameObject("RespawnPoints");
        respawnGroup.transform.SetParent(spawnGroup.transform, false);
        GameObject r0 = new GameObject("Respawn_0");
        r0.transform.SetParent(respawnGroup.transform, false);
        r0.transform.localPosition = new Vector3(-10f, 0f, 0f);

        // Wave
        GameObject waveGo = new GameObject("Wave");
        waveGo.transform.SetParent(stageRoot.transform, false);
        Wave wave = waveGo.AddComponent<Wave>();

        SerializedObject stageSo = new SerializedObject(controller);
        stageSo.FindProperty("_terrainGrid").objectReferenceValue = grid;
        stageSo.FindProperty("_initialPlayerSpawns").objectReferenceValue = initialSpawns;
        stageSo.FindProperty("_stageWave").objectReferenceValue = wave;
        SerializedProperty respawnsProp = stageSo.FindProperty("_respawnPoints");
        respawnsProp.arraySize = 1;
        respawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = r0.transform;
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        return stageRoot;
    }

    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder(StagesRootFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Stages");
        }
        if (!AssetDatabase.IsValidFolder(StagePrefabsFolder))
        {
            AssetDatabase.CreateFolder(StagesRootFolder, "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(StageDataFolder))
        {
            AssetDatabase.CreateFolder(StagesRootFolder, "Data");
        }
    }
}
