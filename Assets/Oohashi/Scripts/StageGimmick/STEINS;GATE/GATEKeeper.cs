using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GATEKeeper : MonoBehaviour
{
    //OneGateは下に射出、TwoGateは上に射出
    [SerializeField,Header("1つ目のゲート")]
    private MonoBehaviour _gateA;
    [SerializeField,Header("2つ目のゲート")]
    private MonoBehaviour _gateB;

    [SerializeField, Header("ワープの待機時間")]
    private float _warpWaitTime = 0.1f;

    private EnythingTouchGate _aGateScript = default;
    private EnythingTouchGate _bGateScript = default;

    private OneGateContactEnything _oneGateScript = default;

    private TwoGateContactEnything _twoGateScript = default;

    private Vector2 _oneGateDirection = new Vector2(0, -1);

    private Vector2 _twoGateDirection = new Vector2(0, 1);

    private bool _canWarp = true;

    private void Start()
    {
        _oneGateScript = _gateA.GetComponent<OneGateContactEnything>();
        _twoGateScript = _gateB.GetComponent<TwoGateContactEnything>();

        _aGateScript = _gateA as EnythingTouchGate;
        _bGateScript = _gateB as EnythingTouchGate;
    }

    private void FixedUpdate()
    {
        if (!_canWarp)
        {
            return;
        }

        //oneGateに触れたとき
        if (_oneGateScript.IsContact)
        {
            WarpToTwoGate(_oneGateScript.OneGateCollisionObject);
            _canWarp = false;
        }

        //twoGateに触れたとき
        if (_twoGateScript.IsTwoGateContact)
        {
            WarpToOneGate(_twoGateScript.TwoGateCollisionObject);
            _canWarp = false;
        }

    }

    /// <summary>
    /// TwoGateに触れたものをOneGateに飛ばす
    /// </summary>
    /// <param name="collision">触れたオブジェクト</param>
    private void WarpToOneGate(GameObject collision)
    {
        collision.transform.position = _gateA.transform.position;
        //敵はレイヤーで判断する
        if(collision.layer == 6)
        {
            EnemyKnockBack knockBack = collision.GetComponent<EnemyKnockBack>();
            if(knockBack != null)
            {
                knockBack.SetWarpDirection(_oneGateDirection);
            }
        }else if (collision.CompareTag("Player"))
        {
            PlayerDamageKnockBack playerKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            if(playerKnockBack != null)
            {
                playerKnockBack.SetWarpDirection(_oneGateDirection);
            }
        }
            StartCoroutine(ResetWarp());
    }

    /// <summary>
    /// OneGateに触れたものをTwoGateに飛ばす
    /// </summary>
    /// <param name="collsion">触れたオブジェクト</param>
    private void WarpToTwoGate(GameObject collision)
    {
        collision.transform.position = _gateB.transform.position;
        if (collision.layer == 6)
        {
            EnemyKnockBack knockBack = collision.GetComponent<EnemyKnockBack>();
            if (knockBack != null)
            {
                knockBack.SetWarpDirection(_twoGateDirection);
            }
        }
        else if (collision.CompareTag("Player"))
        {
            PlayerDamageKnockBack playerKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            if (playerKnockBack != null)
            {
                playerKnockBack.SetWarpDirection(_twoGateDirection);
            }
        }

        StartCoroutine(ResetWarp());
    }

    /// <summary>
    /// ワープのクールタイム、規定秒数待ってからワープできるようにする
    /// </summary>
    /// <returns>クールタイム</returns>
    private IEnumerator ResetWarp()
    {
        yield return new WaitForSeconds(_warpWaitTime);
        _aGateScript.ResetVariable();
        _bGateScript.ResetVariable();
        _canWarp = true;
    }
}
