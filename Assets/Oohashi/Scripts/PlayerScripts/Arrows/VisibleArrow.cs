using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleArrow : MonoBehaviour
{
    #region 変数
    private ShootRange _shoot = default;
    [SerializeField, Header("コンパスを表示するまでの時間")]
    private float _visibleCompasTime = 5f;

    [SerializeField, Header("コンパス")]
    private GameObject _compas = default;

    private float _initDisableTime = 0;
    #endregion

    #region 定数
    private readonly string PLAYER = "Player";
    #endregion

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag(PLAYER);
        _shoot = playerObj.GetComponent<ShootRange>();
        _compas.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (_shoot.IsHit)
        {
            _initDisableTime = 0;
            _compas.SetActive(false);
        }
        else
        {
            _initDisableTime += Time.fixedDeltaTime;
        }

        if(_initDisableTime >= _visibleCompasTime)
        {
            _compas.SetActive(true);
            _initDisableTime = 0;
        }
    }
}
