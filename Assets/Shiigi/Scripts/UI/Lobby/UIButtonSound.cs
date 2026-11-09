using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンにアタッチするだけで、クリック時や選択時に自動でUI用SEを再生するコンポーネント。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, ISelectHandler
{
    [Header("クリック時のSE")]
    [SerializeField] private UISoundType _clickSound = UISoundType.Enter;

    [Header("フォーカス（カーソル選択）時のSE")]
    [SerializeField] private bool _playOnSelect = false;

    private Button _button;

    public UISoundType ClickSound
    {
        get => _clickSound;
        set => _clickSound = value;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (_button != null && _button.interactable)
        {
            UISound.Play(_clickSound);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_playOnSelect && _button != null && _button.interactable)
        {
            UISound.Play(UISoundType.CursorMove);
        }
    }
}
