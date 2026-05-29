using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMirrorBullet : EnemyShotBullet
{
    private float _initTime = 0;

    private bool _isMirroring = false;

    private float _chargeTime = default;

    [SerializeField, Header("弾の内部HP")]
    protected float _bulletHP = 0.1f;

    [SerializeField, Header("吹き飛ばしの乗算値")]
    private float _forceMultiplier = 4;

    private readonly string STONETAGNAME = "Stone";

    private readonly string ARMORTAGNAME = "Armor";

    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";

    private readonly string BOSSTAGNAME = "Boss";

    /// <summary>
    /// 打った時に銃弾の内部HPを減算する
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="playerDirection">プレイヤーの見てる方向、反射に使う</param>
    public virtual void DecBulletHP(float chargeTime,Vector2 playerDirection)
    {
        _bulletHP -= chargeTime;
        if (_bulletHP <= 0)
        {
            Mirror(chargeTime, playerDirection);
        }
    }
    /// <summary>
    /// 反射のメソッド
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="playerDirection">プレイヤーの見てる方向</param>
    public void Mirror(float chargeTime,Vector2 playerDirection)
    {
        _isMirroring = true;
        _direction = playerDirection;
        _bulletLifeLimitTime = 3;
        //チャージ時間の乗を求める
        float powValue = Mathf.Pow(chargeTime, _forceMultiplier);
        //最低6からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
        float force = Mathf.Lerp(6, powValue, 0.5f);
        _moveSpeed *= force;
        _chargeTime = chargeTime;
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        GameObject hitObject = collision.gameObject;
        if (_isMirroring)
        {
            if (collision.CompareTag(STONETAGNAME))
            {
                StoneBlowAway(hitObject, _chargeTime);

            }
            else if (collision.CompareTag(ARMORTAGNAME) || collision.CompareTag(MEDIUMARMORTAGNAME))
            {
                ArmorHitObject(hitObject, _chargeTime);
            }
            else if (collision.CompareTag(BOSSTAGNAME))
            {
                BossHitObject(hitObject, _chargeTime);
            }
            else
            {
                NormalHitObject(hitObject, _chargeTime);
            }
        }
    }

    /// <summary>
    /// 反射して岩に当たった場合のメソッド
    /// </summary>
    /// <param name="hitObject">岩のオブジェクト</param>
    /// <param name="chargeTime">チャージ時間</param>
    private void StoneBlowAway(GameObject hitObject, float chargeTime)
    {
        SetKnockBackStone stoneBlowAway = hitObject.GetComponent<SetKnockBackStone>();
        if(stoneBlowAway != null)
        {
            stoneBlowAway.SetDirectionAndForce((Vector2)transform.position, chargeTime,false,false);
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 反射してボスに当たった場合のメソッド
    /// </summary>
    /// <param name="hitObject">ボスのオブジェクト</param>
    /// <param name="chargeTime">チャージ時間</param>
    private void BossHitObject(GameObject hitObject, float chargeTime)
    {
        BossHP bossHP = hitObject.GetComponent<BossHP>();
        BossKnockBack knockback = hitObject.GetComponent<BossKnockBack>();
        if (knockback != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, false, false);
            bossHP.SetMirrorDamage(chargeTime);
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 反射してアーマーに当たった時のメソッド
    /// </summary>
    /// <param name="hitObject"></param>
    /// <param name="chargeTime"></param>
    private void ArmorHitObject(GameObject hitObject, float chargeTime)
    {
        ArmorKnockBack armorKnockback = hitObject.GetComponent<ArmorKnockBack>();
        ArmorTakeDamage armorTakeDamage = hitObject.GetComponent<ArmorTakeDamage>();
        if(armorKnockback != null)
        {
            armorKnockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, PlayerState.Normal, false, false);
            armorTakeDamage.SetMirrorDamage(chargeTime);
            Destroy(this.gameObject);
        }
    }

    private void NormalHitObject(GameObject hitObject, float chargeTime)
    {
        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (knockback != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, false, false);
            takeDamage.SetMirrorDamage(chargeTime);
            Destroy(this.gameObject);
        }
    }


}
