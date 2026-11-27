using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 確認・エラー表示用のモーダルダイアログUI。
/// </summary>
public class ConfirmDialogUI : LobbyModalDialogBase<bool>
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);

    private const int TopmostSortingOrder = 500;

    private Canvas _overrideCanvas;
    private GraphicRaycaster _graphicRaycaster;
    private Text _yesButtonLabel;
    private string _defaultYesButtonText;
    private bool _isButtonsBound;

    protected override void Awake()
    {
        base.Awake();

        if (_messageText != null)
        {
            _messageText.supportRichText = false;
        }

        if (_yesButton != null)
        {
            _yesButtonLabel = _yesButton.GetComponentInChildren<Text>(true);
            if (_yesButtonLabel != null)
            {
                _defaultYesButtonText = _yesButtonLabel.text;
            }
        }

        UIButtonSound.ConfigureButton(_yesButton, UIButtonType.Default);
        UIButtonSound.ConfigureButton(_noButton, UIButtonType.Cancel);
        BindButtonsIfNeeded();
    }

    protected override void OnDestroy()
    {
        if (_isButtonsBound)
        {
            if (_yesButton != null)
            {
                _yesButton.onClick.RemoveListener(HandleYesClicked);
            }
            if (_noButton != null)
            {
                _noButton.onClick.RemoveListener(HandleNoClicked);
            }
            _isButtonsBound = false;
        }

        base.OnDestroy();
    }

    private void BindButtonsIfNeeded()
    {
        if (_isButtonsBound) return;
        _isButtonsBound = true;

        if (_yesButton != null)
        {
            _yesButton.onClick.AddListener(HandleYesClicked);
        }
        if (_noButton != null)
        {
            _noButton.onClick.AddListener(HandleNoClicked);
        }
    }

    private void HandleYesClicked()
    {
        CloseModalWithResultAsync(true).Forget();
    }

    private void HandleNoClicked()
    {
        CloseModalWithResultAsync(false).Forget();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsAwaitingResult || !gameObject.activeInHierarchy || !IsTopActiveView) return;

        if (LobbyUIInputHelper.WasCancelPressedThisFrame())
        {
            UISound.Play(UISoundType.Cancel);
            bool hasNo = _noButton != null && _noButton.gameObject.activeSelf;
            CloseModalWithResultAsync(!hasNo).Forget();
        }
    }

    public override GameObject GetDefaultFocusTarget()
    {
        if (_yesButton != null && _yesButton.gameObject.activeInHierarchy)
        {
            return _yesButton.gameObject;
        }
        return _noButton != null && _noButton.gameObject.activeInHierarchy ? _noButton.gameObject : null;
    }

    public async UniTask<bool> ShowAsync(string message, bool showNoButton = true, string yesButtonText = null)
    {
        var utcs = BeginResultWait();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        EnsureTopmostSorting();
        EnsureModalBlocker(_blockerColor);

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        BindButtonsIfNeeded();
        ConfigureDialogButtons(showNoButton, yesButtonText);
        FocusYesButton();

        await ShowModalAnimatedSafelyAsync(() =>
        {
            EnsureTopmostSorting();
            FocusYesButton();
        });

        return await utcs.Task;
    }

    private void ConfigureDialogButtons(bool showNoButton, string yesButtonText)
    {
        if (_yesButton != null)
        {
            if (_yesButtonLabel == null)
            {
                _yesButtonLabel = _yesButton.GetComponentInChildren<Text>(true);
                if (_yesButtonLabel != null && string.IsNullOrEmpty(_defaultYesButtonText))
                {
                    _defaultYesButtonText = _yesButtonLabel.text;
                }
            }
            if (_yesButtonLabel != null)
            {
                _yesButtonLabel.text = !string.IsNullOrEmpty(yesButtonText)
                    ? yesButtonText
                    : _defaultYesButtonText;
            }

            _yesButton.navigation = (showNoButton && _noButton != null)
                ? new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = _noButton,
                    selectOnRight = _noButton
                }
                : new Navigation { mode = Navigation.Mode.None };
        }

        if (_noButton != null)
        {
            _noButton.gameObject.SetActive(showNoButton);

            if (showNoButton && _yesButton != null)
            {
                _noButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = _yesButton,
                    selectOnRight = _yesButton
                };
            }
        }
    }

    private void FocusYesButton()
    {
        if (EventSystem.current != null && _yesButton != null && _yesButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(_yesButton.gameObject);
        }
    }

    private void EnsureTopmostSorting()
    {
        transform.SetAsLastSibling();

        if (_overrideCanvas == null && !TryGetComponent(out _overrideCanvas))
        {
            _overrideCanvas = gameObject.AddComponent<Canvas>();
        }

        var rootCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (rootCanvas != null)
        {
            _overrideCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        }

        _overrideCanvas.overrideSorting = true;
        _overrideCanvas.sortingOrder = TopmostSortingOrder;

        if (_graphicRaycaster == null && !TryGetComponent(out _graphicRaycaster))
        {
            _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
