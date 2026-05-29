using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondBossMove : BossMove
{
    private bool _isChangeFallAreaSize = false;
    protected float _secondActionCoolTime = 0.5f;
    [SerializeField, Header("銃弾の方向に加算する数値")]
    private float[] _attackAngle = new float[] { -30, -15, 0, 15, 30 };

    [SerializeField, Header("第二形態スプライトRenderer")]
    private SpriteRenderer _secondFormSpriteRenderer = default;

    [SerializeField, Header("第二形態スプライト")]
    private Sprite _secondFormSprite = default;
    protected override void Start()
    {
        base.Start();
        _secondFormSpriteRenderer.sprite = _secondFormSprite;
        _langeList = _attackAngle;
    }
    private void FixedUpdate()
    {
        _enemyToPlayerDistance = Vector2.Distance(_playerObject.transform.position,
            this.transform.position);

        _actionCoolTimer += Time.fixedDeltaTime;//アクション後の後隙時間計測

        StateAction();

        #region
        ////  最優先でスタン判定
        //if (_currentState == BossState.Stun)
        //{
        //    InStop();
        //    return;
        //}

        //_shotCoolTimer += Time.fixedDeltaTime;
        //_actionCoolTimer += Time.fixedDeltaTime;

        //if (_shotCoolTimer >= _shotCoolTime)
        //    _canShot = true;
        //else
        //    _canShot = false;

        //if (_actionCoolTimer >= _actionCoolTime)
        //    _canAction = true;
        //else
        //    _canAction = false;

        ////  スタン中ならCheckSpecialMoveTriggerは絶対に呼ばない
        //if (_currentState != BossState.Stun)
        //{
        //    CheckSpecialMoveTrigger();
        //}

        //if (_initBossForm == BossForm.Second)
        //{

        //    if (_actionCoolTime > _secondActionCoolTime)
        //    {
        //        _actionCoolTime = _secondActionCoolTime;
        //    }

        //    StateAction();

        //    if (!_isChangeFallAreaSize)
        //    {
        //        _isChangeFallAreaSize = true;
        //        _musccleArm.SetActive(true);

        //    }
        //}
        #endregion
    }

}
