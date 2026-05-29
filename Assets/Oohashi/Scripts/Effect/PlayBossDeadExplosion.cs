using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayBossDeadExplosion : MonoBehaviour
{
    [SerializeField,Header("1回目爆発のエフェクト登録用リスト")]
    private List<ParticleSystem> _firstExplosion = new List<ParticleSystem>();
    [SerializeField, Header("2回目爆発のエフェクト登録用リスト")]
    private List<ParticleSystem> _secondExplosion = new List<ParticleSystem>();

    [SerializeField, Header("ボス撃破時のスロー演出のタイムスケール")]
    private float _slowTimeScale = 0.5f;

    /// <summary>
    /// 一回目のボスの爆死エフェクト再生
    /// </summary>
    /// <param name="position">ボスが死んだ場所</param>
    public void Explosion(Vector2 position)
    {
        Time.timeScale = _slowTimeScale;
        this.transform.position = position;
        for(int i = 0; i< _firstExplosion.Count; i++)
        {
            _firstExplosion[i].Play();
        }
        StartCoroutine(SecondExplosion());
    }

    /// <summary>
    /// 二回目の爆発のコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator SecondExplosion()
    {
        yield return new WaitForSecondsRealtime(1);
        for(int i = 0; i<_secondExplosion.Count; i++)
        {
            _secondExplosion[i].Play();
        }
        Time.timeScale = 1;
    }
}
