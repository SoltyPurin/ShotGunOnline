using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BossFallToCliffAndHole : EnemyFallToCliff
{
    [SerializeField, Header("復帰用アニメーター登録")]
    private Animator _returnAnim = default;

    [SerializeField, Header("盾持ちのスクリプト登録")]
    private ArmorMove _armorMove = default;

    [SerializeField, Header("Sprite矯正スクリプト")]
    private KakudoAndZahyouKyousei _kyousei = default;

    [SerializeField, Header("復帰ポイント登録")]
    private GameObject _returnPointTransform = default;

    [SerializeField,Header("復帰ポイントの名前")]
    private string _returnPointName = default;

    private BossStateManagement _state = default;

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

        _state = GetComponent<BossStateManagement>();
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
        _state.FallStateChange(true);
        _seManager.PlayDropSound();
        _animator.SetTrigger(FALLTRIGGER);
        _enemyTakeDamage.FallDamage();
        if (this.gameObject.activeInHierarchy)
        {
            if(_returnPointTransform == null)
            {
                _returnPointTransform = GameObject.Find(_returnPointName);
            }
            if (IsServer)
            {
                StartCoroutine(HoleFallMethodServer());
            }
            transform.rotation = Quaternion.identity;
        }
        if (_armorMove != null)
        {
            _armorMove.ContactWithSomethingDuring();
        }
    }

    /// <summary>
    /// 落とし穴に落ちた後エフェクトを再生するメソッド,
    ///鯖で実行した後全クライアントに共有
    /// </summary>
    /// <returns></returns>
    public IEnumerator HoleFallMethodServer()
    {
        //Animator retAnim = _returnPointTransform.GetComponent<Animator>();
        //retAnim.SetTrigger(RETURNPOINT);
        yield return new WaitForSeconds(2f);
        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(_returnPointTransform.transform.position, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = _returnPointTransform.transform.position;
            transform.rotation = Quaternion.identity;
        }
        //this.transform.position = _returnPointTransform.transform.position;
        Debug.Log("ボスリターン");

        PlayReturnEffectClientRpc(_returnPointTransform.transform.position);
    }

    [ClientRpc]
    private void PlayReturnEffectClientRpc(Vector2 targetPos)
    {
        if (_returnAnim != null)
        {
            _returnAnim.SetTrigger(RETURNFALL);
        }
        _bombEffect.BombEffect(targetPos);

        //this.transform.rotation = Quaternion.identity;

        if (_kyousei != null)
        {
            _kyousei.RotateReset();
        }

        _state.FallStateChange(false);
    }

}
