using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveState
{
    Approach,  // 移動
    Stop,      // 停止
    Leave      // 離脱
}
public class KeepDistanceMove : EnemyMove
{
    //ロックオンのステート
    private MoveState _initState = MoveState.Approach;
    public MoveState InitState
    {
        get { return _initState; }
    }

    [Header("どれくらい離れたら再移動するか")]
    [SerializeField] private float _reStartDistance = 7;

    [Header("どれくらい近づいたら離れるか")]
    [SerializeField] private float _leaveDistance = 3;

    [Header("どれくらい離れたら離脱を止めるか")]
    [SerializeField] private float _leaveStopDistance = 5;


    private float _stoppingTime = 0.0f; //停止してる時間
    //次に撃つまでの時間
    private float _timeUntilNextShot = 2.0f;

    [Header("銃弾のオブジェクト")]
    [SerializeField] private GameObject _bulletObject = default;

    private SEManager _seManager = default;

    public override void Start()
    {
        base.Start();
        _direction = (_playerObject.transform.position - transform.position).normalized;

        GameObject seManagerObject = GameObject.FindWithTag("SEManager");
        _seManager = seManagerObject.GetComponent<SEManager>();
        _timeUntilNextShot = JsonSaver.Instance.EnemyJson.TimeUntilNextShot;
    } 
    public override void Moving()
    {
        if(_target == null)
        {
            return;
        }
        float distance = Vector2.Distance(_playerObject.transform.position, this.transform.position);
        switch (_initState)
        {
            case MoveState.Approach:

                if(_target == null)
                {
                    return;
                }
                _agent.destination = _target.position; //agentの目的地をtargetの座標にする

                if (distance <= _keepDistance) //一定距離まで近づいたら停止
                {
                    _initState = MoveState.Stop;
                }
                break;

            case MoveState.Stop:

                //止まってる時間を加算
                _stoppingTime += Time.fixedDeltaTime;
                //停止時間が射撃時間を上回ったら射撃
                if (_stoppingTime >= _timeUntilNextShot)
                {
                    //銃弾を生成
                    GameObject bullet = Instantiate(_bulletObject, transform.position, Quaternion.identity);
                    //GameObjectからスクリプトを取得
                    EnemyShotBullet scriptOfBullet = bullet.GetComponent<EnemyShotBullet>();
                    //方向を計算
                    _direction = (_playerObject.transform.position - transform.position).normalized;
                    //銃弾に方向を渡す
                    scriptOfBullet.DirectionSetting(this.gameObject, _direction);
                    //音を再生
                    _seManager.PlayEnemyShotSound();
                    //停止時間をリセット
                    _stoppingTime = 0.0f;
                }

                //距離が再起動距離を上回ったら移動
                if (distance > _reStartDistance)
                {
                    _initState = MoveState.Approach;
                }

                //プレイヤーが近づいてきたら移動
                if(distance <= _leaveDistance)
                {
                    _initState = MoveState.Leave;
                }
                    break;

            case MoveState.Leave:

                _direction = (_playerObject.transform.position - transform.position).normalized * -1;
                transform.position += (Vector3)_direction * _moveSpeed *3 * Time.fixedDeltaTime;

                if (distance > _leaveStopDistance)
                {
                    _initState = MoveState.Stop;
                }

                break;
        }
    }
}




