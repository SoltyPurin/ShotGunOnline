using UnityEngine;
using UnityEngine.UI;

public class RoomCardUI : MonoBehaviour
{
    [Header("テキスト")] 
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;

    [Header("ボタン")]
    [SerializeField] private Button _joinButton;
}
