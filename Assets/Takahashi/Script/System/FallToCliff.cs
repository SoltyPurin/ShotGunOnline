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

    private void Awake()
    {
        // インスペクター未設定（Nothing=0）の場合は TileMap レイヤー(Layer 3)を既定値として設定
        if (_groundLayer.value == 0)
        {
            int tileMapLayer = LayerMask.NameToLayer("TileMap");
            _groundLayer = tileMapLayer >= 0 ? (1 << tileMapLayer) : (1 << 3);
        }
    }

    private void Start()
    {
        GameObject waveManObj = GameObject.FindWithTag("WaveManager");
        if (waveManObj != null)
        {
            _waveTransition = waveManObj.GetComponent<WaveTransition>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // ステージ切り替え等でこのMapオブジェクト自体が非アクティブ化された際のOnTriggerExit2Dではコルーチンを起動しない
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (collision == null || !collision.gameObject.scene.isLoaded)
        {
            return;
        }

        if (_waveTransition != null && _waveTransition.IsWaveChangeMoment)
        {
            return;
        }

        EntityFall entityFall = collision.GetComponent<EntityFall>();
        if (entityFall == null) return;

        // 1. プレイヤーの落下判定
        PlayerHoleFall playerFall = collision.GetComponentInParent<PlayerHoleFall>();
        if (playerFall != null)
        {
            StartCoroutine(PlayerFallCheck(collision, playerFall));
            return;
        }

        // 2. 敵（Layer 6: Enemy）の落下判定
        if (collision.gameObject.layer == 6)
        {
            StartCoroutine(DeathCheck(collision.gameObject, entityFall));
        }
        // 3. 岩（StoneFallToCliff）の落下判定
        else if (collision.GetComponent<StoneFallToCliff>() != null)
        {
            StartCoroutine(StoneFall(collision.gameObject, entityFall));
        }
    }

    private IEnumerator PlayerFallCheck(Collider2D playerCollider, PlayerHoleFall playerFall)
    {
        // 床の出現・Colliderの更新を待つ
        yield return new WaitForSeconds(_fallCheckDelay);

        if (playerCollider == null || playerFall == null || !playerCollider.gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (_waveTransition != null && _waveTransition.IsWaveChangeMoment)
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
        if (enemyTakeDamage == null)
        {
            yield break;
        }
        EnemyMove enemyMove = collision.GetComponent<EnemyMove>();
        if (enemyTakeDamage == null || enemyTakeDamage.IsDead) yield break;

        if (enemyMove != null && enemyMove.EnemyState == EnemyState.roadKill)
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
