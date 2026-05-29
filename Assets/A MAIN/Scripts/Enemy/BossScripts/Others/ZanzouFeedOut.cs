using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZanzouFeedOut : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _mySpriteRenderer = default;
    private void Start()
    {
        StartCoroutine(FadeOutSprite(_mySpriteRenderer));
    }

    public IEnumerator FadeOutSprite(SpriteRenderer renderer)
    {
        float frames = 30f;
        for (int i = 0; i < frames; i++)
        {
            Color c = renderer.color;
            c.a = Mathf.Lerp(0.8f, 0f, i / frames); // 1 ¨ 0 ‚É•âŠÔ
            renderer.color = c;
            yield return null; // 1ƒtƒŒ[ƒ€‘Ò‚Â
        }
    }

}
