using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionAnything : MonoBehaviour
{
    private const int ENEMY_LAYER = 6;
    private readonly string PLAYER_TAGNAME = "Player";
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject colObj = collision.gameObject;
        if(colObj.layer == ENEMY_LAYER)
        {
            EnemyKnockBack enemy = collision.gameObject.GetComponent<EnemyKnockBack>();
            if ((enemy != null))
            {
                enemy.CollisionWall();
            }
        }else if (colObj.CompareTag(PLAYER_TAGNAME))
        {
            PlayerDamageKnockBack player = colObj.GetComponent<PlayerDamageKnockBack>();
            if(player != null)
            {
                player.CollisionWall();
            }
        }
    }
}
