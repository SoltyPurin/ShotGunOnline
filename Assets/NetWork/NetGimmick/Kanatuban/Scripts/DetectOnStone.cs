using UnityEngine;

public class DetectOnStone : DetectOnPlayer
{
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject colObj = collision.gameObject;
        if (!colObj.CompareTag("Stone"))
        {
            return;
        }

        base.OnTriggerEnter2D (collision);
    }

    public override void OnTriggerExit2D(Collider2D collision)
    {
        GameObject colObj = collision.gameObject;
        if (!colObj.CompareTag("Stone"))
        {
            return;
        }
        base.OnTriggerExit2D (collision);
    }
}
