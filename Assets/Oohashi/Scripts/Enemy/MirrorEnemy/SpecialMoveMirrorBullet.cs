using UnityEngine;

public class SpecialMoveMirrorBullet : EnemyMirrorBullet
{
    [SerializeField, Header("スプライト登録")]
    private SpriteRenderer _sprite = default;

    private PlayTheBombEffect _bombEffect = default;

    Color _plusColor = new Color(0, 0.2f, 0.2f);

    //二回撃たれたら反射する
    //一回撃つごとに弾の色変化

    private void Start()
    {
        _bombEffect = GameObject.FindWithTag("EffectManager").GetComponent<PlayTheBombEffect>();
    }


    public override void DecBulletHP(float chargeTime, Vector2 playerDirection)
    {
        Color currentColor = _sprite.color;
        _sprite.color = currentColor + _plusColor;

        _bulletHP -= chargeTime;
        if (_bulletHP <= 0)
        {
            _sprite.color = new Color(1, 1, 1);
            Mirror(2, playerDirection);
        }

    }


    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.CompareTag("Player"))
        {
            _bombEffect.BombEffect(this.transform.position);
        }
    }
}
