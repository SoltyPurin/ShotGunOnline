using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetect : MonoBehaviour
{
    private readonly string PLAYER_TAG = "Player";
    private ReleaseSoul _releaseSoul = default;
    [SerializeField, Header("エフェクト")]
    private GameObject _effect = default;

    private void Start()
    {
        _releaseSoul = GetComponent<ReleaseSoul>(); 
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject obj = collision.gameObject;
        if (obj.CompareTag(PLAYER_TAG))
        {
            GameObject effect = Instantiate(_effect);
            effect.transform.position = transform.position;
            _releaseSoul.ReleaseProtocol();
        }
    }
}
