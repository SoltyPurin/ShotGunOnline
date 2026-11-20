using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// ロビー画面の各UIパネル・ダイアログの基底クラス。
/// UISettingsConfig に基づくフェード＆ポップアニメーションとSE自動再生機能を提供します。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class LobbyViewBase : MonoBehaviour
{
    [Header("UIタイプ設定")]
    [Tooltip("パネルの表示タイプ（標準ウィンドウ・モーダルダイアログなど）。UISettingsConfig の設定が一括適用されます。")]
    [SerializeField] protected UIViewType _viewType = UIViewType.DefaultWindow;

    [Tooltip("true の場合、タイプ設定ではなく下記の個別カスタム設定を優先します")]
    [SerializeField] protected bool _overrideCustom = false;

    [Header("個別カスタム設定（_overrideCustom または Custom 時に有効）")]
    [Tooltip("表示アニメーションの時間（秒）")]
    [SerializeField] protected float _showDuration = 0.16f;

    [Tooltip("非表示アニメーションの時間（秒）")]
    [SerializeField] protected float _hideDuration = 0.12f;

    [Tooltip("表示時にわずかに拡大するポップ演出を行うか")]
    [SerializeField] protected bool _useScalePop = true;

    [Tooltip("ポップ演出の開始スケール")]
    [SerializeField] protected float _startScale = 0.95f;

    [Tooltip("表示開始時に鳴らすSE")]
    [SerializeField] protected UISoundType _showSound = UISoundType.None;

    [Tooltip("非表示開始時に鳴らすSE")]
    [SerializeField] protected UISoundType _hideSound = UISoundType.None;

    [Header("自動化設定")]
    [Tooltip("子階層のButtonにUIButtonSoundがなければ自動付与する")]
    [SerializeField] protected bool _autoAttachButtonSounds = true;

    protected CanvasGroup _canvasGroup;
    protected RectTransform _rectTransform;

    private static readonly List<LobbyViewBase> _activeViews = new();
    private static int _liveViewCount;
    private static InputSystemUIInputModule _configuredUiModule;
    private static InputActionReference _originalMoveActionRef;
    private static InputActionAsset _dpadFilteredAsset;
    private static InputAction _dpadFilteredMoveAction;
    private static InputActionReference _dpadFilteredMoveActionRef;

    private const float DpadRepeatDelay = 0.22f;
    private const float DpadRepeatRate = 0.06f;
    private MoveDirection _dpadLastDirection = MoveDirection.None;
    private float _dpadNextRepeatTime;

    public static LobbyViewBase TopActiveView
    {
        get
        {
            PruneDestroyedViews();
            return _activeViews.Count > 0 ? _activeViews[_activeViews.Count - 1] : null;
        }
    }

    public static bool IsModalOpen
    {
        get
        {
            var top = TopActiveView;
            return top != null && top._viewType == UIViewType.ModalDialog;
        }
    }

    public static event Action OnActiveViewStackChanged;

    private CancellationTokenSource _animCts;
    private bool _isVisible;
    private bool _hasAwakened;

    public bool IsVisible => _isVisible;
    public bool IsTopActiveView => TopActiveView == this;

    public UIViewType ViewType
    {
        get => _viewType;
        set => _viewType = value;
    }

    public bool OverrideCustom
    {
        get => _overrideCustom;
        set => _overrideCustom = value;
    }

    public ViewTypeStyle CurrentStyle
    {
        get
        {
            if (_overrideCustom || _viewType == UIViewType.Custom)
            {
                return new ViewTypeStyle
                {
                    ShowDuration = _showDuration,
                    HideDuration = _hideDuration,
                    UseScalePop = _useScalePop,
                    StartScale = _startScale,
                    ShowSound = _showSound,
                    HideSound = _hideSound
                };
            }
            return UISettingsConfig.Instance.GetViewStyle(_viewType);
        }
    }

    protected virtual void Awake()
    {
        if (!_hasAwakened)
        {
            _hasAwakened = true;
            _liveViewCount++;
        }

        EnsureComponents();
        ConfigureUIInputRepeat();

        if (_autoAttachButtonSounds)
        {
            AutoAttachButtonSounds();
        }
    }

    private static void ConfigureUIInputRepeat()
    {
        if (EventSystem.current == null) return;

        var uiModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        if (uiModule == null)
        {
            uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        }
        if (uiModule != null)
        {
            uiModule.moveRepeatDelay = DpadRepeatDelay;
            uiModule.moveRepeatRate = DpadRepeatRate;
            EnsureDpadFilteredFromUIModule(uiModule);
        }
    }

    private static void EnsureDpadFilteredFromUIModule(InputSystemUIInputModule uiModule)
    {
        if (uiModule == null || _configuredUiModule == uiModule) return;
        if (uiModule.move == null || uiModule.move.action == null) return;

        // ロビーUI表示中はUpdate()でD-Padを直接処理するため、UIInputModule側のMoveからD-Padのみ除外して二重移動を防ぐ。
        // また、DefaultInputActionsのMoveはPassThrough型のため、ゲームパッド接続時にスティックの微小ノイズ／静止値(0,0)が
        // キーボード入力を上書きしてレスポンスを低下させる問題がある。
        // Value型(Vector2)＋スティックデッドゾーン強化にすることで、コントローラー接続中でもキーボード入力が干渉されないようにする。
        _originalMoveActionRef = uiModule.move;
        InputAction originalAction = uiModule.move.action;

        _dpadFilteredAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        _dpadFilteredAsset.name = "LobbyDpadFilteredUIInput";
        string mapName = originalAction.actionMap != null ? originalAction.actionMap.name : "UI";
        var map = new InputActionMap(mapName);
        _dpadFilteredMoveAction = map.AddAction(originalAction.name, InputActionType.Value);
        _dpadFilteredMoveAction.expectedControlType = "Vector2";

        var bindings = originalAction.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding b = bindings[i];
            if (!string.IsNullOrEmpty(b.path) &&
                b.path.IndexOf("dpad", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(b.path) &&
                (b.path.IndexOf("leftStick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 b.path.IndexOf("rightStick", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string deadzoneProcessor = b.isPartOfComposite
                    ? "axisDeadzone(min=0.35,max=0.925)"
                    : "stickDeadzone(min=0.35,max=0.925)";
                b.processors = string.IsNullOrEmpty(b.processors)
                    ? deadzoneProcessor
                    : $"{b.processors},{deadzoneProcessor}";
            }

            _dpadFilteredMoveAction.AddBinding(b);
        }

        _dpadFilteredAsset.AddActionMap(map);
        _dpadFilteredMoveAction.Enable();
        _dpadFilteredMoveActionRef = InputActionReference.Create(_dpadFilteredMoveAction);
        uiModule.move = _dpadFilteredMoveActionRef;
        _configuredUiModule = uiModule;
    }

    private static void RestoreOriginalUIModuleMove()
    {
        if (_configuredUiModule != null && _originalMoveActionRef != null)
        {
            _configuredUiModule.move = _originalMoveActionRef;
        }

        if (_dpadFilteredMoveAction != null)
        {
            _dpadFilteredMoveAction.Disable();
            _dpadFilteredMoveAction = null;
        }

        if (_dpadFilteredMoveActionRef != null)
        {
            Destroy(_dpadFilteredMoveActionRef);
            _dpadFilteredMoveActionRef = null;
        }

        if (_dpadFilteredAsset != null)
        {
            Destroy(_dpadFilteredAsset);
            _dpadFilteredAsset = null;
        }

        _configuredUiModule = null;
        _originalMoveActionRef = null;
    }

    private void EnsureComponents()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void OnDestroy()
    {
        CancelCurrentAnimation();
        RemoveActiveView(this);

        if (_hasAwakened)
        {
            _hasAwakened = false;
            _liveViewCount = Mathf.Max(0, _liveViewCount - 1);
            if (_liveViewCount == 0)
            {
                RestoreOriginalUIModuleMove();
            }
        }
    }

    protected virtual void Update()
    {
        ProcessDPadNavigation();
    }

    protected void ProcessDPadNavigation()
    {
        if (!_isVisible || !IsTopActiveView || _canvasGroup == null || !_canvasGroup.interactable)
        {
            _dpadLastDirection = MoveDirection.None;
            return;
        }

        if (EventSystem.current == null || Gamepad.current == null)
        {
            _dpadLastDirection = MoveDirection.None;
            return;
        }

        var dpad = Gamepad.current.dpad;
        bool upPressed = dpad.up.wasPressedThisFrame;
        bool downPressed = dpad.down.wasPressedThisFrame;
        bool leftPressed = dpad.left.wasPressedThisFrame;
        bool rightPressed = dpad.right.wasPressedThisFrame;

        MoveDirection pressedThisFrameDir = ResolvePressedDirection(upPressed, downPressed, leftPressed, rightPressed);
        MoveDirection heldDir = ResolvePressedDirection(dpad.up.isPressed, dpad.down.isPressed, dpad.left.isPressed, dpad.right.isPressed);

        if (pressedThisFrameDir != MoveDirection.None)
        {
            _dpadLastDirection = pressedThisFrameDir;
            _dpadNextRepeatTime = Time.unscaledTime + DpadRepeatDelay;
            ExecuteDPadMove(pressedThisFrameDir);
            return;
        }

        if (heldDir != MoveDirection.None)
        {
            if (heldDir != _dpadLastDirection)
            {
                _dpadLastDirection = heldDir;
                _dpadNextRepeatTime = Time.unscaledTime + DpadRepeatDelay;
                ExecuteDPadMove(heldDir);
            }
            else if (Time.unscaledTime >= _dpadNextRepeatTime)
            {
                _dpadNextRepeatTime = Time.unscaledTime + DpadRepeatRate;
                ExecuteDPadMove(heldDir);
            }
            return;
        }

        _dpadLastDirection = MoveDirection.None;
    }

    private static MoveDirection ResolvePressedDirection(bool up, bool down, bool left, bool right)
    {
        if (up && !down) return MoveDirection.Up;
        if (down && !up) return MoveDirection.Down;
        if (left && !right) return MoveDirection.Left;
        if (right && !left) return MoveDirection.Right;
        return MoveDirection.None;
    }

    private void ExecuteDPadMove(MoveDirection dir)
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy || !IsOwnedByThisView(selected.transform))
        {
            GameObject fallback = GetDefaultFocusTarget();
            if (fallback != null)
            {
                EventSystem.current.SetSelectedGameObject(fallback);
            }
            return;
        }

        var axisData = new AxisEventData(EventSystem.current)
        {
            moveDir = dir,
            moveVector = dir switch
            {
                MoveDirection.Up => Vector2.up,
                MoveDirection.Down => Vector2.down,
                MoveDirection.Left => Vector2.left,
                MoveDirection.Right => Vector2.right,
                _ => Vector2.zero
            }
        };

        ExecuteEvents.ExecuteHierarchy(selected, axisData, ExecuteEvents.moveHandler);
    }

    protected virtual void LateUpdate()
    {
        if (!_isVisible || !IsTopActiveView || _canvasGroup == null || !_canvasGroup.interactable) return;
        if (EventSystem.current == null) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && !IsOwnedByThisView(selected.transform))
        {
            var fallback = GetDefaultFocusTarget();
            EventSystem.current.SetSelectedGameObject(fallback);
        }
    }

    protected virtual bool IsOwnedByThisView(Transform target)
    {
        return target != null && target.IsChildOf(transform);
    }

    /// <summary>
    /// パネルをアニメーション（フェードイン＋ポップ）付きで表示する
    /// </summary>
    public async UniTask ShowAsync(CancellationToken cancellationToken = default)
    {
        CancelCurrentAnimation();
        _animCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            this.GetCancellationTokenOnDestroy());

        var ct = _animCts.Token;

        _isVisible = true;
        gameObject.SetActive(true);
        PushActiveView(this);

        // 表示アニメーション中は誤操作を防ぐため入力を遮断
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        var style = CurrentStyle;
        UISound.Play(style.ShowSound);
        OnShowStarted();

        if (style.ShowDuration > 0f)
        {
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = style.UseScalePop ? new Vector3(style.StartScale, style.StartScale, 1f) : targetScale;

            while (elapsed < style.ShowDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / style.ShowDuration);
                // EaseOutQuad (1 - (1-t)^2) で滑らかに開く
                float eased = 1f - (1f - t) * (1f - t);

                _canvasGroup.alpha = eased;
                if (style.UseScalePop)
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        _canvasGroup.alpha = 1f;
        _rectTransform.localScale = Vector3.one;
        bool isTop = IsTopActiveView;
        _canvasGroup.interactable = isTop;
        _canvasGroup.blocksRaycasts = true;
        OnActiveInteractableChanged(isTop);

        OnShowCompleted();
        if (isTop)
        {
            RestoreDefaultFocus();
        }
    }

    /// <summary>
    /// パネルをアニメーション（フェードアウト）付きで非表示にする
    /// </summary>
    public async UniTask HideAsync(CancellationToken cancellationToken = default)
    {
        if (!_isVisible && !gameObject.activeSelf) return;

        CancelCurrentAnimation();
        _animCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            this.GetCancellationTokenOnDestroy());

        var ct = _animCts.Token;

        _isVisible = false;
        // 非表示開始時点で直ちに入力受付を遮断し、アクティブUIスタックから外す
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        OnActiveInteractableChanged(false);
        RemoveActiveView(this);

        var style = CurrentStyle;
        UISound.Play(style.HideSound);
        OnHideStarted();

        if (style.HideDuration > 0f && gameObject.activeInHierarchy)
        {
            float elapsed = 0f;
            Vector3 startScale = _rectTransform.localScale;
            Vector3 endScale = style.UseScalePop ? new Vector3(style.StartScale, style.StartScale, 1f) : startScale;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < style.HideDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / style.HideDuration);
                // EaseInQuad (t^2) でスッと消える
                float eased = t * t;

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (style.UseScalePop)
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(false);

        OnHideCompleted();
    }

    /// <summary>
    /// アニメーションを介さず即座に表示/非表示を切り替える（初期化時用）
    /// </summary>
    public void SetVisibleInstant(bool visible)
    {
        CancelCurrentAnimation();
        _isVisible = visible;

        EnsureComponents();

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(visible);

        if (visible)
        {
            PushActiveView(this);
            bool isTop = IsTopActiveView;
            _canvasGroup.interactable = isTop;
            OnActiveInteractableChanged(isTop);
        }
        else
        {
            _canvasGroup.interactable = false;
            OnActiveInteractableChanged(false);
            RemoveActiveView(this);
        }
    }

    /// <summary>
    /// 子階層にある全Buttonを検索し、UIButtonSoundが未アタッチなら自動付与する
    /// （ボタン名からUIButtonTypeを自動設定）
    /// </summary>
    public void AutoAttachButtonSounds()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            if (btn.GetComponent<UIButtonSound>() != null) continue;

            var soundComp = btn.gameObject.AddComponent<UIButtonSound>();
            soundComp.ButtonType = UIButtonSound.InferTypeFromName(btn.name);
        }
    }

    private void CancelCurrentAnimation()
    {
        if (_animCts != null)
        {
            _animCts.Cancel();
            _animCts = null;
        }
    }

    public void RestoreDefaultFocus()
    {
        if (!IsTopActiveView || !_isVisible || EventSystem.current == null) return;
        var target = GetDefaultFocusTarget();
        if (target != null && target.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    public virtual GameObject GetDefaultFocusTarget() => null;
    protected virtual void OnActiveInteractableChanged(bool interactable) { }

    private static void PruneDestroyedViews()
    {
        _activeViews.RemoveAll(v => v == null);
    }

    private static void PushActiveView(LobbyViewBase view)
    {
        if (view == null) return;
        PruneDestroyedViews();
        _activeViews.Remove(view);
        _activeViews.Add(view);
        RefreshActiveViewInteractability(restoreFocusOnTop: false);
    }

    private static void RemoveActiveView(LobbyViewBase view)
    {
        PruneDestroyedViews();
        if (_activeViews.Remove(view))
        {
            RefreshActiveViewInteractability(restoreFocusOnTop: true);
        }
    }

    private static void RefreshActiveViewInteractability(bool restoreFocusOnTop)
    {
        PruneDestroyedViews();
        var top = _activeViews.Count > 0 ? _activeViews[_activeViews.Count - 1] : null;
        foreach (var v in _activeViews)
        {
            bool isTop = (v == top);
            v.EnsureComponents();
            v._canvasGroup.interactable = isTop && v._isVisible;
            v.OnActiveInteractableChanged(isTop && v._isVisible);
        }

        if (restoreFocusOnTop && top != null)
        {
            top.RestoreDefaultFocus();
        }

        OnActiveViewStackChanged?.Invoke();
    }

    protected virtual void OnShowStarted() { }
    protected virtual void OnShowCompleted() { }
    protected virtual void OnHideStarted() { }
    protected virtual void OnHideCompleted() { }
}
