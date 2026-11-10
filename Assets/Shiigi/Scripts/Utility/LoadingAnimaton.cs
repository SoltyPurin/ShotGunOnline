using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingAnimation : MonoBehaviour
{
    private const float DURATION = 1f;
    private Image[] _circles;

    /// <summary>
    /// コピペ！: https://game-ui.net/?p=51#toc2
    /// </summary>
    void Start()
    {
        _circles = GetComponentsInChildren<Image>();
        for (var i = 0; i < _circles.Length; i++)
        {
            var angle = -2 * Mathf.PI * i / _circles.Length;
            _circles[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 50f;
            _circles[i].DOFade(0f, DURATION).SetLoops(-1, LoopType.Yoyo).SetDelay(DURATION * i / _circles.Length);
        }
    }

    private void OnDestroy()
    {
        if (_circles == null) return;
        foreach (var circle in _circles)
        {
            if (circle != null)
            {
                circle.DOKill();
            }
        }
    }
}