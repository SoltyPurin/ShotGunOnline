using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHoleFall : EntityFall
{
    //落とし穴に落ちたら操作不可能＋初期地点(0,0)に移動
    [SerializeField] private Animator _animator;
    [SerializeField] private SoulKeep _soul = default;
    [SerializeField] private PlayerStateManager _playerStateManager = default;
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField] private GameObject _playerImage = default;
    [SerializeField] private GameObject _dropCoinObject = default;
    [SerializeField, Header("リジッドボディを取得")]
    private Rigidbody2D _rigidbody2D = default;
    [SerializeField, Header("ショットガンの吹き飛び")]
    private PlayerKnockBack _knockBack = default;
    [SerializeField, Header("ダメージの吹き飛び")]
    private PlayerDamageKnockBack _damageKnockBack = default;
    [SerializeField, Header("SEマネージャー")]
    private SEManager _seManager = default;
    [SerializeField, Header("リスポーンのスクリプト")]
    private PlayerRespawn _respawn = default;
    [SerializeField, Header("ステートチェンジ用スクリプト")]
    private InputChangeState _inputChangeState = default;

    [Header("アニメーター")]
    [SerializeField, Tooltip("フェードのアニメーター（PlayerUI内にある）")]
    private Animator _fadeAnimator = default;

    private PlayerDamageKnockBack _damage = default;

    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";

    private void Start()
    {
        _soul = GetComponent<SoulKeep>();
        _damage = GetComponent<PlayerDamageKnockBack>();
    }



    /// <summary>
    /// リスポーン座標に移動するコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator MoveToRespawnPoint()
    {
        transform.SetParent(null);
        yield return new WaitForSeconds(2.2f);
        _rigidbody2D.simulated = true;
        _fadeAnimator.SetTrigger("FadeIn");
        _damageKnockBack.ResetForce();
        _respawn.Respawn();
        _playerStateManager.NormalState();
        _inputChangeState.ToNormalAnimation();
    }

    /// <summary>
    /// 落下メソッド
    /// </summary>
    public override void Fall()
    {
        if(_playerStateManager.PlayerState == PlayerState.Movie)
        {
            return;
        }

        _rigidbody2D.simulated = false;
        _fadeAnimator.SetTrigger("FadeOut");
        _seManager.PlayDropSound();
        _knockBack.Fall();
        _damageKnockBack.Fall();
        _animator.SetTrigger(FALLTRIGGER);
        _playerStateManager.FallState();
        StartCoroutine(MoveToRespawnPoint());
        //無敵時間中はコインを減らさない
        if (_damageKnockBack.IsInvincible)
        {
            return;
        }

        StartCoroutine(_damageKnockBack.DamagePerformance());
        //int ultSoul = _soul.VStock;
        //int soul = _soul.UseFullSoul;

        //int dropCoinCount = 0;

        //if (ultSoul >= 1)
        //{
        //    Debug.Log("vストックあり");
        //    if (soul >= 1)
        //    {
        //        dropCoinCount = soul;
        //    }
        //    else
        //    {
        //        dropCoinCount = 10;
        //    }
        //}
        //else
        //{
        //    Debug.Log("vストックなし");
        //    if (soul >= 1)
        //    {
        //        dropCoinCount = soul;
        //    }
        //}
        //for (int i = 0; i < dropCoinCount; i++)
        //{
        //    GameObject coin = Instantiate(_dropCoinObject, transform.position, Quaternion.identity);
        //    coin.GetComponent<BoxCollider2D>().enabled = false;
        //    MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
        //    if (moveCoin != null)
        //    {
        //        moveCoin.MoveStart();
        //    }
        //}

        //_soul.ReduceCoin();


    }
}
