using UnityEngine;

public class CreatePunchEffect : MonoBehaviour
{
    [SerializeField, Header("ボスのState管理")]
    private BossStateManagement _bossStateManagement = default;
    [SerializeField, Header("ぶつかった時のエフェクト")]
    private GameObject _effect = default;
    [SerializeField, Header("ボスのスプライト")]
    private GameObject _bossSprite = default;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_bossStateManagement._currentState == BossStateManagement.BossState.Punch)
        {
            Vector3 createPosition = new Vector3(_bossSprite.transform.position.x + 0.95f,
                        _bossSprite.transform.position.y,
                        transform.position.z);

            if (collision.gameObject.CompareTag("Player"))
            {
                Instantiate(_effect, createPosition, Quaternion.identity);
            }
        }

    }
}
