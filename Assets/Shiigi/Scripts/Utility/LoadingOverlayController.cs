using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通信中・更新中などのローディングオーバーレイ表示を制御するクラス
/// 表示時に全画面ブロック用UIを生成し、背後UIのボタン押下を防止します。
/// </summary>
public class LoadingOverlayController : MonoBehaviour
{
    [SerializeField] private Text _loadingContext;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);

    private GameObject _blocker;

    public void SetLoadingState(bool isLoading)
    {
        SetLoadingState(isLoading, null);
    }

    public void SetLoadingState(bool isLoading, string context)
    {
        if (isLoading)
        {
            EnsureBlocker();

            // キーボードやゲームパッドで背後UIボタンが誤押下されないようフォーカスを解除
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (_loadingContext != null && context != null)
        {
            _loadingContext.text = context;
        }

        gameObject.SetActive(isLoading);
    }

    private void EnsureBlocker()
    {
        if (_blocker == null)
        {
            _blocker = UIBlockerHelper.EnsureBlocker(transform, _blockerColor);
        }
        else
        {
            _blocker.SetActive(true);
            _blocker.transform.SetAsFirstSibling();
        }
    }
}
