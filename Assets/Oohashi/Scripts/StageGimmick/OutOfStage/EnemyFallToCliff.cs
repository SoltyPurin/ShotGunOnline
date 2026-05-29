using UnityEngine;

public class EnemyFallToCliff : EntityFall
{
    //落とし穴に落ちたら操作不可能＋初期地点(0,0)に移動
    [SerializeField] protected Animator _animator;
    [SerializeField] protected EnemyTakeDamage _enemyTakeDamage = default;
    protected SEManager _seManager = default;
    protected PlayTheBombEffect _bombEffect = default; //爆発エフェクト

    //---タグの名前---//
    private readonly string CLIFFTAGNAME = "Cliff";
    private readonly string EFFECTTAG = "EffectManager";
    private readonly string SETAG = "SEManager";
    //---アニメーターのトリガーの名前---//
    private readonly string FALLTRIGGER = "Fall";


    private void Start()
    {
        _bombEffect = GameObject.FindWithTag(EFFECTTAG).GetComponent<PlayTheBombEffect>();
        _seManager = GameObject.FindWithTag(SETAG).GetComponent<SEManager>();
    }

    public override void Fall()
    {
        FallMethod();
        _animator.enabled = true;
    }

    /// <summary>
    /// 死亡処理及びアニメーションとSE再生
    /// </summary>
    public virtual void FallMethod()
    {
        _seManager.PlayDropSound();
        if( _animator != null )
        {
            _animator.SetTrigger(FALLTRIGGER);
        }
        _enemyTakeDamage.FallDamage();
    }
}
