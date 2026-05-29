using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EXOptionViewer : MonoBehaviour
{
    [SerializeField, Header("EXオプションのオブジェクト")]
    private GameObject _exOption = default;
    [SerializeField, Header("Exitボタン")]
    private Button _exitButton = default;
    [SerializeField, Header("アニメーショントリガーをセットするスクリプト")]
    private TriggerChoiceAnim _triggerChoice = default;

    public void OnlineEXOption()
    {
        _exOption.SetActive(true);
    }

    public void OFFLineEXOption()
    {
        _exOption.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_exitButton.gameObject);
            _triggerChoice.TriggerToChoice3();
    }
}
