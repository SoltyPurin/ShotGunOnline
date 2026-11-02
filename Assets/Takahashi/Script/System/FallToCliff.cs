using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallToCliff : MonoBehaviour
{
    [SerializeField, Header("床として判定するレイヤー")]
    private LayerMask _groundLayer;

    [SerializeField, Header("床判定の幅")]
    private float _groundCheckWidth = 0.5f;

    [SerializeField, Header("床判定の高さ")]
    private float _groundCheckHeight = 0.1f;

    [SerializeField, Header("落下の待機時間")]
    private float _fallCheckDelay = 0.1f;

    private WaveTransition _waveTransition = default;

    private void Start()
    {
        GameObject waveManagerObj = GameObject.FindWithTag("WaveManager");
        if (waveManagerObj != null)
        {
            _waveTransition = waveManagerObj.GetComponent<WaveTransition>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == null || !collision.gameObject.scene.isLoaded)
        {
            return;
        }

        if (_waveTransition != null && _waveTransition.IsWaveChangeMoment)
        {
            return;
        }

        // プレイヤーの判定（親階層含む）
        PlayerHoleFall playerFall = collision.GetComponentInParent<PlayerHoleFall>();
        if (playerFall != null)
        {
            StartCoroutine(PlayerFallCheck(collision, playerFall));
            return;
        }

        EntityFall entityFall = collision.GetComponent<EntityFall>();
        if (entityFall == null) return;

        // 6番はEnemyのレイヤー番号
        if (collision.gameObject.layer == 6)
        {
            StartCoroutine(DeathCheck(collision.gameObject, entityFall));
            return;
        }

        // 石などの落下オブジェクト判定
        StoneFallToCliff stoneFall = collision.GetComponent<StoneFallToCliff>();
        if (stoneFall != null)
        {
            StartCoroutine(StoneFall(collision.gameObject, entityFall));
            return;
        }

        // その他の落下エンティティ
        entityFall.Fall();
    }

    private IEnumerator PlayerFallCheck(Collider2D playerCollider, PlayerHoleFall playerFall)
    {
        // 浮遊・Colliderの更新待ち
        yield return new WaitForSeconds(_fallCheckDelay);

        if (playerCollider == null || playerFall == null)
        {
            yield break;
        }

        // プレイヤーの現在のCollider取得
        Bounds bounds = playerCollider.bounds;

        // 床を調べるための判定範囲
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
            yield break;
        }

        playerFall.Fall();
    }

    private IEnumerator StoneFall(GameObject collision, EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;

        StoneFallToCliff stoneFall = collision.GetComponent<StoneFallToCliff>();
        if (stoneFall != null)
        {
            stoneFall.FallMethod();
        }

        if (entityFall != null)
        {
            entityFall.Fall();
        }
    }

    private IEnumerator DeathCheck(GameObject collision, EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;

        EnemyTakeDamage enemyTakeDamage = collision.GetComponent<EnemyTakeDamage>();
        if (enemyTakeDamage == null || enemyTakeDamage.IsDead)
        {
            yield break;
        }

        EnemyMove enemyMove = collision.GetComponent<EnemyMove>();
        if (enemyMove != null && enemyMove.EnemyState == EnemyState.roadKill)
        {
            yield break;
        }

        BombProtocol bomb = collision.GetComponent<BombProtocol>();
        if (bomb != null && bomb.IsExplosion)
        {
            yield break;
        }

        if (entityFall != null)
        {
            entityFall.Fall();
        }
    }
}
