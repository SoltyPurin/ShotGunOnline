using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCoin : MonoBehaviour
{
    private float _xDirection = 0;
    private float _yDirection = 0;

    [SerializeField, Header("プラス方向への最大移動量")]
    private float _movePlusLimit = 10;

    [SerializeField,Header("マイナス方向への最大移動量")]
    private float _moveMinusLimit = -10;

    [SerializeField, Header("最高初速")]
    private float _maxMoveSpeed = 2;

    [SerializeField, Header("最低初速")]
    private float _minMoveSpeed = 0.1f;

    //初速
    private float _firstMoveSpeed = 0;

    [SerializeField, Header("スピード減算に用いる値の最低値")]
    private float _speedDecMinLimit = 0.1f;

    [SerializeField, Header("スピード減算に用いる値の最大値")]
    private float _speedDecMaxLimit = 1.0f;

    [SerializeField, Header("当たり判定がアクティブになるまでの時間")]
    private float _colliderActiveTime = 0.5f;

    [SerializeField,Header("スプライトレンダラー")]
    private SpriteRenderer _spriteRenderer;

    [SerializeField,Header("コインが消えるまでの時間")]
    private float _destroyTime = 5.0f;

    [SerializeField,Header("点滅を開始するまでの時間")]
    private float _blinkingStartTime = 3.0f;

    private float _initAliveTime = 0.0f;

    private Vector2 _moveDirection = default;

    private float _speedDecValue = 0;

    //ここを切り替えることで点滅させる
    private bool _isActive = true;

    private bool _canMove = false;

    private SoulGraduallyMoveToPlayer _soulCollectRange = default;

    public void MoveStart()
    {
        _soulCollectRange = GetComponent<SoulGraduallyMoveToPlayer>();

        _xDirection = Random.Range(_moveMinusLimit, _movePlusLimit);
        _yDirection = Random.Range(_moveMinusLimit, _movePlusLimit);

        _firstMoveSpeed = Random.Range(_minMoveSpeed, _maxMoveSpeed);

        _moveDirection = new Vector2(_xDirection, _yDirection);
        _speedDecValue = Random.Range(_speedDecMinLimit, _speedDecMaxLimit);

        _canMove = true;

        StartCoroutine(ActiveHitjudgment());
    }

    private IEnumerator ActiveHitjudgment()
    {
        yield return new WaitForSeconds(_colliderActiveTime);
        BoxCollider2D boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.enabled = true;
        if(_soulCollectRange != null)
        {
            _soulCollectRange.EnableCollider();
        }
    }

    public void ChangeWaitTimeMethod(float time)
    {
        _blinkingStartTime = time;
    }

    private void FixedUpdate()
    {

        if (!_canMove)
        {
            return;
        }

        _initAliveTime += Time.fixedDeltaTime;
        if(_firstMoveSpeed >=0)
        {
            transform.position += (Vector3)_moveDirection * _firstMoveSpeed * Time.fixedDeltaTime;

            _firstMoveSpeed -= _speedDecValue;


        }

        if(_initAliveTime >= _blinkingStartTime)
        {
            CoinControl control = GetComponent<CoinControl>();
            if(control != null)
            {
                control.WaitMethod(0);
                _canMove = false;   
            }
            else
            {
                _isActive = !_isActive;
                _spriteRenderer.enabled = _isActive;
            }
        }

        if(_initAliveTime >= _destroyTime)
        {
            Destroy(this.gameObject);
        }
    }
}
