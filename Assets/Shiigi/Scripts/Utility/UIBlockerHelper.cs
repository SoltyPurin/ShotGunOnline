using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ポップアップやローディング画面の表示時に、背後UIへの誤クリックや操作を遮断する
/// 全画面ブロック用UI（ScreenBlocker）を動的生成・管理するユーティリティクラス
/// </summary>
public static class UIBlockerHelper
{
    public const string BlockerGameObjectName = "ScreenBlocker";

    /// <summary>
    /// 指定された親Transformの最背面に、全画面を覆うRaycastブロック用UIを生成または取得する
    /// </summary>
    /// <param name="parent">ブロッカーを配置する親Transform</param>
    /// <param name="color">ブロッカーの背景色（半透明黒など）</param>
    /// <returns>生成または取得したブロッカーGameObject</returns>
    public static GameObject EnsureBlocker(Transform parent, Color color)
    {
        if (parent == null) return null;

        // 既に動的生成されたブロッカーが存在する場合は再利用
        var existing = parent.Find(BlockerGameObjectName);
        if (existing != null)
        {
            var img = existing.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                img.raycastTarget = true;
            }
            existing.gameObject.SetActive(true);
            existing.SetAsFirstSibling();
            return existing.gameObject;
        }

        // 既存の背景オブジェクト（BackGround等）がある場合はそれをブロッカーとして再利用
        var background = parent.Find("BackGround");
        if (background != null)
        {
            var bgImg = background.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.raycastTarget = true;
                background.gameObject.SetActive(true);
                background.SetAsFirstSibling();
                return background.gameObject;
            }
        }

        // 新規に全画面ブロック用のUIオブジェクトを生成
        var blockerObj = new GameObject(BlockerGameObjectName, typeof(RectTransform), typeof(Image));
        blockerObj.transform.SetParent(parent, false);

        var blockerRect = blockerObj.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        // 親UIがマージンや縮小を持っていても画面全体を確実にカバーするよう十分なオフセットを設定
        blockerRect.offsetMin = new Vector2(-2000f, -2000f);
        blockerRect.offsetMax = new Vector2(2000f, 2000f);
        blockerRect.pivot = new Vector2(0.5f, 0.5f);

        var blockerImage = blockerObj.GetComponent<Image>();
        blockerImage.color = color;
        blockerImage.raycastTarget = true;

        // パネルやテキスト・ボタンの背後（最奥）に配置
        blockerObj.transform.SetAsFirstSibling();
        return blockerObj;
    }
}
