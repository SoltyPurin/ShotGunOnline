using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFallToCliffAndHole : EnemyFallToCliff
{
    [SerializeField, Header("復帰用アニメーター登録")]
    private Animator _returnAnim = default;

    [SerializeField, Header("盾持ちのスクリプト登録")]
    private ArmorMove _armorMove = default;

    [SerializeField, Header("Sprite矯正スクリプト")]
    private KakudoKyousei _kyousei = default;

    [SerializeField, Header("復帰ポイント登録")]
    private GameObject _returnPointTransform = default;

    //そのままだと登場した瞬間に落下死するので無理やりガードする
    [SerializeField, Header("登場直後の落下無効時間（秒）")]
    private float _spawnProtectTime = 2.0f;
    private float _spawnTimer = 0f;

    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";
    private readonly string RETURNFALL = "IsFall";


    private void OnEnable()
    {
        // オブジェクトが有効化した時にタイマーリセット
        _spawnTimer = 0f;
    }

    private void Update()
    {
        if (_spawnTimer < _spawnProtectTime)
        {
            _spawnTimer += Time.deltaTime;
        }
    }
    /// <summary>
    /// 落下処理を行う
    /// </summary>
    public override void FallMethod()
    {
        if (_spawnTimer < _spawnProtectTime)
        {
            return;
        }
        _seManager.PlayDropSound();
        _animator.SetTrigger(FALLTRIGGER);
        _enemyTakeDamage.FallDamage();
        if (this.gameObject.activeInHierarchy)
        {
            if(_returnPointTransform == null)
            {
                _returnPointTransform = GameObject.Find("SpawonPoint2");
            }
            StartCoroutine(HoleFallMethod());
            transform.rotation = Quaternion.identity;
        }
        if (_armorMove != null)
        {
            _armorMove.ContactWithSomethingDuring();
        }
    }

    /// <summary>
    /// 落とし穴に落ちた後エフェクトを再生するメソッド
    /// </summary>
    /// <returns></returns>
    public IEnumerator HoleFallMethod()
    {
        //Animator retAnim = _returnPointTransform.GetComponent<Animator>();
        //retAnim.SetTrigger(RETURNPOINT);
        yield return new WaitForSeconds(2f);
        this.transform.position = _returnPointTransform.transform.position;
        Debug.Log("ボスリターン");
        if (_returnAnim != null)
        {
            _returnAnim.SetTrigger(RETURNFALL);
        }
        _bombEffect.BombEffect(this.transform.position);

        this.transform.rotation = Quaternion.identity;

        if (_kyousei != null)
        {
            _kyousei.RotateReset();
        }
    }

}
