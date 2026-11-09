using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// マルチプレイ集会所（ロビー）用の2DステージPrefab・オフライン分身Prefab・ロビー用ネットワークプレイヤーPrefabを生成し、
/// MultiplayerLobbyScene へ一括配置・配線するエディタツール。
/// </summary>
[InitializeOnLoad]
public static class LobbyStageSetupEditor
{
    private const string LobbyStagePrefabPath = "Assets/Stages/Prefabs/Stage_Lobby.prefab";
    private const string LobbyPrefabsFolder = "Assets/Shiigi/Prefabs/Lobby";
    private const string LocalAvatarPrefabPath = "Assets/Shiigi/Prefabs/Lobby/LobbyLocalAvatar.prefab";
    private const string NetworkPlayerPrefabPath = "Assets/Shiigi/Prefabs/Lobby/LobbyNetworkPlayer.prefab";

    private const string LobbyScenePath = "Assets/Shiigi/Scenes/MultiplayerLobbyScene.unity";
    private const string Stage01PrefabPath = "Assets/Stages/Prefabs/Stage_01_Wave1.prefab";
    private const string BasePlayerPrefabPath = "Assets/A MAIN/Prefab/Player/Player.prefab";
    private const string NetworkPlayerVariantPath = "Assets/NetWork/Test/NetWork Player Variant.prefab";
    private const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
    private const string TileRoot = "Assets/Masaka/Tile/Eiji18TileObject";

    private const string AutoPromptSessionKey = "ShotGunOnline_LobbyStageSetup_Prompted";

    static LobbyStageSetupEditor()
    {
        EditorApplication.delayCall += TryPromptSetupOnLoad;
    }

    private static void TryPromptSetupOnLoad()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // 既にロビーステージPrefabとロビープレイヤーPrefabが存在し、PlayerSprites階層も含まれている場合はスキップ
        GameObject existingAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(LocalAvatarPrefabPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyStagePrefabPath) != null &&
            existingAvatar != null &&
            existingAvatar.transform.Find("PlayerSprites") != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerPrefabPath) != null)
        {
            return;
        }

        // PlayerSprites 欠落の修正時はダイアログなしで自動再生成する
        if (existingAvatar != null && existingAvatar.transform.Find("PlayerSprites") == null)
        {
            BuildAll();
            Debug.Log("[LobbyStageSetupEditor] プレイヤーの見た階層(PlayerSprites)とカメラ・移動速度設定を自動更新しました。");
            return;
        }

        if (SessionState.GetBool(AutoPromptSessionKey, false))
        {
            return;
        }
        SessionState.SetBool(AutoPromptSessionKey, true);

        bool execute = EditorUtility.DisplayDialog(
            "ロビー集会所ステージ自動構築ツール",
            "マルチプレイ集会所（ロビー）用のステージとプレイヤー出現システムのスクリプトが読み込まれました。\n\n" +
            "・集会所ステージ (Stage_Lobby.prefab)\n" +
            "・オフライン分身 (LobbyLocalAvatar.prefab)\n" +
            "・ロビー用ネットワークプレイヤー (LobbyNetworkPlayer.prefab)\n" +
            "・MultiplayerLobbyScene への自動配置と参照設定\n\n" +
            "を今すぐ自動構築しますか？",
            "今すぐ自動構築を実行",
            "後でメニューから実行"
        );

        if (execute)
        {
            BuildLobbyStageAndSetupSceneInteractive();
        }
    }

    [MenuItem("Tools/Lobby System/ロビー集会所ステージ＆プレイヤーを一括構築 (MultiplayerLobbyScene)", priority = 10)]
    public static void BuildLobbyStageAndSetupSceneInteractive()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("実行不可", "再生モードを終了してから実行してください。", "OK");
            return;
        }

        BuildAll();

        EditorUtility.DisplayDialog(
            "ロビー集会所構築完了",
            "集会所ステージとロビー用プレイヤーの生成・シーン配置が完了しました！\n\n" +
            $"・ステージ: {LobbyStagePrefabPath}\n" +
            $"・オフライン分身: {LocalAvatarPrefabPath}\n" +
            $"・NWプレイヤー: {NetworkPlayerPrefabPath}\n" +
            $"・更新シーン: {LobbyScenePath}",
            "OK"
        );
    }

    public static void BuildAll()
    {
        EnsureFoldersExist();

        try
        {
            EditorUtility.DisplayProgressBar("ロビー集会所構築", "1/4 集会所ステージPrefabを生成中...", 0.15f);
            StageController lobbyStagePrefab = BuildAndSaveLobbyStagePrefab();

            EditorUtility.DisplayProgressBar("ロビー集会所構築", "2/4 オフライン分身＆ネットワークプレイヤーPrefabを生成中...", 0.45f);
            LobbyLocalAvatar localAvatarPrefab = BuildAndSaveLocalAvatarPrefab();
            LobbyNetworkPlayer networkPlayerPrefab = BuildAndSaveNetworkPlayerPrefab();

            EditorUtility.DisplayProgressBar("ロビー集会所構築", "3/4 DefaultNetworkPrefabs へ登録中...", 0.70f);
            RegisterInDefaultNetworkPrefabs(networkPlayerPrefab != null ? networkPlayerPrefab.gameObject : null);

            EditorUtility.DisplayProgressBar("ロビー集会所構築", "4/4 MultiplayerLobbyScene をセットアップ中...", 0.85f);
            SetupLobbyScene(lobbyStagePrefab, localAvatarPrefab, networkPlayerPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LobbyStageSetupEditor] ロビー集会所ステージとプレイヤーシステムの構築が完了しました。");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Stages"))
        {
            AssetDatabase.CreateFolder("Assets", "Stages");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Stages/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/Stages", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Shiigi/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/Shiigi", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(LobbyPrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Shiigi/Prefabs", "Lobby");
        }
    }

    /// <summary>
    /// 集会所広場ステージ (Stage_Lobby.prefab) を生成・保存する
    /// </summary>
    private static StageController BuildAndSaveLobbyStagePrefab()
    {
        GameObject stageRoot = new GameObject("Stage_Lobby");
        StageController controller = stageRoot.AddComponent<StageController>();

        // 1. Terrain (Grid + Ground / Decolation / Wall / BoundaryWalls / StageLight)
        GameObject terrainGo = new GameObject("Terrain");
        terrainGo.transform.SetParent(stageRoot.transform, false);
        Grid grid = terrainGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Tilemap用マテリアルやライトを Stage_01_Wave1 から取得
        Material tilemapLitMaterial = null;
        GameObject stage01Asset = AssetDatabase.LoadAssetAtPath<GameObject>(Stage01PrefabPath);
        if (stage01Asset != null)
        {
            var sampleRenderer = stage01Asset.GetComponentInChildren<TilemapRenderer>(true);
            if (sampleRenderer != null)
            {
                tilemapLitMaterial = sampleRenderer.sharedMaterial;
            }
        }

        GameObject mapContainer = new GameObject("LobbyMap");
        mapContainer.transform.SetParent(terrainGo.transform, false);
        mapContainer.layer = 3; // TileMap layer

        // Ground Tilemap (歩行床: sortingOrder = -2)
        GameObject groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(mapContainer.transform, false);
        groundGo.layer = 3;
        Tilemap groundMap = groundGo.AddComponent<Tilemap>();
        TilemapRenderer groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = -2;
        if (tilemapLitMaterial != null) groundRenderer.sharedMaterial = tilemapLitMaterial;

        // Decolation Tilemap (床の装飾・道・紋章: sortingOrder = -1)
        GameObject decoGo = new GameObject("Decolation");
        decoGo.transform.SetParent(mapContainer.transform, false);
        decoGo.layer = 3;
        Tilemap decoMap = decoGo.AddComponent<Tilemap>();
        TilemapRenderer decoRenderer = decoGo.AddComponent<TilemapRenderer>();
        decoRenderer.sortingOrder = -1;
        if (tilemapLitMaterial != null) decoRenderer.sharedMaterial = tilemapLitMaterial;

        // Wall Tilemap (壁・柱オブジェクト: sortingOrder = 0 + Collider)
        GameObject wallGo = new GameObject("Wall");
        wallGo.transform.SetParent(mapContainer.transform, false);
        wallGo.tag = "Wall";
        Tilemap wallMap = wallGo.AddComponent<Tilemap>();
        TilemapRenderer wallRenderer = wallGo.AddComponent<TilemapRenderer>();
        wallRenderer.sortingOrder = 0;
        if (tilemapLitMaterial != null) wallRenderer.sharedMaterial = tilemapLitMaterial;
        Rigidbody2D wallRb = wallGo.AddComponent<Rigidbody2D>();
        wallRb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D wallTileCol = wallGo.AddComponent<TilemapCollider2D>();
        wallTileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        wallGo.AddComponent<CompositeCollider2D>();

        // タイルパレットから集会所広場を描画
        PaintLobbyPlazaTiles(groundMap, decoMap, wallMap);

        // 外周の落下防止コライダー（集会所の島から外へ出られないように4辺を囲む）
        CreatePerimeterBoundaryWalls(mapContainer.transform, minX: -11f, maxX: 11f, minY: -6f, maxY: 6f);

        // Stage_01_Wave1 から StageLight（2Dライト照明）を複製
        if (stage01Asset != null)
        {
            Transform srcLight = stage01Asset.transform.Find("Terrain/Map1/StageLight");
            if (srcLight != null)
            {
                GameObject lightCopy = Object.Instantiate(srcLight.gameObject, mapContainer.transform, false);
                lightCopy.name = "StageLight";
            }
        }

        // 2. SpawnPoints (P1～P4: 広場中央の4つの紋章の上)
        GameObject spawnGroup = new GameObject("SpawnPoints");
        spawnGroup.transform.SetParent(stageRoot.transform, false);

        GameObject initialSpawnsGo = new GameObject("InitialPlayerSpawns");
        initialSpawnsGo.transform.SetParent(spawnGroup.transform, false);
        StageSpawnPoints initialSpawns = initialSpawnsGo.AddComponent<StageSpawnPoints>();

        Vector3[] spawnPositions =
        {
            new Vector3(-3.5f,  2.0f, 0f), // P1 (左上紋章)
            new Vector3( 3.5f,  2.0f, 0f), // P2 (右上紋章)
            new Vector3(-3.5f, -2.0f, 0f), // P3 (左下紋章)
            new Vector3( 3.5f, -2.0f, 0f), // P4 (右下紋章)
        };

        var pointTransforms = new Transform[spawnPositions.Length];
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject pt = new GameObject($"p{i + 1}");
            pt.transform.SetParent(initialSpawnsGo.transform, false);
            pt.transform.localPosition = spawnPositions[i];
            pointTransforms[i] = pt.transform;
        }

        SerializedObject spawnsSo = new SerializedObject(initialSpawns);
        SerializedProperty pointsProp = spawnsSo.FindProperty("_spawnPoints");
        pointsProp.arraySize = pointTransforms.Length;
        for (int i = 0; i < pointTransforms.Length; i++)
        {
            pointsProp.GetArrayElementAtIndex(i).objectReferenceValue = pointTransforms[i];
        }
        spawnsSo.ApplyModifiedPropertiesWithoutUndo();

        // RespawnPoints
        GameObject respawnGroup = new GameObject("RespawnPoints");
        respawnGroup.transform.SetParent(spawnGroup.transform, false);
        GameObject r0 = new GameObject("Respawn_0");
        r0.transform.SetParent(respawnGroup.transform, false);
        r0.transform.localPosition = Vector3.zero;

        //StageController 参照設定
        SerializedObject stageSo = new SerializedObject(controller);
        stageSo.FindProperty("_terrainGrid").objectReferenceValue = grid;
        stageSo.FindProperty("_initialPlayerSpawns").objectReferenceValue = initialSpawns;
        SerializedProperty respawnsProp = stageSo.FindProperty("_respawnPoints");
        respawnsProp.arraySize = 1;
        respawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = r0.transform;
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        try
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(stageRoot, LobbyStagePrefabPath);
            return saved != null ? saved.GetComponent<StageController>() : null;
        }
        finally
        {
            Object.DestroyImmediate(stageRoot);
        }
    }

    private static void PaintLobbyPlazaTiles(Tilemap groundMap, Tilemap decoMap, Tilemap wallMap)
    {
        // 基本床タイル
        TileBase floorMain = LoadTile("Tile18x18_16");
        TileBase floorEdge = LoadTile("Tile18x18_15");
        TileBase cornerTL = LoadTile("Tile18x18_10");
        TileBase cornerTR = LoadTile("Tile18x18_11");
        TileBase edgeLeft = LoadTile("Tile18x18_19");
        TileBase edgeRight = LoadTile("Tile18x18_22");
        TileBase cliffSkirt = LoadTile("Tile18x18_24");
        TileBase cliffSkirtR = LoadTile("Tile18x18_25");
        TileBase cliffShadow = LoadTile("Tile2_26");

        // 装飾・道・紋章タイル
        TileBase emblemTL = LoadTile("Tile18x18_2");
        TileBase emblemTR = LoadTile("Tile18x18_3");
        TileBase emblemBL = LoadTile("Tile18x18_7");
        TileBase emblemBR = LoadTile("Tile18x18_8");
        TileBase pebble = LoadTile("Tile18x18_21");
        TileBase moss = LoadTile("DecorationV2_29");

        TileBase roadTL = LoadTile("Road_8");
        TileBase roadTM = LoadTile("Road_9");
        TileBase roadTR = LoadTile("Road_10");
        TileBase roadBL = LoadTile("Road_14");
        TileBase roadBM = LoadTile("Road_15");
        TileBase roadBR = LoadTile("Road_16");

        // 壁・柱タイル
        TileBase wallL = LoadTile("Decorations1_10");
        TileBase wallM = LoadTile("Decorations1_5");
        TileBase wallM2 = LoadTile("DecoV3_28");
        TileBase wallR = LoadTile("Decorations1_11");
        TileBase gateLT = LoadTile("DecorationV2_31");
        TileBase gateLM = LoadTile("Decorations1_9");
        TileBase gateLB = LoadTile("DecorationV2_25");
        TileBase gateRT = LoadTile("DecorationV2_30");
        TileBase gateRM = LoadTile("Decorations1_12");
        TileBase gateRB = LoadTile("DecorationV2_24");

        const int minX = -12;
        const int maxX = 11;
        const int minY = -7;
        const int maxY = 6;

        // 1. Ground: 24x14 の浮島フロアを敷き詰める
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                TileBase tile = floorMain;

                if (y == maxY && x == minX) tile = cornerTL;
                else if (y == maxY && x == maxX) tile = cornerTR;
                else if (y == maxY || y == minY) tile = floorEdge;
                else if (x == minX) tile = edgeLeft;
                else if (x == maxX) tile = edgeRight;

                if (tile != null)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            // 左右外側の崖影
            if (cliffShadow != null && y < maxY)
            {
                groundMap.SetTile(new Vector3Int(minX - 1, y, 0), cliffShadow);
                groundMap.SetTile(new Vector3Int(maxX + 1, y, 0), cliffShadow);
            }
        }

        // 2. Decolation: 下端の崖側面スカート
        if (cliffSkirt != null)
        {
            if (cliffShadow != null) decoMap.SetTile(new Vector3Int(minX, minY - 1, 0), cliffShadow);
            for (int x = minX + 1; x < maxX; x++)
            {
                decoMap.SetTile(new Vector3Int(x, minY - 1, 0), cliffSkirt);
            }
            if (cliffSkirtR != null) decoMap.SetTile(new Vector3Int(maxX, minY - 1, 0), cliffSkirtR);
        }

        // 広場中央のクロスロード舗装
        for (int blockX = -6; blockX <= 3; blockX += 3)
        {
            PaintRoadPatch3x2(decoMap, blockX, 0, roadTL, roadTM, roadTR, roadBL, roadBM, roadBR);
        }
        PaintRoadPatch3x2(decoMap, -1, 2, roadTL, roadTM, roadTR, roadBL, roadBM, roadBR);
        PaintRoadPatch3x2(decoMap, -1, -2, roadTL, roadTM, roadTR, roadBL, roadBM, roadBR);

        // P1～P4 スポーン地点の2x2石畳紋章
        PaintEmblem2x2(decoMap, -4, 2, emblemTL, emblemTR, emblemBL, emblemBR);
        PaintEmblem2x2(decoMap,  3, 2, emblemTL, emblemTR, emblemBL, emblemBR);
        PaintEmblem2x2(decoMap, -4, -2, emblemTL, emblemTR, emblemBL, emblemBR);
        PaintEmblem2x2(decoMap,  3, -2, emblemTL, emblemTR, emblemBL, emblemBR);

        // アクセント装飾（小石・苔）
        Vector2Int[] pebbleCoords =
        {
            new Vector2Int(-9, 3), new Vector2Int(8, 3),
            new Vector2Int(-9, -4), new Vector2Int(8, -4),
            new Vector2Int(0, 4), new Vector2Int(-1, -5)
        };
        foreach (var p in pebbleCoords)
        {
            if (pebble != null) decoMap.SetTile(new Vector3Int(p.x, p.y, 0), pebble);
            if (moss != null) decoMap.SetTile(new Vector3Int(p.x + 1, p.y, 0), moss);
        }

        // 3. Wall: 左右の門柱と上下の装飾石壁
        if (gateLT != null && gateLM != null && gateLB != null)
        {
            wallMap.SetTile(new Vector3Int(minX + 1,  1, 0), gateLT);
            wallMap.SetTile(new Vector3Int(minX + 1,  0, 0), gateLM);
            wallMap.SetTile(new Vector3Int(minX + 1, -1, 0), gateLM);
            wallMap.SetTile(new Vector3Int(minX + 1, -2, 0), gateLB);
        }
        if (gateRT != null && gateRM != null && gateRB != null)
        {
            wallMap.SetTile(new Vector3Int(maxX - 1,  1, 0), gateRT);
            wallMap.SetTile(new Vector3Int(maxX - 1,  0, 0), gateRM);
            wallMap.SetTile(new Vector3Int(maxX - 1, -1, 0), gateRM);
            wallMap.SetTile(new Vector3Int(maxX - 1, -2, 0), gateRB);
        }

        // 上辺・下辺の石壁アクセント
        PaintHorizontalWall3(wallMap, -7, maxY - 1, wallL, wallM2 != null ? wallM2 : wallM, wallR);
        PaintHorizontalWall3(wallMap,  4, maxY - 1, wallL, wallM2 != null ? wallM2 : wallM, wallR);
        PaintHorizontalWall3(wallMap, -7, minY + 1, wallL, wallM, wallR);
        PaintHorizontalWall3(wallMap,  4, minY + 1, wallL, wallM, wallR);
    }

    private static void PaintRoadPatch3x2(
        Tilemap map, int x, int y,
        TileBase tl, TileBase tm, TileBase tr,
        TileBase bl, TileBase bm, TileBase br)
    {
        if (tl != null) map.SetTile(new Vector3Int(x,     y,     0), tl);
        if (tm != null) map.SetTile(new Vector3Int(x + 1, y,     0), tm);
        if (tr != null) map.SetTile(new Vector3Int(x + 2, y,     0), tr);
        if (bl != null) map.SetTile(new Vector3Int(x,     y - 1, 0), bl);
        if (bm != null) map.SetTile(new Vector3Int(x + 1, y - 1, 0), bm);
        if (br != null) map.SetTile(new Vector3Int(x + 2, y - 1, 0), br);
    }

    private static void PaintEmblem2x2(
        Tilemap map, int x, int y,
        TileBase tl, TileBase tr, TileBase bl, TileBase br)
    {
        if (tl != null) map.SetTile(new Vector3Int(x,     y,     0), tl);
        if (tr != null) map.SetTile(new Vector3Int(x + 1, y,     0), tr);
        if (bl != null) map.SetTile(new Vector3Int(x,     y - 1, 0), bl);
        if (br != null) map.SetTile(new Vector3Int(x + 1, y - 1, 0), br);
    }

    private static void PaintHorizontalWall3(Tilemap map, int x, int y, TileBase l, TileBase m, TileBase r)
    {
        if (l != null) map.SetTile(new Vector3Int(x,     y, 0), l);
        if (m != null) map.SetTile(new Vector3Int(x + 1, y, 0), m);
        if (r != null) map.SetTile(new Vector3Int(x + 2, y, 0), r);
    }

    private static void CreatePerimeterBoundaryWalls(Transform parent, float minX, float maxX, float minY, float maxY)
    {
        GameObject boundaryRoot = new GameObject("BoundaryWalls");
        boundaryRoot.transform.SetParent(parent, false);
        boundaryRoot.tag = "Wall";

        float width = (maxX - minX) + 2f;
        float height = (maxY - minY) + 2f;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        const float thickness = 1f;

        AddBoundaryBox(boundaryRoot.transform, "NorthWall", new Vector2(centerX, maxY + thickness * 0.5f), new Vector2(width, thickness));
        AddBoundaryBox(boundaryRoot.transform, "SouthWall", new Vector2(centerX, minY - thickness * 0.5f), new Vector2(width, thickness));
        AddBoundaryBox(boundaryRoot.transform, "WestWall",  new Vector2(minX - thickness * 0.5f, centerY), new Vector2(thickness, height));
        AddBoundaryBox(boundaryRoot.transform, "EastWall",  new Vector2(maxX + thickness * 0.5f, centerY), new Vector2(thickness, height));
    }

    private static void AddBoundaryBox(Transform parent, string name, Vector2 center, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.tag = "Wall";
        go.transform.localPosition = new Vector3(center.x, center.y, 0f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static TileBase LoadTile(string tileName)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/{tileName}.asset");
    }

    /// <summary>
    /// オフライン分身プレハブ (LobbyLocalAvatar.prefab) を生成・保存する
    /// </summary>
    private static LobbyLocalAvatar BuildAndSaveLocalAvatarPrefab()
    {
        GameObject root = CreateCleanPlayerVisualHierarchy("LobbyLocalAvatar");

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.offset = new Vector2(0.0036386847f, -0.3598805f);
        col.radius = 0.34091502f;

        LobbyLocalAvatar avatar = root.AddComponent<LobbyLocalAvatar>();
        WireVisualReferences(root, avatar);

        try
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, LocalAvatarPrefabPath);
            return saved != null ? saved.GetComponent<LobbyLocalAvatar>() : null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// ロビー用ネットワークプレイヤープレハブ (LobbyNetworkPlayer.prefab) を生成・保存する
    /// </summary>
    private static LobbyNetworkPlayer BuildAndSaveNetworkPlayerPrefab()
    {
        GameObject root = CreateCleanPlayerVisualHierarchy("LobbyNetworkPlayer");

        NetworkObject netObj = root.AddComponent<NetworkObject>();
        netObj.DontDestroyWithOwner = false;

        ClientNetworkTransform netTransform = root.AddComponent<ClientNetworkTransform>();
        netTransform.SyncPositionX = true;
        netTransform.SyncPositionY = true;
        netTransform.SyncPositionZ = false;
        netTransform.SyncRotAngleX = false;
        netTransform.SyncRotAngleY = false;
        netTransform.SyncRotAngleZ = false;
        netTransform.SyncScaleX = false;
        netTransform.SyncScaleY = false;
        netTransform.SyncScaleZ = false;

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.offset = new Vector2(0.0036386847f, -0.3598805f);
        col.radius = 0.34091502f;

        LobbyNetworkPlayer netPlayer = root.AddComponent<LobbyNetworkPlayer>();
        WireVisualReferences(root, netPlayer);

        // NetWork Player Variant から PlayerSkinSync 設定（P1～P4スキン配列）をコピー
        CopyPlayerSkinSyncIfAvailable(root);

        try
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, NetworkPlayerPrefabPath);
            return saved != null ? saved.GetComponent<LobbyNetworkPlayer>() : null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// NetWork Player Variant (または Player.prefab) から見た目に必要な子階層（PlayerSprites / Shadow / PlayerLight）を抽出したクリーンなGameObjectを作る
    /// </summary>
    private static GameObject CreateCleanPlayerVisualHierarchy(string name)
    {
        GameObject root = new GameObject(name);
        root.layer = 8; // Player layer
        root.tag = "Player";

        GameObject basePlayerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerVariantPath);
        if (basePlayerAsset == null)
        {
            basePlayerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePlayerPrefabPath);
        }
        if (basePlayerAsset == null)
        {
            Debug.LogError($"[LobbyStageSetupEditor] {BasePlayerPrefabPath} が見つかりません。");
            return root;
        }

        GameObject tempPlayer = Object.Instantiate(basePlayerAsset);
        try
        {
            // Player.prefab では SpriteManager は PlayerSprites の子にあるため PlayerSprites ごと複製する
            string[] visualChildNames = { "PlayerSprites", "Shadow", "PlayerLight" };
            foreach (string childName in visualChildNames)
            {
                Transform child = tempPlayer.transform.Find(childName);
                if (child == null) continue;

                GameObject copy = Object.Instantiate(child.gameObject, root.transform, false);
                copy.name = childName;
            }

            // ロビーでは不要な必殺技オーラ (UltAura) を非表示化
            Transform ultAura = root.transform.Find("PlayerSprites/UltAura");
            if (ultAura != null)
            {
                ultAura.gameObject.SetActive(false);
            }

            // SpriteDirection や DamageMaterial 等の本編依存・NetworkBehaviour コンポーネントを除去
            foreach (var sd in root.GetComponentsInChildren<SpriteDirection>(true))
            {
                Object.DestroyImmediate(sd);
            }
            foreach (var dm in root.GetComponentsInChildren<DamageMaterial>(true))
            {
                Object.DestroyImmediate(dm);
            }
            foreach (var nb in root.GetComponentsInChildren<NetworkBehaviour>(true))
            {
                Object.DestroyImmediate(nb);
            }
            foreach (var no in root.GetComponentsInChildren<NetworkObject>(true))
            {
                Object.DestroyImmediate(no);
            }
        }
        finally
        {
            Object.DestroyImmediate(tempPlayer);
        }

        return root;
    }

    private static void WireVisualReferences(GameObject root, Component targetComponent)
    {
        PlayerAnimation anim = root.GetComponentInChildren<PlayerAnimation>(true);
        SpriteRenderer front = null;
        SpriteRenderer back = null;

        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.name == "FrontSprite") front = sr;
            else if (sr.name == "BackSprite") back = sr;
        }

        SerializedObject so = new SerializedObject(targetComponent);
        so.FindProperty("_playerAnimation").objectReferenceValue = anim;
        so.FindProperty("_frontSprite").objectReferenceValue = front;
        so.FindProperty("_backSprite").objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyPlayerSkinSyncIfAvailable(GameObject root)
    {
        GameObject variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerVariantPath);
        if (variantAsset == null) return;

        PlayerSkinSync srcSkin = variantAsset.GetComponent<PlayerSkinSync>();
        if (srcSkin == null) return;

        PlayerSkinSync dstSkin = root.AddComponent<PlayerSkinSync>();
        EditorUtility.CopySerialized(srcSkin, dstSkin);

        Animator frontAnim = null;
        Animator backAnim = null;
        foreach (var anim in root.GetComponentsInChildren<Animator>(true))
        {
            if (anim.name == "FrontSprite") frontAnim = anim;
            else if (anim.name == "BackSprite") backAnim = anim;
        }

        SerializedObject skinSo = new SerializedObject(dstSkin);
        if (frontAnim != null) skinSo.FindProperty("_flontPlayerAnimator").objectReferenceValue = frontAnim;
        if (backAnim != null) skinSo.FindProperty("_backPlayerAnimator").objectReferenceValue = backAnim;
        skinSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RegisterInDefaultNetworkPrefabs(GameObject networkPlayerPrefab)
    {
        if (networkPlayerPrefab == null) return;

        NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(DefaultNetworkPrefabsPath);
        if (list == null) return;

        if (!list.Contains(networkPlayerPrefab))
        {
            list.Add(new NetworkPrefab { Prefab = networkPlayerPrefab });
            EditorUtility.SetDirty(list);
        }
    }

    /// <summary>
    /// MultiplayerLobbyScene に集会所ステージ・カメラ追従・LobbyPlayerSystem を配置して保存する
    /// </summary>
    private static void SetupLobbyScene(
        StageController lobbyStagePrefab,
        LobbyLocalAvatar localAvatarPrefab,
        LobbyNetworkPlayer networkPlayerPrefab)
    {
        if (!File.Exists(LobbyScenePath))
        {
            Debug.LogError($"[LobbyStageSetupEditor] {LobbyScenePath} が見つかりません。");
            return;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene lobbyScene = SceneManager.GetSceneByPath(LobbyScenePath);
        bool openedAdditively = false;

        if (!lobbyScene.IsValid() || !lobbyScene.isLoaded)
        {
            lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            openedAdditively = true;
        }

        SceneManager.SetActiveScene(lobbyScene);

        try
        {
            // 1. 既存の Stage_Lobby があれば置き換え、無ければ新規配置
            foreach (GameObject root in lobbyScene.GetRootGameObjects())
            {
                if (root != null && root.name == "Stage_Lobby")
                {
                    Object.DestroyImmediate(root);
                }
            }

            StageSpawnPoints sceneSpawnPoints = null;
            if (lobbyStagePrefab != null)
            {
                GameObject stageInst = (GameObject)PrefabUtility.InstantiatePrefab(lobbyStagePrefab.gameObject, lobbyScene);
                stageInst.name = "Stage_Lobby";
                stageInst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                sceneSpawnPoints = stageInst.GetComponentInChildren<StageSpawnPoints>(true);
            }

            // 2. 背景スプライト (BackGround) がステージ床(-2)を隠さないよう sortingOrder を -10 に下げる
            foreach (GameObject root in lobbyScene.GetRootGameObjects())
            {
                if (root == null) continue;

                if (root.name.StartsWith("BackGround"))
                {
                    foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.sortingOrder = -10;
                        EditorUtility.SetDirty(sr);
                    }
                }
                else if (root.name == "Canvas")
                {
                    // タイトル画面から流用された巨大立ち絵(ImageShadow)と上下黒帯(Black)を非表示化
                    Transform shadow = root.transform.Find("ImageShadow");
                    if (shadow != null) shadow.gameObject.SetActive(false);

                    Transform black = root.transform.Find("Black");
                    if (black != null) black.gameObject.SetActive(false);
                }
            }

            // 3. Main Camera に LobbyCameraFollow をアタッチし、本編に近いカメラサイズ(11)に設定
            LobbyCameraFollow cameraFollow = null;
            foreach (GameObject root in lobbyScene.GetRootGameObjects())
            {
                if (root == null) continue;
                Camera cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null && cam.CompareTag("MainCamera"))
                {
                    cam.orthographicSize = 11f;
                    cameraFollow = cam.GetComponent<LobbyCameraFollow>();
                    if (cameraFollow == null)
                    {
                        cameraFollow = cam.gameObject.AddComponent<LobbyCameraFollow>();
                    }
                    EditorUtility.SetDirty(cam);
                    EditorUtility.SetDirty(cam.gameObject);
                    break;
                }
            }

            // 4. LobbyPlayerSystem (LobbyNetworkSpawner + LobbyAvatarSwitcher) を配置・配線
            GameObject systemGo = null;
            foreach (GameObject root in lobbyScene.GetRootGameObjects())
            {
                if (root != null && root.name == "LobbyPlayerSystem")
                {
                    systemGo = root;
                    break;
                }
            }

            if (systemGo == null)
            {
                systemGo = new GameObject("LobbyPlayerSystem");
                SceneManager.MoveGameObjectToScene(systemGo, lobbyScene);
            }

            LobbyNetworkSpawner spawner = systemGo.GetComponent<LobbyNetworkSpawner>();
            if (spawner == null) spawner = systemGo.AddComponent<LobbyNetworkSpawner>();

            LobbyAvatarSwitcher switcher = systemGo.GetComponent<LobbyAvatarSwitcher>();
            if (switcher == null) switcher = systemGo.AddComponent<LobbyAvatarSwitcher>();

            if (networkPlayerPrefab != null)
            {
                SerializedObject spawnerSo = new SerializedObject(spawner);
                spawnerSo.FindProperty("_lobbyPlayerPrefab").objectReferenceValue = networkPlayerPrefab.GetComponent<NetworkObject>();
                spawnerSo.FindProperty("_spawnPoints").objectReferenceValue = sceneSpawnPoints;
                spawnerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (localAvatarPrefab != null)
            {
                SerializedObject switcherSo = new SerializedObject(switcher);
                switcherSo.FindProperty("_localAvatarPrefab").objectReferenceValue = localAvatarPrefab;
                switcherSo.FindProperty("_spawnPoints").objectReferenceValue = sceneSpawnPoints;
                switcherSo.FindProperty("_cameraFollow").objectReferenceValue = cameraFollow;
                switcherSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
        }
        finally
        {
            if (openedAdditively && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
                EditorSceneManager.CloseScene(lobbyScene, true);
            }
        }
    }
}
