using System.Collections;
using UnityEngine;

public class ZanzouFeedOut : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _mySpriteRenderer = default;

    [SerializeField, Header("フェードアウトにかける時間（秒）")]
    private float _fadeDuration = 0.5f;

    private void Start()
    {
        StartCoroutine(FadeOutSprite(_mySpriteRenderer));
    }

    public IEnumerator FadeOutSprite(SpriteRenderer renderer)
    {
        if (renderer == null) yield break;

        float elapsed = 0f;
        Color initialColor = renderer.color;
        float startAlpha = 0.8f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);

            Color c = renderer.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            renderer.color = c;

            yield return null;
        }

        Color finalColor = renderer.color;
        finalColor.a = 0f;
        renderer.color = finalColor;
    }
}
