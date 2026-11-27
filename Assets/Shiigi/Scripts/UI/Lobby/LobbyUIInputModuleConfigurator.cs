using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// ロビーUI表示中の InputSystemUIInputModule リピート速度調整および D-Pad/スティック入力フィルタを管理する。
/// </summary>
public static class LobbyUIInputModuleConfigurator
{
    public const float DpadRepeatDelay = 0.22f;
    public const float DpadRepeatRate = 0.06f;

    private static int _activeViewInstanceCount;
    private static InputSystemUIInputModule _configuredUiModule;
    private static InputActionReference _originalMoveActionRef;
    private static InputActionAsset _dpadFilteredAsset;
    private static InputAction _dpadFilteredMoveAction;
    private static InputActionReference _dpadFilteredMoveActionRef;
    private static float _originalMoveRepeatDelay;
    private static float _originalMoveRepeatRate;
    private static bool _hasOriginalRepeatSettings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        RestoreOriginalMoveAction();
        _activeViewInstanceCount = 0;
    }

    public static void RegisterViewInstance()
    {
        _activeViewInstanceCount++;
        ConfigureCurrentEventSystem();
    }

    public static void UnregisterViewInstance()
    {
        _activeViewInstanceCount = Mathf.Max(0, _activeViewInstanceCount - 1);
        if (_activeViewInstanceCount == 0)
        {
            RestoreOriginalMoveAction();
        }
    }

    public static void ConfigureCurrentEventSystem()
    {
        if (EventSystem.current == null) return;

        var uiModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        if (uiModule == null)
        {
            uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        }
        if (uiModule == null) return;

        if (_configuredUiModule != uiModule)
        {
            if (_configuredUiModule != null)
            {
                RestoreOriginalMoveAction();
            }

            _originalMoveRepeatDelay = uiModule.moveRepeatDelay;
            _originalMoveRepeatRate = uiModule.moveRepeatRate;
            _hasOriginalRepeatSettings = true;
            ApplyFilteredMoveActionIfNeeded(uiModule);
        }

        uiModule.moveRepeatDelay = DpadRepeatDelay;
        uiModule.moveRepeatRate = DpadRepeatRate;
    }

    private static void ApplyFilteredMoveActionIfNeeded(InputSystemUIInputModule uiModule)
    {
        if (uiModule == null || _configuredUiModule == uiModule) return;
        if (uiModule.move == null || uiModule.move.action == null) return;

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

    private static void RestoreOriginalMoveAction()
    {
        if (_configuredUiModule != null)
        {
            if (_originalMoveActionRef != null)
            {
                _configuredUiModule.move = _originalMoveActionRef;
            }
            if (_hasOriginalRepeatSettings)
            {
                _configuredUiModule.moveRepeatDelay = _originalMoveRepeatDelay;
                _configuredUiModule.moveRepeatRate = _originalMoveRepeatRate;
            }
        }
        _hasOriginalRepeatSettings = false;

        if (_dpadFilteredMoveAction != null)
        {
            _dpadFilteredMoveAction.Disable();
            _dpadFilteredMoveAction = null;
        }

        if (_dpadFilteredMoveActionRef != null)
        {
            UnityEngine.Object.Destroy(_dpadFilteredMoveActionRef);
            _dpadFilteredMoveActionRef = null;
        }

        if (_dpadFilteredAsset != null)
        {
            UnityEngine.Object.Destroy(_dpadFilteredAsset);
            _dpadFilteredAsset = null;
        }

        _configuredUiModule = null;
        _originalMoveActionRef = null;
    }
}
