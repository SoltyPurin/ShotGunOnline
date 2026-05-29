using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

public class TitleButtonManager : MonoBehaviour
{
    [SerializeField, Header("タイトル画面から移動するスクリプト")]
    private MoveScene _moveScene = default;

    [SerializeField, Header("最初のボタン達を管理するオブジェクト")]
    private GameObject _firstButtons = default;

    [SerializeField, Header("BGM管理スクリプト")]
    private BGMControl_Ver2 _bgmControll = default;

    [SerializeField, Header("オプションのボタン達を管理してるオブジェクト")]
    private GameObject _optionButtons = default;

    [SerializeField, Header("オプション画面で最初に選択されるボタン")]
    private GameObject _optionFirstSelectButton = default;

    [SerializeField,Header("最初の画面で最初に選択されるボタン")]
    private GameObject _firstTitleSelectButton = default;

    [SerializeField,Header("本当にゲームを終了するか選択するボタン")]
    private GameObject _reallyExitButtons = default;

    [SerializeField,Header("ゲーム終了をNoというボタン")]
    private GameObject _noExitButton = default;

    [SerializeField,Header("ハードモードのオプションボタン群")]
    private GameObject _hardOptions = default;

    [SerializeField,Header("ハードオプションで最初に選択されるボタン")]
    private GameObject _hardFirstSelectButton =default;

    [SerializeField, Header("スポットライトのアニメーター")]
    private Animator _spotLightAnim = default;

    [SerializeField, Header("キャンバスのグループ")]
    private CanvasGroup _canvasGroup = default;

    [SerializeField, Header("タイムライン")]
    private PlayableDirector _playableDirector = default;

    private void Start()
    {
        Time.timeScale = 1;
    }

    public void GameStart()
    {
        _moveScene.LoadHonpen();
    }

    public void GameStartMovie()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _playableDirector.Play();
        _bgmControll.BGMFade_Out(1, 0);
    }

    public void OpenOption()
    {
        _firstButtons.SetActive(false);
        _optionButtons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_optionFirstSelectButton);
        _spotLightAnim.SetTrigger("Option");
    }

    public void OpenHardOption()
    {
        _firstButtons.SetActive(false);
        _hardOptions.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_hardFirstSelectButton);
    }

    public void ReallyExitOpen()
    {
        _firstButtons.SetActive(false);
        _reallyExitButtons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_noExitButton);
    }

    public void FirstExitButton()
    {
        Application.Quit();
    }

    public void ReturnFirstMenu()
    {
        _firstButtons.SetActive(true);
        _optionButtons.SetActive(false);
        _reallyExitButtons.SetActive(false);
        _hardOptions.SetActive(false);
        EventSystem.current.SetSelectedGameObject(_firstTitleSelectButton);
        _spotLightAnim.SetTrigger("Main");
    }
}
