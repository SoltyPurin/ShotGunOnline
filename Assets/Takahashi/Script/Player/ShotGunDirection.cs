using UnityEngine;

public class ShotGunDirection : MonoBehaviour
{
    [SerializeField, Header("ショットガンのスプライト")]
    private GameObject _shotGun = default;

    [SerializeField, Header("取得系")]
    private PlayerAiming _playerAiming = default;

    private SpriteRenderer _shotgunMesh = default;

    [SerializeField, Header("プレイヤーのスプライト")]
    private SpriteRenderer _playerSprite = default;

    private int _playerSpriteOrderInLayer = default;

    private void Start()
    {
        _shotgunMesh = _shotGun.GetComponent<SpriteRenderer>();
        _playerSpriteOrderInLayer = _playerSprite.sortingOrder;
    }

    private void FixedUpdate()
    {
        float horizontalDirection = _playerAiming.Direction.x;
        float verticalDirection = _playerAiming.Direction.y;

        if (verticalDirection > 0)
        {
            //上向き
            _shotgunMesh.sortingOrder = _playerSpriteOrderInLayer - 1;
        }
        else
        {
            //下向き
            _shotgunMesh.sortingOrder = _playerSpriteOrderInLayer + 1;
        }

        if(horizontalDirection > 0)
        {
            RotateRight();
        }
        else
        {
            RotateLeft();
        }
    }

    private void RotateRight()
    {
        float rotationY = transform.localRotation.y;
        float rotationX = 0;
        _shotGun.transform.localRotation = Quaternion.Euler(rotationX,rotationY,0);
    }

    private void RotateLeft()
    {
        float rotationY = transform.localRotation.y;
        float rotationX = 180;
        _shotGun.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

}
