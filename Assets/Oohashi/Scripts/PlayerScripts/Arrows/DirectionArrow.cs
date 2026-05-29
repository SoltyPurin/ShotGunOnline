using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionArrow : MonoBehaviour
{
    [SerializeField, Header("プレイヤーのオブジェクト")]
    private GameObject _player = default;
    private Vector3 _direction = default;
    private Vector3 _enemyPosition = default;
    public Vector3 EnemyPosition
    {
        get { return _enemyPosition; }
    }


    private void FixedUpdate()
    {
        _direction = (FindNearEnemy("Enemy","MediumArmor") - _player.transform.position).normalized;
        _enemyPosition = (FindNearEnemy("Enemy", "MediumArmor"));
        float atanDirection = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        //angle -= 90;
        GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 一番近い敵を探す
    /// </summary>
    /// <param name="tagName">通常エネミーのタグの名前</param>
    /// <param name="armor">盾持ちのタグの名前</param>
    /// <returns>一番近い敵の位置</returns>
    private Vector3 FindNearEnemy(string tagName,string armor)
    {
        List<GameObject> enemyList = new List<GameObject>();
        enemyList.AddRange(GameObject.FindGameObjectsWithTag(tagName));
        enemyList.AddRange(GameObject.FindGameObjectsWithTag(armor));
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        foreach (GameObject enemy in enemyList)
        {
            float dist = Vector3.Distance(_player.transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }
        if (nearest != null)
        {
            return nearest.transform.position;
        }
        else
        {
            return Vector3.zero;
        }
    }
}
