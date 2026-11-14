using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// コントローラー／キーボード操作時に、選択中のUI要素がScrollRectの表示範囲内に収まるよう自動スクロールするコンポーネント。
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollRectAutoFollower : MonoBehaviour
{
    [SerializeField] private float _padding = 16f;
    [SerializeField] private float _smoothSpeed = 14f;

    private ScrollRect _scrollRect;
    private GameObject _lastSelected;
    private float _targetNormalizedY;
    private bool _isAutoScrolling;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect != null)
        {
            _targetNormalizedY = _scrollRect.verticalNormalizedPosition;
        }
    }

    private void Update()
    {
        if (_scrollRect == null || _scrollRect.content == null || _scrollRect.viewport == null) return;

        var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (current != _lastSelected)
        {
            _lastSelected = current;
            if (current != null && current.transform.IsChildOf(_scrollRect.content))
            {
                if (TryComputeTargetNormalizedY(current.GetComponent<RectTransform>(), out float nextY))
                {
                    _targetNormalizedY = nextY;
                    _isAutoScrolling = true;
                }
            }
        }

        if (_isAutoScrolling)
        {
            float next = Mathf.Lerp(
                _scrollRect.verticalNormalizedPosition,
                _targetNormalizedY,
                1f - Mathf.Exp(-_smoothSpeed * Time.unscaledDeltaTime));

            _scrollRect.verticalNormalizedPosition = next;
            if (Mathf.Abs(next - _targetNormalizedY) < 0.001f)
            {
                _scrollRect.verticalNormalizedPosition = _targetNormalizedY;
                _isAutoScrolling = false;
            }
        }
    }

    private bool TryComputeTargetNormalizedY(RectTransform target, out float normalizedY)
    {
        normalizedY = _scrollRect.verticalNormalizedPosition;
        if (target == null) return false;

        float contentHeight = _scrollRect.content.rect.height;
        float viewportHeight = _scrollRect.viewport.rect.height;
        float scrollableHeight = contentHeight - viewportHeight;
        if (scrollableHeight <= 1f) return false;

        Vector3[] targetCorners = new Vector3[4];
        Vector3[] viewportCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);
        _scrollRect.viewport.GetWorldCorners(viewportCorners);

        float targetBottom = targetCorners[0].y;
        float targetTop = targetCorners[1].y;
        float viewBottom = viewportCorners[0].y;
        float viewTop = viewportCorners[1].y;

        float deltaWorld = 0f;
        if (targetTop > viewTop - _padding)
        {
            deltaWorld = targetTop - (viewTop - _padding);
        }
        else if (targetBottom < viewBottom + _padding)
        {
            deltaWorld = targetBottom - (viewBottom + _padding);
        }
        else
        {
            return false;
        }

        // ワールド差分をローカルのスクロール比率に変換
        float lossyY = _scrollRect.viewport.lossyScale.y;
        float deltaLocal = Mathf.Abs(lossyY) > 0.0001f ? deltaWorld / lossyY : deltaWorld;
        normalizedY = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + deltaLocal / scrollableHeight);
        return true;
    }
}
