using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";
    private readonly string STONETAGNAME = "Stone";
    private readonly string BOSSTAGNAME = "Boss";
    private readonly string ENEMYBULLETTAGNAME = "EnemyBullet";
    private readonly string MIRRORBULLETTAGNAME = "MirrorBullet";

    private readonly HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

    [SerializeField, Header("反射弾に与える力")]
    private float _giveForceOfMirroBullet = 1.4f;

    [SerializeField, Header("ウルト本体の吹き飛ばし力")]
    private float _ultimateForce = 2.5f;

    // 射程固定
    private float _radius = 20f;
    // 前方90度以内だけ
    private float _maxAngle = 90;

    private void Start()
    {
        if(_shake == null && _ultBloom == null)
        {
            var mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null) _shake = mainCam.GetComponent<ScreenVibration>();
            var postMgr = GameObject.Find("PostManager");
            if (postMgr != null) _ultBloom = postMgr.GetComponent<BloomScript>();
        }
    }

    /// <summary>
    /// ウルトの範囲内にいる敵を確認及びそれに対してアクションを起こすメソッド
    /// </summary>
    public void UltimateShotProtocol()
    {
        if (_ultBloom != null) _ultBloom.UseUltimate();
        if (_playerAiming == null) return;

        //判定内にいたオブジェクト全てを一旦収容
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        _hitTargets.Clear();

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.gameObject == this.gameObject || hit.transform.IsChildOf(this.transform))
            {
                continue;
            }

            NetworkObject parentNetObj = hit.GetComponentInParent<NetworkObject>();
            GameObject hitObject = parentNetObj != null ? parentNetObj.gameObject : hit.gameObject;
            if (hitObject == this.gameObject || !_hitTargets.Add(hitObject))
            {
                continue;
            }

            //ターゲットのいる方向
            Vector2 directionToTarget = (hitObject.transform.position - transform.position).normalized;
            //正面をプレイヤーの見ている方向と定める
            Vector2 forward = _playerAiming.Direction;
            //敵のいる角度を求める
            float angle = Vector2.Angle(forward, directionToTarget);
            //ターゲットが射角(maxAngle)の中央方向±(maxAngle/2)以内にいるか判定
            if (angle <= _maxAngle / 2)
            {
                if (hitObject.CompareTag(ARMORTAGNAME) || hitObject.CompareTag(MEDIUMARMORTAGNAME))
                {
                    ArmorHitObject(hitObject, _ultimateForce);
                }
                else if (hitObject.CompareTag(STONETAGNAME))
                {
                    StoneHit(hitObject, _ultimateForce);
                }
                else if (hitObject.CompareTag(BOSSTAGNAME))
                {
                    BossHit(hitObject, _ultimateForce);
                }
                else if (hitObject.CompareTag(ENEMYBULLETTAGNAME) || hitObject.CompareTag(MIRRORBULLETTAGNAME))
                {
                    MirrorTheBullet(hitObject);
                }
                else
                {
                    NormalHitObject(hitObject, _ultimateForce, true);
                }
            }
        }
    }

    private bool IsTargetValid(GameObject hitObject, EnemyTakeDamage takeDamage = null)
    {
        if (hitObject == null) return false;
        if (hitObject.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned)
        {
            return false;
        }
        if (takeDamage == null)
        {
            takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        }
        if (takeDamage != null && takeDamage.IsDead)
        {
            return false;
        }
        return true;
    }

    private void TriggerUltImpactFeedback()
    {
        if (_hitStop != null)
        {
            _hitStop.UltHitStopMethod();
        }
        if (_shake != null)
        {
            _shake.CanShake = true;
            StartCoroutine(ShakeStop());
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
            EnemyShotBullet bullet = hitObject.GetComponent<EnemyShotBullet>();
            if (bullet != null)
            {
                bullet.DestroyProtocol();
            }
            else
            {
                Destroy(hitObject);
            }
        }
    }

    /// <summary>
    /// ボスが判定に引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">ボスのオブジェクト</param>
    /// <param name="force">与える力</param>
    private void BossHit(GameObject hitObject, float force)
    {
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (!IsTargetValid(hitObject, takeDamage)) return;

        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        if (knockback != null)
        {
            // ボスはノックバック力だけ force - 0.5f、ダメージ計算は force
            knockback.RequestKnockBackServerRpc((Vector2)transform.position, force - 0.5f, false, true);
            if (takeDamage != null)
            {
                takeDamage.RequestTakeDamageServerRpc(force, PlayerState.Ultimate);
            }
            knockback.PredictLocalKnockBack((Vector2)transform.position, force - 0.5f, PlayerState.Ultimate, false, true);
            TriggerUltImpactFeedback();
        }
    }

    /// <summary>
    /// 岩が引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">岩のオブジェクト</param>
    /// <param name="force">与える力</param>
    private void StoneHit(GameObject hitObject, float force)
    {
        if (!IsTargetValid(hitObject)) return;

        SetKnockBackStone stoneKnockBack = hitObject.GetComponent<SetKnockBackStone>();
        if (stoneKnockBack != null)
        {
            stoneKnockBack.RequestKnockBackServerRpc((Vector2)transform.position, force, false, false);
            TriggerUltImpactFeedback();
        }
    }

    /// <summary>
    /// 特殊な処理がない敵に当たった時の処理を行うメソッド
    /// </summary>
    /// <param name="hitObject">敵のオブジェクト</param>
    /// <param name="force">与える力</param>
    private void NormalHitObject(GameObject hitObject, float force, bool isSpecial)
    {
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (!IsTargetValid(hitObject, takeDamage)) return;

        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        if (knockback != null)
        {
            knockback.RequestHitServerRpc((Vector2)transform.position, force, PlayerState.Ultimate, false, true);
            knockback.PredictLocalKnockBack((Vector2)transform.position, force, PlayerState.Ultimate, false, true);
            TriggerUltImpactFeedback();
        }
    }

    /// <summary>
    /// 盾持ちが引っかかった時のメソッド
    /// </summary>
    /// <param name="hitObject">盾持ちのオブジェクト</param>
    /// <param name="force">与える力</param>
    private void ArmorHitObject(GameObject hitObject, float force)
    {
        ArmorTakeDamage armorTakeDamage = hitObject.GetComponent<ArmorTakeDamage>();
        if (!IsTargetValid(hitObject, armorTakeDamage)) return;

        ArmorKnockBack armorKnockback = hitObject.GetComponent<ArmorKnockBack>();
        if (armorKnockback != null)
        {
            armorKnockback.RequestHitServerRpc((Vector2)transform.position, force, PlayerState.Ultimate, false, true);
            armorKnockback.PredictLocalKnockBack((Vector2)transform.position, force, PlayerState.Ultimate, false, true);
            TriggerUltImpactFeedback();
        }
    }

    /// <summary>
    /// 画面の揺れを止めるコルーチン
    /// </summary>
    /// <returns>長すぎると酔うので1秒で停止</returns>
    private IEnumerator ShakeStop()
    {
        yield return new WaitForSecondsRealtime(1f);
        if (_shake != null) _shake.CanShake = false;
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
