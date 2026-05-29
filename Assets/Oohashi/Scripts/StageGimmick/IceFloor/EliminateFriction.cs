using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliminateFriction : MonoBehaviour
{
    private float _playerDefaultDrag = 0;

    private float _enemyDefaultDrag = 0;

    private readonly string PLAYER_TAGNAME = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject colObj = collision.gameObject;
        if (colObj.CompareTag(PLAYER_TAGNAME))
        {
            PlayerMove move = colObj.GetComponent<PlayerMove>();
            move.IsFloating = true;
        }
        else if(colObj.layer == 6)
        {
            EnemyMove enemy = colObj.GetComponent<EnemyMove>();
            enemy.ChangeFloat(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        GameObject colObj = collision.gameObject;
        if (colObj.CompareTag(PLAYER_TAGNAME))
        {
            PlayerMove move = colObj.GetComponent<PlayerMove>();
            move.IsFloating = false;
        }
        else if( colObj.layer == 6)
        {
            EnemyMove enemy = colObj.GetComponent<EnemyMove>();
            enemy.ChangeFloat(false);

        }
    }
}
