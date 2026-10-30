using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FallToCliff : MonoBehaviour
{

    [SerializeField, Header("床として判定するレイヤー")]
    private LayerMask _groundLayer;

    [SerializeField, Header("足元判定の横幅")]
    private float _groundCheckWidth = 0.5f;

    [SerializeField, Header("足元判定の高さ")]
    private float _groundCheckHeight = 0.1f;

    [SerializeField, Header("足元判定の待機時間")]
    private float _fallCheckDelay = 0.1f;


    private WaveTransition _waveTransition = default;

    private void Start()
    {
        _waveTransition = GameObject.FindWithTag("WaveManager").GetComponent<WaveTransition>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.scene.isLoaded)
        {
            return;
        }

        if (collision == null) return;

        if (_waveTransition.IsWaveChangeMoment)
        {
            return;
        }

        EntityFall entityFall = collision.GetComponent<EntityFall>();
        if (entityFall == null) return;


        //6番はEnemyのレイヤー番号
        if (collision.gameObject.layer == 6)
        {
            StartCoroutine(DeathCheck(collision.gameObject,entityFall));
        }
        //else if (collision.CompareTag("Player"))
        //{
        //    PlayerDamageKnockBack damageKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
        //    if (damageKnockBack != null && damageKnockBack.gameObject.activeInHierarchy)
        //    {
        //        entityFall.Fall();
        //        //StartCoroutine(damageKnockBack.StartCoroutine(DamagePerformance());
        //        //damageKnockBack.StartCoroutine("");
        //    }
        //}
        else
        {
            
            StartCoroutine(StoneFall(collision.gameObject, entityFall));
        }

        PlayerHoleFall playerFall =
       collision.GetComponentInParent<PlayerHoleFall>();

        if (playerFall != null)
        {
            StartCoroutine(PlayerFallCheck(collision, playerFall));
            return;
        }


    }

    private IEnumerator PlayerFallCheck(
    Collider2D playerCollider,
    PlayerHoleFall playerFall)
    {
        // 床の出現・Colliderの更新を待つ
        yield return new WaitForSeconds(_fallCheckDelay);

        if (playerCollider == null || playerFall == null)
        {
            yield break;
        }

        // プレイヤーの現在のCollider情報を取得
        Bounds bounds = playerCollider.bounds;

        // 足元を調べるための判定範囲
        Vector2 checkPosition = new Vector2(
            bounds.center.x,
            bounds.min.y - 0.02f
        );

        Vector2 checkSize = new Vector2(
            Mathf.Min(bounds.size.x, _groundCheckWidth),
            _groundCheckHeight
        );

        // 現在の足元に床があるか確認
        Collider2D ground = Physics2D.OverlapBox(
            checkPosition,
            checkSize,
            0f,
            _groundLayer
        );

        if (ground != null)
        {
            Debug.Log($"床を検知したため落下キャンセル: {ground.name}");
            yield break;
        }

        Debug.Log("足元に床がないため落下");

        playerFall.Fall();
    }
    private IEnumerator StoneFall(GameObject collision,EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;
        StoneFallToCliff stoneFall = collision.GetComponent<StoneFallToCliff>();
        stoneFall.FallMethod();
        entityFall.Fall();

    }

    private IEnumerator DeathCheck(GameObject collision,EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;
        EnemyTakeDamage enemyTakeDamage = collision.GetComponent<EnemyTakeDamage>();
        if(enemyTakeDamage == null)
        {
            yield break;
        }
        EnemyMove enemyMove = collision.GetComponent<EnemyMove>();
        if (enemyTakeDamage == null || enemyTakeDamage.IsDead) yield break;

        if (enemyMove.EnemyState == EnemyState.roadKill)
        {
            yield break;
        }

        BombProtocol bomb = collision.GetComponent<BombProtocol>();
        if (bomb != null && bomb.IsExplosion)
        {
            yield break;
        }
        entityFall.Fall();


    }
}
