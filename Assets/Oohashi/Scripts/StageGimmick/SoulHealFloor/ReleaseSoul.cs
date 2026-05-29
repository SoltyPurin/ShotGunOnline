using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReleaseSoul : MonoBehaviour
{
    [SerializeField, Header("生成するソウルのオブジェクト")]
    private GameObject _soulObject = default;
    [SerializeField, Header("何個ソウルを出すか")]
    private int _releaseSoulCount = 7;
    [SerializeField, Header("バラまいてから何秒で回収させるか")]
    private float _collectTime = 1;

    private SEManager _se = default;

    private readonly string SEMANAGER_TAG = "SEManager";

    private void Start()
    {
        _se = GameObject.FindWithTag(SEMANAGER_TAG).GetComponent<SEManager>();
    }
    public void ReleaseProtocol()
    {
        for (int i = 0; i < _releaseSoulCount; i++)
        {
            GameObject coin = Instantiate(_soulObject, transform.position, Quaternion.identity);
            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if(moveCoin != null )
            {
                moveCoin.MoveStart();
                moveCoin.ChangeWaitTimeMethod(_collectTime);
            }
        }
        _se.PlayStepOnSoulFloor();
        Destroy(this.gameObject);  
    }
}
