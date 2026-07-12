using UnityEngine;

public class RoomSerializer : MonoBehaviour
{

    /// <summary>
    /// 渡したRoomInfoをJson形式に変換する
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private string RoomInfoToJson(RoomInfo data)
    {
        return JsonUtility.ToJson(data);
    }

    /// <summary>
    /// 渡したJson形式の文字列をRoomInfoに変換する
    /// </summary>
    /// <returns></returns>
    private RoomInfo JsonToRoomInfo(string json)
    {
        return JsonUtility.FromJson<RoomInfo>(json);
    }
}
