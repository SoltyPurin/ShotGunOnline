using UnityEngine;

public class Bullet : MonoBehaviour
{
    private bool _canMove = false; //銃弾が移動してもよいか
    private Vector3 _moveDirection = default; //移動方向
    [Header("銃弾の速度")]
    [SerializeField] private float _moveSpeed = 5.0f;//銃弾の速度
    [Header("銃弾のオブジェクト登録")]
    [SerializeField] private GameObject _bulletObject = default;
    private float _disableTimer = 0.0f; //現在の銃弾が存在してる時間
    private float _chargeTime = 0f; //銃弾の生存時間、ただしこれはチャージ時間によって変動する
    [SerializeField, Header("最大射程")]
    private int _maxRange = 15;
    public float ChargeTime
    {
        set { _chargeTime = value; }//チャージ時間によって生存時間が変わる
    }
    private const float MAXBULLETLIMITTIME = 4;//最大生存時間、ここからチャージ時間を引いて生存時間を決める
    private Vector3 _firstDirection = new Vector3(0, 0, 0);//右スティックの入力無しかどうかを判断する
    private Vector3 _startPosition = default;

    /// <summary>
    /// 銃弾の向きを設定するメソッド
    /// </summary>
    /// <param name="direction">プレイヤーの向いている方向</param>
    /// <param name="position">射撃を行った地点</param>
    public void Init(Vector3 direction, Vector3 position)
    {
        if(_firstDirection == direction)//スティックに入力せずに撃った場合
        {
            _moveDirection = new Vector3(1, 0, 0);//右向きに弾を撃つ
        }
        else
        {
            _moveDirection = direction;//右スティックの入力方向を移動方向に代入
        }
        _startPosition = position;
        transform.position = position;//プレイヤーのポジションからスタートさせる
        _canMove = true;//移動を許可する
        //恐らくだがここでMAXBULLETLIMITTIMEから_chargeTimeを引く新しい変数を作って渡す必要がある

    }

    private void FixedUpdate()
    {
        if (_canMove)//移動が可能か
        {
            Moveing(_moveDirection);//移動のメソッドに移動方向を渡す
        }
        else
        {
            return;//そうじゃなければリターン
        }
    }
    /// <summary>
    /// 銃弾の移動スクリプト
    /// </summary>
    /// <param name="direction">向かう方向</param>
    private void Moveing(Vector3 direction)
    {
        float t = Mathf.Clamp01(_chargeTime / 2f);
        float radius = Mathf.Lerp(_maxRange, 5f, t); //最大距離
        float initDistance = (transform.position - _startPosition).magnitude;
        //指定方向に移動速度を乗算して時間単位で移動させる
        transform.position += direction * _moveSpeed * Time.deltaTime;
        if(radius<= initDistance)//現在の進行した距離が最大距離についたら処理を行う
        {
            _bulletObject.SetActive(false);//銃弾のオブジェクトを非アクティブ化する
            _canMove = false;//移動許可を取り消す
        }
    }
}
