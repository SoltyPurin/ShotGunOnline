using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPopEndReset : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("ボスのState管理")]
    private BossStateManagement _stateManagement = default;
    [SerializeField, Header("ボスのHP管理")]
    private BossHP _bossHP = default;

    private GameObject _camera = default;

    #endregion

    private void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
    }
}
