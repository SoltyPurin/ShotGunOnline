using System;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomDialogUI : MonoBehaviour
{
    [Header("ÉãÅ[ÉÄçÏê¨ópUI")] 
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Dropdown _maxPlayersDropDown;
    [SerializeField] private Button _confirmCreateButton;
    [SerializeField] private Button _cancelDialogButton;

    public event Action OnCancellCallback;
}
