using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowThePauseMenu : MonoBehaviour
{
    private bool _canShowPause = true;
    [SerializeField, Header("ポーズメニューのオブジェクト群")]
    private GameObject _pauseObject = default;
    [SerializeField,Header("戻るボタン")]
    private GameObject _resumeObject = default;
    [SerializeField, Header("イベントシステム")]
    private EventSystem _eventSystem = default;
    private InputPause _input = default;

    private readonly string PLAYER_TAG = "Player";


    private void Start()
    {
        _pauseObject.SetActive(false);
        _input = GetComponent<InputPause>();
    }
    void Update()
    {
        if(_input.IsPauseing && _canShowPause)
        {
            _canShowPause = false;
            _pauseObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_resumeObject);
        }
        else if(!_input.IsPauseing && !_canShowPause)
        {
            _canShowPause=true;
            _pauseObject.SetActive(false);
        }
    }
}
