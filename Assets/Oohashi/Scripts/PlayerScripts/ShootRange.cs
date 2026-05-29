using UnityEngine;

public class ShootRange : MonoBehaviour
{
    #region 変数
    [SerializeField, Header("見てる方向のスクリプト")]
    private PlayerAiming _playerAiming = default;

    [SerializeField, Header("ステートのスクリプト")]
    private PlayerStateManager _stateManager = default;

    [SerializeField, Header("最大射程")]
    private int _maxRange = 15;

    [SerializeField, Header("最低射程")]
    private int _minRange = 7;
    public int MinRange
    {
        get { return _minRange; }
    }

    public int MaxRange
    {
        get { return _maxRange; }
    }

    [SerializeField, Header("最大チャージの最低射角")]
    private float _minAngle = 15;
    public float MinAngle
    {
        get { return _minAngle; }
    }
    [SerializeField, Header("ノーチャージの最大角度")]
    private float _maxAngle = 90;
    public float MaxAngle
    {
        get { return _maxAngle; }
    }

    private float _originMaxAngle = default;
    //表示用に返す最大角度
    private float _viewMaxAngle = default;
    public float ViewAngle
    {
        get { return _viewMaxAngle; }
    }
    //チャージ時間
    private float _chargeTime = 0f;
    //射程距離
    private float _range = 0;
    //Lerpの補正値
    private float _t = 0;

    //横幅
    private float _width = 0;


    [SerializeField, Header("最大チャージの時の振れ幅、最低角度に足す数字")]
    private float _swingWidth = 5;

    [SerializeField, Header("最大チャージの時の揺れる速度")]
    private float _swingTime = 10;

    [SerializeField, Header("弾が消えるエフェクト")]
    private GameObject _destroyEffect = default;

    private bool _isHit = false;
    public bool IsHit
    {
        get { return _isHit; }
    }

    private bool _canCharge = true;

    private bool _didSwingStart = false;
    private float _swingStartTime = 0f;


    #endregion

    #region 定数
    private readonly string STONETAGNAME = "Stone";
    private readonly string ARMORTAGNAME = "Armor";
    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";
    private readonly string BOSSTAGNAME = "Boss";
    private readonly string NORMALBULLETTAGNAME = "EnemyBullet";
    private readonly string MIRRORBULLETTAGNAME = "MirrorBullet";
    #endregion

    private void Start()
    {
        _originMaxAngle = _maxAngle;
        _viewMaxAngle = _maxAngle;
    }

    public void StartCharge()
    {
        //if (!_canCharge)
        //{
        //    return;
        //}
        //_chargeStartTime = Time.time;
        //_canCharge = false;
    }

    /// <summary>
    /// 最大射角及び射程距離を計算するメソッド
    /// </summary>
    /// <param name="chargeTime">現在のチャージ時間</param>
    /// <returns>射角を返す</returns>
    public float CalcChargeAngle(float chargeTime)
    {
        //チャージ時間を代入
        _chargeTime = chargeTime;
        //チャージ時間を2で割ってそれを0か1で補正値を代入
        _t = Mathf.Clamp01(chargeTime / 2f);
        //射程距離をLerpで変更する
        _range = Mathf.Lerp(_minRange, _maxRange, _t);
        //float halfAngleRad = Mathf.Deg2Rad * (_maxAngle / 2);
        if (chargeTime < 2)
        {
            //チャージ2秒未満の場合は最大角度から最低角度まで下げていく
            _viewMaxAngle = Mathf.Lerp(_originMaxAngle, _minAngle, _t) ;
            //_viewMaxAngle = _maxAngle;
        }
        else
        {
            if (!_didSwingStart)
            {
                _swingStartTime = Time.time;
                _didSwingStart = true;
            }

            float elapsed = Time.time - _swingStartTime;

            _viewMaxAngle = _minAngle + Mathf.PingPong(elapsed * _swingTime, _swingWidth);
            //_viewMaxAngle = _minAngle + Mathf.PingPong(Time.time * _swingTime, _swingWidth);
            //2秒以上だったら最低角度をピンポンで大きくしたり小さくしたりする
            //_minAngle += Mathf.PingPong(Time.time * _swingTime, _swingWidth);
            //_maxAngle = _minAngle + Mathf.PingPong(_chargeStartTime * _swingTime, _swingWidth); 
            //_viewMaxAngle = _minAngle;
            //halfAngleRad = Mathf.Deg2Rad * (_minAngle / 2);
        }
        //半分の角度の値を度からラジアンに変換して渡す
        //当たり判定の横幅を設定
        float halfAngleRad = Mathf.Deg2Rad * (_viewMaxAngle / 2);
        _width = Mathf.Tan(halfAngleRad) * _range;
        return _viewMaxAngle;
    }

    private void Update()
    {
        _isHit = false;
    }

    /// <summary>
    /// 当たり判定の処理を行う
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="isCritical">クリティカルショットかどうか</param>
    public void ShotgunHitCheck(float chargeTime,bool isCritical)
    {
        //当たり判定の中心
        Vector2 center = (Vector3)transform.position + _playerAiming.Direction.normalized * (_range / 2);
        //箱のサイズ
        Vector2 size = new Vector2(_width * 2, _range);
        //プレイヤーが見てる方向をの角度(度数)を求める
        float angle = Mathf.Atan2(_playerAiming.Direction.y, _playerAiming.Direction.x) * Mathf.Rad2Deg - 90;
        //当たり判定のボックスの中に入っているオブジェクトを配列に登録
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);
        //コライダー配列の中に入っているオブジェクトを探索
        foreach (Collider2D hit in hits)
        {
            bool isHit = false;
            //ヒットしたオブジェクトを登録
            GameObject hitObject = hit.gameObject;
            if (hitObject.CompareTag(STONETAGNAME))
            {
                StoneBlowAway(hitObject, chargeTime);
            }
            else if (hitObject.CompareTag(ARMORTAGNAME) || hitObject.CompareTag(MEDIUMARMORTAGNAME))
            {
                ArmorHitObject(hitObject, chargeTime,isCritical);
                isHit = true;
            }
            else if (hitObject.CompareTag(BOSSTAGNAME))
            {
                GameObject boss = hitObject;
                BossStateManagement bossStateManagement = boss.GetComponent<BossStateManagement>();
                isHit = true;
                BossHitObject(hitObject, chargeTime);
            }
            else if (hitObject.CompareTag(NORMALBULLETTAGNAME) || hitObject.CompareTag(MIRRORBULLETTAGNAME))
            {
                MirrorTheBullet(hitObject);
            }
            else
            {
                NormalHitObject(hitObject, chargeTime,isCritical);
                isHit = true;
            }

            _isHit = isHit;
        }
        _canCharge = true;
        _didSwingStart = false;
    }
    /// <summary>
    /// 銃弾を撃った時のメソッド、反射弾かどうかで処理が変わる
    /// </summary>
    /// <param name="hitObject">当たったオブジェクト</param>
    private void MirrorTheBullet(GameObject hitObject)
    {
        EnemyMirrorBullet mirror = hitObject.GetComponent<EnemyMirrorBullet>();
        //反射弾だった時の処理
        if (mirror != null)
        {
            mirror.DecBulletHP(_chargeTime, _playerAiming.Direction);
        }
        else //反射弾じゃなかったとき
        {
            Instantiate(_destroyEffect, hitObject.transform.position, Quaternion.identity);
            Destroy(hitObject);
        }
    }

    /// <summary>
    /// 岩を撃った時のメソッド
    /// </summary>
    /// <param name="hitObject">渡されたオブジェクト</param>
    /// <param name="chargeTime">チャージ時間</param>
    private void StoneBlowAway(GameObject hitObject, float chargeTime)
    {
        SetKnockBackStone stoneBlowAway = hitObject.GetComponent<SetKnockBackStone>();
        if (stoneBlowAway != null)
        {
            stoneBlowAway.SetDirectionAndForce((Vector2)transform.position, chargeTime,false,false);
        }
    }
    /// <summary>
    /// ボスを撃った時のメソッド
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
            bossHP.SetTakeDamege(chargeTime, _stateManager.PlayerState);
        }
    }
    /// <summary>
    /// 盾持ちに当たったときのメソッド
    /// </summary>
    /// <param name="hitObject">盾持ちのオブジェクト</param>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="isCritical">クリティカルかどうか</param>
    private void ArmorHitObject(GameObject hitObject, float chargeTime,bool isCritical)
    {
        ArmorKnockBack armorKnockback = hitObject.GetComponent<ArmorKnockBack>();
        ArmorTakeDamage armorTakeDamage = hitObject.GetComponent<ArmorTakeDamage>();
        if (armorKnockback != null)
        {
            armorKnockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, PlayerState.Normal,isCritical,false);
            armorTakeDamage.SetTakeDamege(chargeTime, _stateManager.PlayerState);
        }
    }

    /// <summary>
    /// 通常の敵に当たったときのメソッド
    /// </summary>
    /// <param name="hitObject">当たったオブジェクト</param>
    /// <param name="chargeTime">チャージ時間</param>
    private void NormalHitObject(GameObject hitObject, float chargeTime,bool isSpecial)
    {
        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (knockback != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, chargeTime,isSpecial,false);
            takeDamage.SetTakeDamege(chargeTime, _stateManager.PlayerState);
        }
    }

    /// <summary>
    /// デバッグ用に当たり判定表示
    /// </summary>
    private void OnDrawGizmos()
    {

        // プレイヤーの位置（このスクリプトがアタッチされてるオブジェクト）
        Vector3 origin = transform.position;

        Vector3 forward = new Vector3(_playerAiming.Direction.x, _playerAiming.Direction.y, 0).normalized;
        // プレイヤーの向き
        float halfAngleRad = Mathf.Deg2Rad * (_viewMaxAngle / 2);

        Vector3 right = Quaternion.Euler(0, 0, 90) * forward; // プレイヤーの右方向

        // 四角形の各頂点
        Vector3 p1 = origin + right * _width;
        Vector3 p2 = origin - right * _width;
        Vector3 p3 = p2 + forward * _range;
        Vector3 p4 = p1 + forward * _range;

        Gizmos.color = new Color(1, 0, 0, 0.4f); // 赤色（半透明）

        // 四角形の線を描く
        Gizmos.DrawLine(p1, p4);
        Gizmos.DrawLine(p4, p3);
        Gizmos.DrawLine(p3, p2);
        Gizmos.DrawLine(p2, p1);
    }
}

