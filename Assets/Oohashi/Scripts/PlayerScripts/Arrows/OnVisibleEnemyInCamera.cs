using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnVisibleEnemyInCamera : MonoBehaviour
{
    [SerializeField, Header("敵の方向を示す矢印")]
    private GameObject _arrow = default;
    [SerializeField, Header("矢印のスクリプト")]
    private DirectionArrow _directionArrow = default;

    private void Update()
    {
        Vector3 pos = Camera.main.WorldToViewportPoint(_directionArrow.EnemyPosition);
        bool isOnScreen = pos.z > 0 && pos.x >= 0 && pos.x <= 1 && pos.y >= 0 && pos.y <= 1;

        if (isOnScreen)
        {
            _arrow.SetActive(false);
        }
        else
        {
            _arrow.SetActive(true);
        }
    }
}
