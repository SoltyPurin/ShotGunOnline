using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneGateContactEnything : MonoBehaviour,EnythingTouchGate
{
    private bool _isOneGateContact = false;
    public bool IsContact
    {
        get { return _isOneGateContact; }
    }

    private GameObject _oneGateCollisionObject = default;
    public GameObject OneGateCollisionObject
    {
        get { return _oneGateCollisionObject; }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _isOneGateContact = true;
        _oneGateCollisionObject = collision.gameObject;
    }

    public void ResetVariable()
    {
        _isOneGateContact = false;
        _oneGateCollisionObject = default;
    }

}
