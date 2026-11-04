using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;

public class EnemyFall : MonoBehaviour
{
    private readonly string PLAYERTAGNAME = "Player";
    private readonly string ENEMYBULLETNAME = "EnemyBullet";
    [SerializeField, Header("ギミックが湧くスクリプト")]
    private WaveGimmickSpawn _waveGimmick = default;

    [SerializeField, Header("床として判定するレイヤー")]
    private LayerMask _groundLayer;

    [SerializeField, Header("足元判定の横幅")]
    private float _groundCheckWidth = 0.5f;

    [SerializeField, Header("足元判定の高さ")]
    private float _groundCheckHeight = 0.1f;

    [SerializeField, Header("足元判定の待機時間")]
    private float _fallCheckDelay = 0.1f;



    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";
    //---敵のタグの名前---//
    private readonly string STONETAGNAME = "Stone";
    private readonly string BOSSTAGNAME = "Boss";
    private readonly string ARMORTAGNAME = "Armor";
    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";
    //---コルーチンの名前---//
    private readonly string RESPAWNFALLHOLE = "RespawnFallHole";
    private readonly string BOSSFALLCOROUTINE = "HoleFallMethod";


    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool cantFallObject = collision.gameObject.CompareTag(ENEMYBULLETNAME) || collision.gameObject.CompareTag(PLAYERTAGNAME);
        if (!cantFallObject)
        {
            FallCheckAndDoMethod(collision.gameObject.GetComponent<Collider2D>());
        }
    }

    /// <summary>
    /// 受け取ったオブジェクトを落下させる
    /// </summary>
    /// <param name="collision">落下させるオブジェクト</param>
    private IEnumerator FallCheckAndDoMethod(Collider2D collision)
    {
        // 床の出現・Colliderの更新を待つ
        yield return new WaitForSeconds(0.1f);

        // プレイヤーの現在のCollider情報を取得
        Bounds bounds = collision.bounds;

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

        EnemyKnockBack enKnockback = collision.gameObject.GetComponent<EnemyKnockBack>();
        //吹き飛び中であれば落下させる
        if (enKnockback.CanKnockBack)
        {
            enKnockback.Fall();
            collision.gameObject.transform.position = this.gameObject.transform.position;
            Animator animator = collision.gameObject.GetComponent<Animator>();
            animator.enabled = true;
            animator.SetTrigger(FALLTRIGGER);
            if (collision.gameObject.CompareTag(STONETAGNAME))
            {
                StartCoroutine(StoneDestroy(collision.gameObject));
            }
            else if (collision.gameObject.CompareTag(BOSSTAGNAME))
            {
                EnemyTakeDamage takeDamage = collision.gameObject.GetComponent<EnemyTakeDamage>();
                _waveGimmick.StartCoroutine(RESPAWNFALLHOLE);
                BossFallToCliffAndHole bossFall = collision.gameObject.GetComponent<BossFallToCliffAndHole>();
                bossFall.StartCoroutine(BOSSFALLCOROUTINE);
                takeDamage.FallDamage();
            }
            else
            {
                EnemyTakeDamage takeDamage = collision.gameObject.GetComponent<EnemyTakeDamage>();
                takeDamage.FallDamage();
            }
        }

        if(collision.CompareTag(ARMORTAGNAME) || collision.CompareTag(MEDIUMARMORTAGNAME))
        {
            enKnockback.Fall();
            collision.gameObject.transform.position = this.gameObject.transform.position;
            Animator animator = collision.gameObject.GetComponent<Animator>();
            animator.enabled = true;
            animator.SetTrigger(FALLTRIGGER);

            ArmorMove armorMove = collision.gameObject.GetComponent<ArmorMove>();
            if(armorMove != null)
            {
                armorMove.ContactWithSomethingDuring();
                RushArmorCollisionHole(armorMove,collision.gameObject);
            }
        }
    }

    /// <summary>
    /// 突撃中の盾持ちが落とし穴に触れた時のメソッド
    /// </summary>
    /// <param name="armorMove">盾持ちのステート取得</param>
    /// <param name="collision">盾持ちのゲームオブジェクト</param>
    private void RushArmorCollisionHole(ArmorMove armorMove,GameObject collision)
    {
        if(armorMove.InitState == ArmorState.Rush)
        {
            ArmorTakeDamage takeDamage = collision.gameObject.GetComponent<ArmorTakeDamage>();
            if(takeDamage != null)
            {
                takeDamage.FallDamage();
            }
        }
    }

    /// <summary>
    /// 岩の落下アニメーションを再生してから消すコルーチン
    /// </summary>
    /// <param name="hitObject">岩のゲームオブジェクト</param>
    /// <returns></returns>
    private IEnumerator StoneDestroy(GameObject hitObject)
    {
        yield return new WaitForSecondsRealtime(1);
        Destroy(hitObject.gameObject);
    }
}
