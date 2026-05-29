using UnityEngine;

public class DamageMaterial : MonoBehaviour
{
    [SerializeField, Header("レンダラー")]
    private SpriteRenderer _sprite = default;

    [SerializeField, Header("ダメージアニメ")]
    private Animator _damageAnim = default;

    private void Start()
    {
        _sprite.material = Instantiate(_sprite.material);
    }

    public void Damage()
    {
        _damageAnim.SetTrigger("Damage");
    }
}
