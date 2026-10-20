using UnityEngine;

/// <summary>
/// ボスのパンチがプレイヤーに衝突した際のエフェクト生成スクリプト
/// </summary>
public class CreatePunchEffect : MonoBehaviour
{
    [SerializeField, Header("ボスState管理")]
    private BossStateManagement _bossStateManagement = default;
    [SerializeField, Header("ぶつかったエフェクト")]
    private GameObject _effect = default;
    [SerializeField, Header("ボスのスプライト")]
    private GameObject _bossSprite = default;
    [SerializeField, Header("エフェクト自動破棄時間(秒)")]
    private float _effectDestroyTime = 0.8f;

    private void Start()
    {
        if (_bossStateManagement == null)
        {
            _bossStateManagement = GetComponentInParent<BossStateManagement>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_bossStateManagement != null && _bossStateManagement._currentState == BossStateManagement.BossState.Punch)
        {
            Vector3 spritePos = _bossSprite != null ? _bossSprite.transform.position : transform.position;
            Vector3 createPosition = new Vector3(spritePos.x + 0.95f, spritePos.y, transform.position.z);

            if (collision.gameObject.CompareTag("Player") && _effect != null)
            {
                GameObject instantiatedEffect = Instantiate(_effect, createPosition, Quaternion.identity);
                // 生成したエフェクトが消えずに残り続けるのを防ぐため、一定時間後に確実に破棄
                Destroy(instantiatedEffect, _effectDestroyTime);
            }
        }
    }
}