using UnityEngine;

public class PlayerNumberSave : MonoBehaviour
{
    private int _myNumber = default;
    public int MyNumber
    {
        get { return _myNumber; } 
    }


    /// <summary>
    /// プレイヤーの番号を保存する関数。PlayerSkinSyncから呼び出される
    /// </summary>
    /// <param name="number">プレイヤーの番号</param>
    public void SetPlayerNumber(int number)
    {
        _myNumber = number;
    }
}
