using UnityEngine;

/// <summary>
/// プレイヤー番号に応じたスポーン地点を提供するインターフェース (DIP / ISP)
/// </summary>
public interface ISpawnPointProvider
{
    Transform GetSpawnPoint(int playerIndex);
}

