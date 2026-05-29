using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneFallToCliff : EnemyFallToCliff
{

    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";

    /// <summary>
    /// 落下のSEとアニメーション再生
    /// </summary>
    public override void FallMethod()
    {
        _seManager.PlayDropSound();
        _animator.SetTrigger(FALLTRIGGER);
        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(DestroyStone());
        }
    }
    /// <summary>
    /// 岩を消す
    /// </summary>
    /// <returns></returns>
    private IEnumerator DestroyStone()
    {
        yield return new WaitForSeconds(2);
        Destroy(this.gameObject);
    }
}
