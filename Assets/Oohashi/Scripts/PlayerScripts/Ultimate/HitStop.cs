using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    //必殺技の時に視覚的にもっとわかりやすく
    //例、SF6のSA打つときのようにキャラクターに
    //この極端に短い停止時間の中に効果音を入れる
    //ヒットストップのタイムスケール
    private float _ultHitStopTimeScale = 0.0f;
    //タイムスケールを戻すまでの時間
    private float _returnTimeScaleTime = 0.1f;

    private Coroutine _hitStopCoroutine = null;

    /// <summary>
    /// タイムスケールを変更するメソッド
    /// </summary>
    public void UltHitStopMethod()
    {
        if (!isActiveAndEnabled) return;

        Time.timeScale = _ultHitStopTimeScale;
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
        }
        _hitStopCoroutine = StartCoroutine(ReturnTimeScale());
    }

    /// <summary>
    /// クリティカルヒットのタイムスケールを変更
    /// </summary>
    public void CriticalHitStopMethod()
    {
        if (!isActiveAndEnabled) return;

        Time.timeScale = _ultHitStopTimeScale;
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
        }
        _hitStopCoroutine = StartCoroutine(ReturnTimeScale());
    }

    /// <summary>
    /// タイムスケールを戻すコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReturnTimeScale()
    {
        //実際の時間を元にタイムスケールを戻す
        yield return new WaitForSecondsRealtime(_returnTimeScaleTime);
        Time.timeScale = 1;
        _hitStopCoroutine = null;
    }

    private void OnDisable()
    {
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = null;
        }
        if (Time.timeScale == 0.0f)
        {
            Time.timeScale = 1.0f;
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0.0f)
        {
            Time.timeScale = 1.0f;
        }
    }
}
