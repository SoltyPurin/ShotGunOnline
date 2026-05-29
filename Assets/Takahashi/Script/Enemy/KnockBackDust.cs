using UnityEngine;

/// <summary>
/// 敵がノックバックした時に出すエフェクトを管理するスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class KnockBackDust : MonoBehaviour
{
    [SerializeField, Header("スクリプト取得")]
    private EnemyMove _enemyMove = default;

    [SerializeField, Header("エフェクト取得")]
    private ParticleSystem _dustParticle = default;

    [Range(0f, 0.2f)]
    [SerializeField, Header("パーティクル発生頻度")]
    private float _dustFormationPeriod = default;

    private float _counter = default;

    public virtual void FixedUpdate()
    {
        //敵の状態によってエフェクトの再生を切り替える
        switch (_enemyMove.EnemyState)
        {
            //ノックバック状態ならエフェクト再生
            case EnemyState.knockback:
                PlayEffect();
                break;

            //それ以外ならエフェクトを停止
            case EnemyState.move:
                _dustParticle.Stop();
                break;

            case EnemyState.fall:
                _dustParticle.Stop();
                break;
        }
    }

    /// <summary>
    /// エフェクトを再生するメソッド
    /// </summary>
    public void PlayEffect()
    {
        if(_counter >= _dustFormationPeriod)
        {
            _dustParticle.Play();
            _counter = 0.0f;
        }

        _counter += Time.deltaTime;
    }
}
