using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 直近の操作デバイスがゲームパッド（コントローラー）かキーボード／マウスかを追跡する軽量ヘルパー。
/// コントローラーがPCに接続されたままキーボード／マウスで操作している場合でも誤判定しないよう、
/// 実際の入力イベントに基づいてアクティブデバイスを切り替えます。
/// </summary>
public static class LobbyInputDeviceTracker
{
    private const float StickThreshold = 0.35f;
    private const float MouseMoveThresholdSqr = 9f; // 3px 以上動いたときのみマウス操作とみなす

    public static bool IsUsingGamepad { get; private set; }
    public static bool ShouldUseVirtualKeyboard => IsUsingGamepad && Gamepad.current != null;
    public static event Action<bool> OnInputModeChanged;

    private static bool _isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        if (_isInitialized)
        {
            InputSystem.onEvent -= HandleInputEvent;
            Application.quitting -= HandleApplicationQuitting;
            _isInitialized = false;
        }
        IsUsingGamepad = false;
        OnInputModeChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void EnsureInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        InputSystem.onEvent += HandleInputEvent;
        Application.quitting += HandleApplicationQuitting;
    }

    private static void HandleApplicationQuitting()
    {
        if (!_isInitialized) return;
        InputSystem.onEvent -= HandleInputEvent;
        Application.quitting -= HandleApplicationQuitting;
        _isInitialized = false;
    }

    private static void HandleInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == null) return;
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        if (device is Gamepad gamepad)
        {
            if (IsUsingGamepad) return;
            if (HasMeaningfulGamepadInput(gamepad, eventPtr))
            {
                SetUsingGamepad(true);
            }
            return;
        }

        if (device is Keyboard keyboard)
        {
            if (!IsUsingGamepad) return;
            if (HasMeaningfulKeyboardInput(keyboard, eventPtr))
            {
                SetUsingGamepad(false);
            }
            return;
        }

        if (device is Mouse mouse)
        {
            if (!IsUsingGamepad) return;
            if (HasMeaningfulMouseInput(mouse, eventPtr))
            {
                SetUsingGamepad(false);
            }
        }
    }

    private static bool HasMeaningfulGamepadInput(Gamepad gamepad, InputEventPtr eventPtr)
    {
        if (gamepad.buttonSouth.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.buttonEast.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.buttonWest.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.buttonNorth.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.startButton.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.selectButton.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.leftShoulder.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.rightShoulder.ReadValueFromEvent(eventPtr) > 0.5f ||
            gamepad.leftTrigger.ReadValueFromEvent(eventPtr) > 0.35f ||
            gamepad.rightTrigger.ReadValueFromEvent(eventPtr) > 0.35f)
        {
            return true;
        }

        Vector2 dpad = gamepad.dpad.ReadValueFromEvent(eventPtr);
        if (dpad.sqrMagnitude > 0.25f) return true;

        Vector2 leftStick = gamepad.leftStick.ReadValueFromEvent(eventPtr);
        if (leftStick.sqrMagnitude > StickThreshold * StickThreshold) return true;

        Vector2 rightStick = gamepad.rightStick.ReadValueFromEvent(eventPtr);
        return rightStick.sqrMagnitude > StickThreshold * StickThreshold;
    }

    private static bool HasMeaningfulKeyboardInput(Keyboard keyboard, InputEventPtr eventPtr)
    {
        var keys = keyboard.allKeys;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].ReadValueFromEvent(eventPtr) > 0.5f)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasMeaningfulMouseInput(Mouse mouse, InputEventPtr eventPtr)
    {
        if (mouse.leftButton.ReadValueFromEvent(eventPtr) > 0.5f ||
            mouse.rightButton.ReadValueFromEvent(eventPtr) > 0.5f ||
            mouse.middleButton.ReadValueFromEvent(eventPtr) > 0.5f)
        {
            return true;
        }

        Vector2 scroll = mouse.scroll.ReadValueFromEvent(eventPtr);
        if (scroll.sqrMagnitude > 0.01f) return true;

        Vector2 delta = mouse.delta.ReadValueFromEvent(eventPtr);
        return delta.sqrMagnitude >= MouseMoveThresholdSqr;
    }

    private static void SetUsingGamepad(bool usingGamepad)
    {
        if (IsUsingGamepad == usingGamepad) return;
        IsUsingGamepad = usingGamepad;
        OnInputModeChanged?.Invoke(IsUsingGamepad);
    }
}
