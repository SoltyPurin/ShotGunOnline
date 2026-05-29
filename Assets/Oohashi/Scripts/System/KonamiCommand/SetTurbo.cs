using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTurbo : MonoBehaviour
{
    [SerializeField, Header("ターボモードを管理するオブジェクト")]
    private GameObject _turboObject = default;
    private void Start()
    {
        if (SaveHardOptionSetting._isTurboOn)
        {
            _turboObject.SetActive(true);
        }
        else
        {
            _turboObject.SetActive(false);
        }
    }
}
