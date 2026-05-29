using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveHardOptionSetting : MonoBehaviour
{
    #region ƒVƒ“ƒOƒ‹ƒgƒ“‰»
    private static SaveHardOptionSetting _instance;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    public static int _heartValue = 5;
    public static bool _isTurboOn = false;

    public void UpdateHeartValue(int heart)
    {
        _heartValue = heart;
    }

    public void UpdateTurboMode(bool isTurbo)
    {
        _isTurboOn = isTurbo;
    }

}
