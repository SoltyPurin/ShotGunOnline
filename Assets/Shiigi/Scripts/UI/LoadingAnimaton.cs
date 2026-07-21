using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingAnimation : MonoBehaviour
{
    private const float DURATION = 1f;

    /// <summary>
    /// コピペ！: https://game-ui.net/?p=51
    /// </summary>
    void Start()
    {
        Image[] circles = GetComponentsInChildren<Image>();
        for (var i = 0; i < circles.Length; i++)
        {
            var angle = -2 * Mathf.PI * i / circles.Length;
            circles[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 50f;
            circles[i].DOFade(0f, DURATION).SetLoops(-1, LoopType.Yoyo).SetDelay(DURATION * i / circles.Length);
        }
    }
}