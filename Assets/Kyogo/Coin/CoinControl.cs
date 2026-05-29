using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinControl : MonoBehaviour
{
    private enum CoinState
    {
        Up,
        Wait,
        Move
    }
    private CoinState _coinState = CoinState.Up;
    [SerializeField, Header("上昇値")]
    private float _increasedValue = 0.5f;
    [SerializeField, Header("最大上昇量")]
    private float _maxUpValue = 2f;
    [SerializeField, Header("飛び上がって何秒待つ？")]
    private float _waitLimitTime = 3f;
    [SerializeField,Header("プレイヤーに向かう速度")]
    private float _absorbSpeed = 5;

    [SerializeField, Header("上昇値をだんだん小さくしていく値")]
    private float _risePowerDecay = 0.8f;

    [SerializeField, Header("どれくらい近づいたら消える？")]
    private float _disappearDistance = 0.1f;

    private float _initIncreasedValue = 0f;
    private float _initWaitTime = 0f;
    /*
     _playerObj プレイヤー
    _targetDirection 向かう方向
     */
    private GameObject _playerObj = default;
    private Vector2 _targetDirection = default;

    private readonly string PLAYER_TAGNAME = "Player";

    void Start()
    {
        _playerObj = GameObject.FindWithTag(PLAYER_TAGNAME);
    }

    //左右に移動させる処理
    void Update()
    {
        switch( _coinState )
        {
            case CoinState.Up:
                UpMethod();
                break;

            case CoinState.Wait:
                WaitMethod(_waitLimitTime);
                break;

            case CoinState.Move:
                MoveMethod();
                break;
        }
    }

    private void UpMethod()
    {
        if(_initIncreasedValue <= _maxUpValue)
        {
            Vector3 initPos = transform.position;
            initPos.y += _increasedValue;
            transform.position = initPos;
            _increasedValue *= _risePowerDecay;
            _initIncreasedValue += _increasedValue;
        }
        else
        {
            _coinState = CoinState.Wait;
        }
    }

    public void WaitMethod(float waitTime)
    {
        _waitLimitTime = waitTime;
        _initWaitTime += Time.deltaTime;
        if( _waitLimitTime <= _initWaitTime)
        {
            _coinState = CoinState.Move;
        }
    }

    private void MoveMethod()
    {
        Vector3 direction = _playerObj.transform.position - this.transform.position;
        transform.position += direction * _absorbSpeed * Time.deltaTime;

        float distance = Vector3.Distance(_playerObj.transform.position, this.transform.position);
        if(distance <= _disappearDistance)
        {
            SoulKeep soul = _playerObj.GetComponent<SoulKeep>();
            soul.AdditionCoin();
            Destroy(this.gameObject);
        }
    }

}
