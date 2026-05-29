using UnityEngine;

public class ArmorRotation : MonoBehaviour
{
    //プレイヤーのオブジェクトを収容する変数
    private GameObject _playerObject = default;

    [SerializeField,Header("アニメーター")] 
    private Animator _animator = default;

    private Quaternion _rotateDirection = default;
    public Quaternion RotateDirection
    {
        get { return _rotateDirection; }
    }

    private void Start()
    {
        _playerObject = GameObject.FindWithTag("Player");
        _animator.enabled = false;
        //下3行で最初にプレイヤーの方を向かせる
        Vector2 direction = (_playerObject.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    /// <summary>
    /// プレイヤーの方に回転させるメソッド
    /// </summary>
    /// <param name="direction">プレイヤーのいる方向</param>
    public void ChangeRotate(Vector2 direction)
    {
        //ラジアン角を求めて、それを度に変換
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        _rotateDirection = Quaternion.Euler(0, 0, angle-90);   
    }
}
