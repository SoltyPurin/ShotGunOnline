using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BossFallToCliffAndHole : EnemyFallToCliff
{
    [SerializeField, Header("復帰用アニメーター登録")]
    private Animator _returnAnim = default;

    [SerializeField, Header("アーマームーブ登録")]
    private ArmorMove _armorMove = default;

    [SerializeField, Header("Sprite補正スクリプト")]
    private KakudoAndZahyouKyousei _kyousei = default;

    [SerializeField, Header("復帰ポイント登録")]
    private GameObject _returnPointTransform = default;

    [SerializeField, Header("復帰ポイントの名前")]
    private string _returnPointName = "SpawnPoint2";

    private BossStateManagement _state = default;

    // 登場直後の落下防止ガード
    [SerializeField, Header("登場直後の無敵時間（秒）")]
    private float _spawnProtectTime = 2.0f;
    private float _spawnTimer = 0f;

    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";
    private readonly string RETURNFALL = "IsFall";

    private void Start()
    {
        _state = GetComponent<BossStateManagement>();
        FindReturnPoint();
    }

    private void OnEnable()
    {
        _spawnTimer = 0f;
        if (_state == null)
        {
            _state = GetComponent<BossStateManagement>();
        }
    }

    private void Update()
    {
        if (_spawnTimer < _spawnProtectTime)
        {
            _spawnTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// 落下実行
    /// </summary>
    public override void FallMethod()
    {
        if (_spawnTimer < _spawnProtectTime)
        {
            return;
        }

        // 既に死亡している場合は処理しない
        if (_enemyTakeDamage != null && _enemyTakeDamage.IsDead)
        {
            return;
        }

        if (_state == null)
        {
            _state = GetComponent<BossStateManagement>();
        }

        if (_state != null)
        {
            _state.FallStateChange(true);
        }

        if (_seManager != null)
        {
            _seManager.PlayDropSound();
        }

        if (_animator != null)
        {
            _animator.SetTrigger(FALLTRIGGER);
        }

        if (_enemyTakeDamage != null)
        {
            _enemyTakeDamage.FallDamage();
            // 落下ダメージで死亡した場合は復帰コルーチンを実行しない
            if (_enemyTakeDamage.IsDead)
            {
                return;
            }
        }

        if (this.gameObject.activeInHierarchy)
        {
            FindReturnPoint();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (IsServer)
                {
                    PlayFallPerformanceClientRpc();
                    StartCoroutine(HoleFallMethodServer());
                }
            }
            else
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

    [ClientRpc]
    private void PlayFallPerformanceClientRpc()
    {
        if (IsServer) return; // サーバー側では既に再生済み

        if (_state != null)
        {
            _state.FallStateChange(true);
        }

        if (_seManager != null)
        {
            _seManager.PlayDropSound();
        }

        if (_animator != null)
        {
            _animator.SetTrigger(FALLTRIGGER);
        }
    }

    /// <summary>
    /// 外部スクリプト（EnemyFall / MediumArmorTakeDamage）から文字列 "HoleFallMethod" で呼ばれる互換用コルーチン
    /// </summary>
    public IEnumerator HoleFallMethod()
    {
        FallMethod();
        yield break;
    }

    /// <summary>
    /// 復帰ポイントを名前から安全に取得（スペルミスやシーンごとの差異に対応）
    /// </summary>
    private void FindReturnPoint()
    {
        if (_returnPointTransform != null) return;

        if (!string.IsNullOrEmpty(_returnPointName))
        {
            _returnPointTransform = GameObject.Find(_returnPointName);
        }

        if (_returnPointTransform == null)
        {
            _returnPointTransform = GameObject.Find("SpawnPoint2") 
                ?? GameObject.Find("SpawonPoint2") 
                ?? GameObject.Find("BossReturnPoint") 
                ?? GameObject.Find("SpawnPoint");
        }
    }

    /// <summary>
    /// 落下後に復帰地点へテレポート・エフェクト再生するサーバー処理
    /// </summary>
    public IEnumerator HoleFallMethodServer()
    {
        yield return new WaitForSeconds(2f);

        if (this == null || !gameObject.activeInHierarchy) yield break;
        if (_enemyTakeDamage != null && _enemyTakeDamage.IsDead) yield break;

        FindReturnPoint();

        Vector3 returnPos = _returnPointTransform != null 
            ? _returnPointTransform.transform.position 
            : new Vector3(0, 0, 0);

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(returnPos, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = returnPos;
            transform.rotation = Quaternion.identity;
        }

        Debug.Log("ボスリターン: " + returnPos);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            PlayReturnEffectClientRpc(returnPos);
        }
        else
        {
            PlayReturnEffectLocal(returnPos);
        }
    }

    [ClientRpc]
    private void PlayReturnEffectClientRpc(Vector2 targetPos)
    {
        PlayReturnEffectLocal(targetPos);
    }

    private void PlayReturnEffectLocal(Vector2 targetPos)
    {
        if (_returnAnim != null)
        {
            _returnAnim.SetTrigger(RETURNFALL);
        }

        if (_bombEffect != null)
        {
            _bombEffect.BombEffect(targetPos);
        }

        if (_kyousei != null)
        {
            _kyousei.RotateReset();
        }

        if (_state != null)
        {
            _state.FallStateChange(false);
        }
    }
}
