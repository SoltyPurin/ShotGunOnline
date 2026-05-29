using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnderRay : MonoBehaviour
{
    private RaycastHit _hit = default;

    private EnemyTakeDamage _damage = default;

    private void Start()
    {
        _damage = GetComponent<EnemyTakeDamage>();  
    }
    private void Update()
    {
        RayMethod();
    }

    private void RayMethod()
    {
        Debug.DrawRay(transform.position, Vector3.forward, Color.red);
        int groundLayer = LayerMask.GetMask("TileMap");
        //z方向にレイを出す、1はレイの長さで3はステージのレイヤー番号
        if (!Physics.Raycast(transform.position,Vector3.forward,out _hit, 1,groundLayer))
        {
            //Debug.Log("地面");
        }
        else
        {
            _damage.FallDamage();
            //Debug.Log("NOT地面");
        }
    }
}
