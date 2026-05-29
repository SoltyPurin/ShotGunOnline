using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCoin : MonoBehaviour
{
    private readonly string PLAYER_TAGNAME = "Player";
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject colObject = collision.gameObject;

        if (colObject.CompareTag(PLAYER_TAGNAME))
        {
            SoulKeep soul = colObject.GetComponent<SoulKeep>();
            if ((soul != null))
            {
                soul.AdditionCoin();
                Destroy(this.gameObject);
            }
        }
    }
}
