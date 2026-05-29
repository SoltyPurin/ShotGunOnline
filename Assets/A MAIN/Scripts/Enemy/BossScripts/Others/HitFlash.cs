using UnityEngine;
using System.Collections;

/// <summary>
/// 発光するメソッド
/// </summary>
public class HitFlash : MonoBehaviour
{
    [SerializeField,Header("ボスのスプライト")] 
    private SpriteRenderer sr;           // 未設定なら自動取得
    [SerializeField, Range(0f, 1f)] 
    private float flashAmountOnHit = 1f;
    [SerializeField,Header("発光時間")] 
    private float flashDuration = 0.07f;

    private static readonly int FlashProp = Shader.PropertyToID("_FlashAmount");
    private MaterialPropertyBlock mpb;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashProp, 0f);
        sr.SetPropertyBlock(mpb);
    }

    public void DoFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // ピカッ！
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashProp, flashAmountOnHit);
        sr.SetPropertyBlock(mpb);

        yield return new WaitForSeconds(flashDuration);

        // 速攻で戻す
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashProp, 0f);
        sr.SetPropertyBlock(mpb);
    }
}

