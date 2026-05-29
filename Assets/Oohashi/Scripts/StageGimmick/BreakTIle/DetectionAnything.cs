using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionAnything : MonoBehaviour
{
    [SerializeField, Header("âΩâÒêGÇÍÇΩÇÁïˆÇÍÇÈÇ©")]
    private int _breakLimit = 2;

    [SerializeField, Header("âΩïbÇ≈ïˆÇÍÇÈÇ©")]
    private float _breakTime = 1.0f;

    private BreakProtocol _breakProtocol = default;

    private int _triggerCount = 0;

    private readonly string BULLET_TAGNAME = "EnemyBullet";

    private void Start()
    {
        _breakProtocol = GetComponent<BreakProtocol>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject colObj = collision.gameObject;
        if (colObj.CompareTag(BULLET_TAGNAME))
        {
            return;
        }

        _triggerCount++;

        if( _triggerCount >= _breakLimit)
        {
            StartCoroutine(BreakFloorMethod());
        }
    }

    private IEnumerator BreakFloorMethod()
    {
        yield return new WaitForSeconds(_breakTime);
        Vector3 pos = transform.position;
        _breakProtocol.EraseTile(pos);
    }
}
