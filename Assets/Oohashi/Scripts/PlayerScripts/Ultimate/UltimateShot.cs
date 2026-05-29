using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateShot : MonoBehaviour
{
    //カメラの揺れとヒットストップは当たった時だけ
    //ブルームやコインを減らすのは当たってなくても実行
    [SerializeField,Header("狙ってる方向のスクリプト")]
    private PlayerAiming _playerAiming = default;
    [SerializeField,Header("所有してるコインのスクリプト")]
    private SoulKeep _coinKeep = default;
    [SerializeField,Header("プレイヤーのステート")] 
    private PlayerStateManager _stateManager = default;
    [SerializeField,Header("ヒットストップのスクリプト")]
    private HitStop _hitStop = default;
    [SerializeField,Header("カメラが揺れるスクリプト")]
    private ScreenVibration _shake = default;
    [SerializeField,Header("ウルトのブルーム効果演出のスクリプト")]
    private BloomScript _ultBloom = default;
    [SerializeField, Header("銃弾が消えるときのエフェクト")]
    private GameObject _destroyEffect = default;

    private readonly string ARMORTAGNAME = "Armor";
    private readonly string STONETAGNAME = "Stone";
    private readonly string BOSSTAGNAME = "Boss";
    private readonly string ENEMYBULLETTAGNAME = "EnemyBullet";
    private readonly string MIRRORBULLETTAGNAME = "MirrorBullet";

    [SerializeField, Header("反射弾に与える力")]
    private float _giveForceOfMirroBullet = 1.4f;

    [SerializeField, Header("ウルト本体の吹き飛ばし力")]
    private float _ultimateForce = 2.5f;

    // 射程固定
    private float _radius = 20f;
    // 前方90度以内だけ
    private float _maxAngle = 90;

    /// <summary>
    /// ウルトの範囲内にいる敵を確認及びそれに対してアクションを起こすメソッド
    /// </summary>
    public void UltimateShotProtocol()
    {
        _ultBloom.UseUltimate();

        //判定内にいたオブジェクト全てを一旦収容
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);

        foreach (Collider2D hit in hits)
        {
            //ターゲットのいる方向
            Vector2 directionToTarget = (hit.transform.position - transform.position).normalized;
            //正面をプレイヤーの見ている方向と定める
            Vector2 forward = _playerAiming.Direction;
            //敵のいる角度を求める
            float angle = Vector2.Angle(forward, directionToTarget);
            //ターゲットが射角(maxAngle)の中央方向±(maxAngle/2)以内にいるか判定
            if (angle <= _maxAngle / 2)
            {
                GameObject hitObject = hit.gameObject;
                if (hitObject.CompareTag(ARMORTAGNAME))
                {
                    ArmorHitObject(hitObject, _ultimateForce);
                }
                else if(hitObject.CompareTag(STONETAGNAME))
                {
                    StoneHit(hitObject, _ultimateForce);
                }
                else if(hitObject.CompareTag(BOSSTAGNAME))
                {
                    BossHit(hitObject, _ultimateForce);
                }
                else if (hitObject.CompareTag(ENEMYBULLETTAGNAME) || hitObject.CompareTag(MIRRORBULLETTAGNAME))
                {
                    MirrorTheBullet(hitObject);
                }
                else
                {
                    NormalHitObject(hitObject, _ultimateForce,true);
                }
            }
        }
    }

    /// <summary>
    /// 銃弾のヒット処理、反射弾だった場合は処理を変更する
    /// </summary>
    /// <param name="hitObject">ウルトに引っかかったオブジェクト</param>
    private void MirrorTheBullet(GameObject hitObject)
    {
        EnemyMirrorBullet mirror = hitObject.GetComponent<EnemyMirrorBullet>();
        if (mirror != null)
        {
            //反射弾にある程度の力を与えて反射する
            mirror.DecBulletHP(_giveForceOfMirroBullet, _playerAiming.Direction);
        }
        else
        {
            //反射弾でない場合はエフェクトを沸かせてデストロイする
            Instantiate(_destroyEffect, hitObject.transform.position, Quaternion.identity);
            Destroy(hitObject);
        }
    }

    /// <summary>
    /// ボスが判定に引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">ボスのオブジェクト</param>
    /// <param name="force">与える力</param>
    private void BossHit(GameObject hitObject, float force)
    {
        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (knockback != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, force-0.5f, false,true);
            takeDamage.SetTakeDamege(force, PlayerState.Ultimate);
            _hitStop.UltHitStopMethod();
            _shake.CanShake = true;
            StartCoroutine(ShakeStop());
        }
    }

    /// <summary>
    /// 岩が引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">岩のオブジェクト</param>
    /// <param name="force">与える力</param>
    private void StoneHit(GameObject hitObject, float force)
    {
        SetKnockBackStone stoneKnockBack = hitObject.GetComponent<SetKnockBackStone>();
        if(stoneKnockBack != null)
        {
            stoneKnockBack.SetDirectionAndForce((Vector2)transform.position, force,false, false);
            _hitStop.UltHitStopMethod();
            _shake.CanShake = true;
            StartCoroutine(ShakeStop());
        }
    }

    /// <summary>
    /// 特殊な処理がない敵に当たった時の処理を行うメソッド
    /// </summary>
    /// <param name="hitObject">敵のオブジェクト</param>
    /// <param name="force">与える力</param>
    private void NormalHitObject(GameObject hitObject, float force,bool isSpecial)
    {
        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (knockback != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, force,false, true);
            takeDamage.SetTakeDamege(force, PlayerState.Ultimate);
            _hitStop.UltHitStopMethod();
            _shake.CanShake = true;
            StartCoroutine(ShakeStop());
        }
    }

    /// <summary>
    /// 盾持ちが引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">盾持ちのオブジェクト</param>
    /// <param name="force">与える力</param>
    private void ArmorHitObject(GameObject hitObject, float force)
    {
        ArmorKnockBack armorKnockback = hitObject.GetComponent<ArmorKnockBack>();
        ArmorTakeDamage armorTakeDamage = hitObject.GetComponent<ArmorTakeDamage>();
        armorKnockback.SetDirectionAndForce((Vector2)transform.position, force,PlayerState.Ultimate,false,true);
        armorTakeDamage.SetTakeDamege(force, _stateManager.PlayerState);
        _hitStop.UltHitStopMethod();
        _shake.CanShake = true;
        StartCoroutine(ShakeStop());
    }

    /// <summary>
    /// 画面の揺れを止めるコルーチン
    /// </summary>
    /// <returns>長すぎると酔うので1秒で停止</returns>
    private IEnumerator ShakeStop()
    {
        yield return new WaitForSecondsRealtime(1f);
        _shake.CanShake = false;
    }

    /// <summary>
    /// ウルトのデバッグ当たり判定を表示
    /// </summary>
    private void OnDrawGizmos()
    {

        // プレイヤーの位置（このスクリプトがアタッチされてるオブジェクト）
        Vector3 origin = transform.position;

        // プレイヤーの向き
        Vector3 forward = transform.right; // 方向は場合によって変更
        float radius = 20;       // 射程固定
        float maxAngle = 90f;     // 前方90度以内だけ

        int segments = 30; // 扇形を何分割するか（多いほど滑らか）

        Gizmos.color = new Color(1, 0, 0, 0.4f); // 赤、透明

        Vector3 prevPoint = origin;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -maxAngle / 2 + (maxAngle / segments) * i;
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 dir = rot * forward;
            Vector3 point = origin + dir.normalized * radius;

            // 線を描く
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }

            prevPoint = point;
        }

        // 中心から両端にも線を引く
        Vector3 left = Quaternion.Euler(0, 0, -maxAngle / 2) * forward * radius;
        Vector3 right = Quaternion.Euler(0, 0, maxAngle / 2) * forward * radius;
        Gizmos.DrawLine(origin, origin + left);
        Gizmos.DrawLine(origin, origin + right);
    }

}
