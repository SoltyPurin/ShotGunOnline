using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DustPos : MonoBehaviour
{

    private GameObject _boss = default;
    private GameObject _player = default;

    private readonly string PLAYER = "Player";
    private readonly string BOSS = "BossSprite";

    private Vector2 _leftRotation = new Vector3(0, 180f, 0);
    private void Start()
    {
        _player = GameObject.FindWithTag(PLAYER);
        _boss = GameObject.FindWithTag(BOSS);
        transform.rotation = _boss.transform.rotation;
        //EnemyLook();
        //Vector3 instantiatePos = new Vector3(_boss.transform.position.x - 7,
        //   _boss.transform.position.y - 1,
        //   transform.position.z);

        //transform.position = instantiatePos;
        transform.position = _boss.transform.position;
    }

    private void EnemyLook()
    {
        float direction = (_player.transform.position.x - this.transform.position.x);
        if (direction > 0)
        {
            TurnRight();
        }
        else
        {
            TurnLeft();
        }
    }

    private void TurnLeft()
    {
        transform.localRotation = Quaternion.Euler(_leftRotation);
    }

    private void TurnRight()
    {
        transform.localRotation = Quaternion.Euler(Vector2.zero);
    }
}
