#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TextMeshPro の Dynamic SDF フォントがエディタ再生中やシーン表示中に生成する一時グリフキャッシュを
/// 再生終了時およびアセット保存直前に自動クリアし、意図しない .asset の Git 差分・コンフリクトを防ぐエディタ拡張。
/// </summary>
[InitializeOnLoad]
public static class TmpDynamicFontAutoClear
{
    static TmpDynamicFontAutoClear()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            ClearAllDynamicFontCaches(logOnlyWhenCleared: true);
        }
    }

    [MenuItem("Tools/Font/Dynamicフォントのキャッシュをクリア")]
    public static void ClearAllDynamicFontCachesFromMenu()
    {
        int count = ClearAllDynamicFontCaches(logOnlyWhenCleared: false);
        if (count > 0)
        {
            AssetDatabase.SaveAssets();
        }
    }

    internal static bool ClearDynamicFontIfNeeded(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null) return false;
        if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic) return false;

        bool hasDynamicData =
            (fontAsset.characterTable != null && fontAsset.characterTable.Count > 0) ||
            (fontAsset.glyphTable != null && fontAsset.glyphTable.Count > 0);

        if (!hasDynamicData) return false;

        fontAsset.ClearFontAssetData(setAtlasSizeToZero: true);
        return true;
    }

    private static int ClearAllDynamicFontCaches(bool logOnlyWhenCleared)
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int clearedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (ClearDynamicFontIfNeeded(fontAsset))
            {
                clearedCount++;
            }
        }

        if (!logOnlyWhenCleared || clearedCount > 0)
        {
            Debug.Log($"[TmpDynamicFontAutoClear] {clearedCount} 件の Dynamic TMP_FontAsset キャッシュをクリアしました。");
        }

        return clearedCount;
    }
}

/// <summary>
/// Unity がアセットをディスクへ保存する直前に Dynamic TMP_FontAsset のキャッシュをクリアするフック。
/// Edit モード中にシーンビュー描画で溜まったキャッシュが保存されるのも防ぎます。
/// </summary>
public class TmpDynamicFontSaveProcessor : AssetModificationProcessor
{
    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (paths == null || paths.Length == 0) return paths;

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset")) continue;

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            TmpDynamicFontAutoClear.ClearDynamicFontIfNeeded(fontAsset);
        }

        return paths;
    }
}
#endif
