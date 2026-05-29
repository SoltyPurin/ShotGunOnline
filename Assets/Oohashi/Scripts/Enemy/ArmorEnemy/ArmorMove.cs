using System.Collections;
using UnityEngine;

public enum ArmorState
{
    Stop,
    Rush,
    Rotate,
    Back,
    Reservoir
}
public class ArmorMove : EnemyMove
{
    [SerializeField, Header("方向変換するスクリプト")]
    private ArmorRotation _armorRotation = default;
    //盾持ちの現在のステート
    private ArmorState _initState = ArmorState.Stop;
    public ArmorState InitState
    {
        get { return _initState; }
    }
    [SerializeField, Header("突進後の後隙")] 
    private float _recoveryTime = 5;
    public float RecoveryTime
    {
        get { return _recoveryTime; }
    }
    [SerializeField, Header("突進する時間")]
    private float _rushTime = 2;
    public float RushTime
    {
        get { return _rushTime; }
    }
    [SerializeField, Header("突撃するまでの待機時間")]
    private float _waitLimitTime = 5;
    public float WaitLimitTime
    {
        get { return _waitLimitTime; }
    }
    //突撃の経過時間
    private float _initRushTime = 0;
    //回転の経過時間
    private float _initWaitTime = 0;
    public float InitWaitTime
    {
        get { return _initWaitTime; }
    }
    //後隙の経過時間
    private float _initStopTime = 0;
    public float StopTime
    {
        get { return _initStopTime; }
    }

    [SerializeField, Header("後退する速度")]
    private float _backSpeed = 100;

    //後退速度を保存する変数
    private float _originBackSpeed = 0;

    [SerializeField, Header("後退の減算値")]
    private float _decSpeed = 20;

    [SerializeField, Header("後退する秒数")]
    private float _backTime = 0.5f;

    [SerializeField, Header("後退後の溜め")]
    private float _reservoirTime = 0.5f;

    private SEManager _seManager = default;



    //後退開始してどれくらい経過したか
    private float _initBackTime = 0;

    public override void Start()
    {
        base.Start();
        GameObject seManagerObject = GameObject.FindWithTag("SEManager");
        _seManager = seManagerObject.GetComponent<SEManager>();
        _originBackSpeed = _backSpeed;
    }
    private void FixedUpdate()
    {
        switch (_initState)
        {
            case ArmorState.Stop:
                //現在の停止時間を測る(後隙)
                _initStopTime += Time.fixedDeltaTime;
                //後隙が終わったらまたプレイヤーの方に回転して溜め開始
                if(_initStopTime>= _recoveryTime)
                {
                    _initState = ArmorState.Rotate;
                    _initStopTime = 0;
                }
                break;
            case ArmorState.Rotate:
                //狙いを定めてる時間を測る(溜め)
                _initWaitTime += Time.fixedDeltaTime;
                //プレイヤーの座標から自分の座標を引くことで方向を取得
                _direction = (_playerObject.transform.position - transform.position).normalized;
                //回転させる
                _armorRotation.ChangeRotate(_direction);
                //溜め時間が来たら後退させる
                if(_initWaitTime>= _waitLimitTime)
                {
                    _initState = ArmorState.Back;
                    _seManager.ShieldBackSE();
                    _initWaitTime = 0;
                }
                break;

            case ArmorState.Back:
                //現在の後退した時間が指定の後退時間まで来たら実行
                if(_initBackTime >= _backTime )
                {
                    //ステートをReservoirにして動かないようにする
                    _initState = ArmorState.Reservoir;
                    //コルーチン作動
                    StartCoroutine(Reservoir());
                }
                else
                {
                    //後退の処理、徐々に後退速度が下がる
                    transform.position -= (Vector3)_direction * _backSpeed * Time.fixedDeltaTime;
                    _initBackTime += Time.fixedDeltaTime;
                    _backSpeed -= _decSpeed;
                    _backSpeed = Mathf.Clamp(_backSpeed, 0, _originBackSpeed);
                }
                break;

            case ArmorState.Reservoir:
                //コルーチンで処理してるのでなにもしない
                break;


            case ArmorState.Rush:
                _cantMove = _enemyState == EnemyState.knockback || _enemyState == EnemyState.fall;
                //ノックバック中または落下中は移動しないようにする
                if (_cantMove) 
                {
                    return;
                }
                Moving();
                break;

        }
    }

    /// <summary>
    ///　後退してから少し待って突撃させるコルーチン
    /// </summary>
    /// <returns>指定時間分待機</returns>
    private IEnumerator Reservoir()
    {
        //突撃待機時間分待つ
        yield return new WaitForSeconds(_reservoirTime);
        //突撃ステートに変更、後退時間及び後退速度を変更
        _initState = ArmorState.Rush;
        _initBackTime = 0;
        _initRushTime = 0;
        _backSpeed = _originBackSpeed;

    }

    /// <summary>
    /// 突撃のメソッド
    /// </summary>
    public override void Moving()
    {
        //移動方向に速度と時間を掛けて移動させる
        transform.position += (Vector3)_direction * _moveSpeed * Time.fixedDeltaTime;
        //突撃時間計測
        _initRushTime += Time.fixedDeltaTime;
        //効果音再生
        _seManager.ShieldRushSE();
        //指定時間分突撃したら停止
        if(_initRushTime>= _rushTime)
        {
            _initRushTime = 0;
            _initState = ArmorState.Stop;
        }
    }

    /// <summary>
    /// 突撃してるときに落とし穴や岩に触れた時のメソッド
    /// </summary>
    public void ContactWithSomethingDuring()
    {
        //突撃時間を強制的に最大にして突進停止
        _initRushTime = _rushTime;
    }
}
