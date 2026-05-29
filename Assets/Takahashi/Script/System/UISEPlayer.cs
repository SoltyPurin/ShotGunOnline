using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISEPlayer : MonoBehaviour
{
    [SerializeField, Header("SEマネージャー取得")]
    private SEManager _seManager = default;

    public void ForcusSEPlay()
    {
        _seManager.PlayCursorMoveSound();
    }

    public void EnterSEPlay()
    {
        _seManager.PlayEnterSound();
    }

    public void DisableSEPlay()
    {
        _seManager.PlayCancelSound();
    }
}
