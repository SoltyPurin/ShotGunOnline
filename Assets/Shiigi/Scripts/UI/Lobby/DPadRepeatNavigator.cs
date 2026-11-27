using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッド十字キー（D-Pad）の押下・リピート移動イベントを発行する。
/// </summary>
public sealed class DPadRepeatNavigator
{
    private MoveDirection _lastDirection;
    private float _nextRepeatTime;
    private EventSystem _cachedEventSystem;
    private AxisEventData _cachedAxisData;

    public void Reset()
    {
        _lastDirection = MoveDirection.None;
    }

    public void Process(bool canNavigate, Func<Transform, bool> isOwnedByView, Func<GameObject> getDefaultFocusTarget)
    {
        if (!canNavigate || EventSystem.current == null || Gamepad.current == null)
        {
            _lastDirection = MoveDirection.None;
            return;
        }

        var dpad = Gamepad.current.dpad;
        MoveDirection pressedDir = ResolveDirection(
            dpad.up.wasPressedThisFrame,
            dpad.down.wasPressedThisFrame,
            dpad.left.wasPressedThisFrame,
            dpad.right.wasPressedThisFrame);

        MoveDirection heldDir = ResolveDirection(
            dpad.up.isPressed,
            dpad.down.isPressed,
            dpad.left.isPressed,
            dpad.right.isPressed);

        if (pressedDir != MoveDirection.None)
        {
            _lastDirection = pressedDir;
            _nextRepeatTime = Time.unscaledTime + LobbyUIInputModuleConfigurator.DpadRepeatDelay;
            ExecuteMove(pressedDir, isOwnedByView, getDefaultFocusTarget);
            return;
        }

        if (heldDir != MoveDirection.None)
        {
            if (heldDir != _lastDirection)
            {
                _lastDirection = heldDir;
                _nextRepeatTime = Time.unscaledTime + LobbyUIInputModuleConfigurator.DpadRepeatDelay;
                ExecuteMove(heldDir, isOwnedByView, getDefaultFocusTarget);
            }
            else if (Time.unscaledTime >= _nextRepeatTime)
            {
                _nextRepeatTime = Time.unscaledTime + LobbyUIInputModuleConfigurator.DpadRepeatRate;
                ExecuteMove(heldDir, isOwnedByView, getDefaultFocusTarget);
            }
            return;
        }

        _lastDirection = MoveDirection.None;
    }

    private static MoveDirection ResolveDirection(bool up, bool down, bool left, bool right)
    {
        if (up && !down) return MoveDirection.Up;
        if (down && !up) return MoveDirection.Down;
        if (left && !right) return MoveDirection.Left;
        if (right && !left) return MoveDirection.Right;
        return MoveDirection.None;
    }

    private void ExecuteMove(
        MoveDirection dir,
        Func<Transform, bool> isOwnedByView,
        Func<GameObject> getDefaultFocusTarget)
    {
        var eventSystem = EventSystem.current;
        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy || !isOwnedByView(selected.transform))
        {
            GameObject fallback = getDefaultFocusTarget();
            if (fallback != null)
            {
                eventSystem.SetSelectedGameObject(fallback);
            }
            return;
        }

        if (_cachedAxisData == null || _cachedEventSystem != eventSystem)
        {
            _cachedEventSystem = eventSystem;
            _cachedAxisData = new AxisEventData(eventSystem);
        }

        _cachedAxisData.Reset();
        _cachedAxisData.moveDir = dir;
        _cachedAxisData.moveVector = dir switch
        {
            MoveDirection.Up => Vector2.up,
            MoveDirection.Down => Vector2.down,
            MoveDirection.Left => Vector2.left,
            MoveDirection.Right => Vector2.right,
            _ => Vector2.zero
        };

        ExecuteEvents.ExecuteHierarchy(selected, _cachedAxisData, ExecuteEvents.moveHandler);
    }
}
