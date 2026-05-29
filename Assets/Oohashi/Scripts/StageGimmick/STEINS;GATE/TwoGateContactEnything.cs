using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoGateContactEnything : MonoBehaviour, EnythingTouchGate
{
    private bool _isTwoGateContact = false;
    public bool IsTwoGateContact
    {
        get { return _isTwoGateContact; }
    }

    private GameObject _twoGatecollisionObject = default;
    public GameObject TwoGateCollisionObject
    {
        get { return _twoGatecollisionObject; }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _isTwoGateContact = true;
        _twoGatecollisionObject = collision.gameObject;
    }

    public void ResetVariable()
    {
        _isTwoGateContact = false;
        _twoGatecollisionObject = default;
    }
}
