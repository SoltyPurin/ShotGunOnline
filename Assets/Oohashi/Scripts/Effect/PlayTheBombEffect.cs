using System.Collections;
using UnityEngine;

public class PlayTheBombEffect : MonoBehaviour
{
    [SerializeField, Header("爆発のエフェクトのオブジェクトを登録")]
    private GameObject _bombEffectObject = default;
    [SerializeField, Header("砕ける岩のエフェクトのオブジェクト登録")]
    private GameObject _breakStoneObject = default;
    [SerializeField, Header("何秒後にエフェクトを破棄するか")]
    private float _destroyTime = 4f;

    /// <summary>
    /// 爆発のエフェクト再生
    /// </summary>
    /// <param name="bombPosition">爆発の起きたポイント、GroundZero</param>
    public void BombEffect(Vector2 bombPosition)
    {
        GameObject bomb = Instantiate(_bombEffectObject,(Vector3)bombPosition, Quaternion.identity);
        //ParticleSystem bombP = bomb.GetComponent<ParticleSystem>();
        //if ((bombP != null))
        //{
        //    bombP.Play();
        //}
        StartCoroutine(DestroyEffect(bomb));
    }

    /// <summary>
    /// 岩の破壊エフェクト再生
    /// </summary>
    /// <param name="breakPosition">岩が砕かれたポイント</param>
    public void BreakEffect(Vector2 breakPosition)
    {
        GameObject stoneBreak = Instantiate(_breakStoneObject,(Vector3)breakPosition, Quaternion.identity);
        ParticleSystem stoneP = stoneBreak.GetComponent<ParticleSystem>();
        if((stoneBreak != null))
        {
            stoneP.Play();
        }
        StartCoroutine(DestroyEffect(stoneBreak));
    }

    private IEnumerator DestroyEffect(GameObject obj)
    {
        yield return new WaitForSecondsRealtime(_destroyTime);
        Destroy(obj);
    }
}
