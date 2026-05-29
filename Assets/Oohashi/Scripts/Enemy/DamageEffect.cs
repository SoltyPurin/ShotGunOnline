using UnityEngine;
//using UnityEngine.SocialPlatforms.GameCenter;

public class DamageEffect : MonoBehaviour
{
    #region 変数
    private GameObject _player = default;
    private GameObject _parent = default;
    private EnemyMove _enemyMove = default;
    private SpriteRenderer _sprite = default;
    private Animator _anim = default;
    private bool _canPlayAnim = true;
    #endregion

    #region 定数
    private readonly string PLAYER = "Player";
    #endregion

    private void Start()
    {
        _player = GameObject.FindWithTag(PLAYER);
        _parent = transform.parent.gameObject;
        _enemyMove = GetComponentInParent<EnemyMove>();
        _anim = GetComponent<Animator>();
    }

    public void PlayAnim()
    {
        Vector3 point = _parent.transform.position;
        Vector3 axis = Vector3.forward;

        Vector3 direction = -1 * (_player.transform.position - point).normalized;

        Vector3 from = (transform.position - point).normalized; // 自分の方向ベクトル
        Vector3 to = -1 * (_player.transform.position - point).normalized; // プレイヤーの方向ベクトル

        float angle = Vector3.SignedAngle(from, to, Vector3.forward); // Z軸回りで回転角を計算
        transform.RotateAround(point, axis, angle);

        _anim.SetTrigger("Damage");
    }
}
