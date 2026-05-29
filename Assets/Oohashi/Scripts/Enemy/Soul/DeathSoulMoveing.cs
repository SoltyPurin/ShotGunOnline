using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSoulMoveing : MonoBehaviour
{
    [SerializeField, Header("飛び上がる距離")]
    private float _upDistance = 1.0f;

    [SerializeField, Header("飛び上がる速度")]
    private float _upSpeed = 0.3f;

    [SerializeField, Header("出現してどれくらいの距離で滞空に移行するか")]
    private float _limitDistance = 1.5f;

    [SerializeField, Header("滞空時間")]
    private float _stayTime = 0.5f;

    [SerializeField, Header("ソウルのオブジェクト")]
    private GameObject _soulObject = default;

    private float _saveYPos = 0;

    //敵が死んだかの判定、trueで死亡及び魂が飛び出す
    private bool _isDeathEnemy = false;


    private void Start()
    {
        _soulObject.SetActive(false);
    }
    public void DeathProtocol()
    {
        _soulObject.SetActive(true);
        _isDeathEnemy = true;
    }

    private void FixedUpdate()
    {
        if(_isDeathEnemy)
        {
            float yPos = _soulObject.transform.position.y;
            _saveYPos += _upDistance;
            _upDistance += _upSpeed;
            if(_saveYPos <= _limitDistance)
            {
                _soulObject.transform.position = new Vector2(_soulObject.transform.position.x, _saveYPos);  
            }
        }
    }

}
