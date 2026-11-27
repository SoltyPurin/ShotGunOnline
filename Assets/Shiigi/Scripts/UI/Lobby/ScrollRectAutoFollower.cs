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

    private readonly Vector3[] _targetCorners = new Vector3[4];
    private readonly Vector3[] _viewportCorners = new Vector3[4];

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

    private void OnDisable()
    {
        _isAutoScrolling = false;
        _lastSelected = null;
    }

    private void Update()
    {
        if (_scrollRect == null || !_scrollRect.vertical || _scrollRect.content == null || _scrollRect.viewport == null) return;

        var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (current != _lastSelected)
        {
            _lastSelected = current;
            if (current != null && current.transform.IsChildOf(_scrollRect.content))
            {
                RectTransform itemRoot = ResolveContentItemRect(current.transform);
                if (TryComputeTargetNormalizedY(itemRoot, out float nextY))
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

    private RectTransform ResolveContentItemRect(Transform selected)
    {
        if (selected == null || _scrollRect == null || _scrollRect.content == null) return null;

        Transform current = selected;
        while (current.parent != null && current.parent != _scrollRect.content)
        {
            current = current.parent;
        }
        return current.GetComponent<RectTransform>();
    }

    private bool TryComputeTargetNormalizedY(RectTransform target, out float normalizedY)
    {
        normalizedY = _scrollRect.verticalNormalizedPosition;
        if (target == null) return false;

        float contentHeight = _scrollRect.content.rect.height;
        float viewportHeight = _scrollRect.viewport.rect.height;
        float scrollableHeight = contentHeight - viewportHeight;
        if (scrollableHeight <= 1f) return false;

        float lossyY = _scrollRect.viewport.lossyScale.y;
        float absLossyY = Mathf.Abs(lossyY);
        if (absLossyY <= 0.0001f) return false;

        float worldPadding = _padding * absLossyY;

        target.GetWorldCorners(_targetCorners);
        _scrollRect.viewport.GetWorldCorners(_viewportCorners);

        float targetBottom = _targetCorners[0].y;
        float targetTop = _targetCorners[1].y;
        float viewBottom = _viewportCorners[0].y;
        float viewTop = _viewportCorners[1].y;

        float deltaWorld;
        if (targetTop > viewTop - worldPadding)
        {
            deltaWorld = targetTop - (viewTop - worldPadding);
        }
        else if (targetBottom < viewBottom + worldPadding)
        {
            deltaWorld = targetBottom - (viewBottom + worldPadding);
        }
        else
        {
            return false;
        }

        float deltaLocal = deltaWorld / lossyY;
        normalizedY = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + deltaLocal / scrollableHeight);
        return true;
    }
}
