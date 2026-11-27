using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ロビーUI共通のキーボード／ゲームパッド入力判定およびUI補助ヘルパー。
/// </summary>
public static class LobbyUIInputHelper
{
    private const float StickNavigationThresholdSqr = 0.25f;

    /// <summary>
    /// キャンセル操作（Escキー または ゲームパッド東ボタン[B/○]）がこのフレームに押されたか
    /// </summary>
    public static bool WasCancelPressedThisFrame(bool allowKeyboardEscape = true)
    {
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
        {
            return true;
        }
        var keyboard = Keyboard.current;
        return allowKeyboardEscape && keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }

    /// <summary>
    /// メニュー開閉トグル操作（Tab / Esc / Start / Select）がこのフレームに押されたか
    /// </summary>
    public static bool WasMenuTogglePressedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {
            return true;
        }

        var gamepad = Gamepad.current;
        return gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame);
    }

    /// <summary>
    /// フォーカス復帰トリガーとなる方向・決定入力がこのフレームに行われたか
    /// </summary>
    public static bool WasNavigationInputPressedThisFrame()
    {
        if (WasDirectionalInputPressedThisFrame())
        {
            return true;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }

        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.wKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame);
    }

    /// <summary>
    /// 十字キー・左スティック・矢印キーのいずれかの方向入力が行われたか（仮想キーボード等のフォーカス復帰用）
    /// </summary>
    public static bool WasDirectionalInputPressedThisFrame()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null &&
            (gamepad.dpad.up.wasPressedThisFrame ||
             gamepad.dpad.down.wasPressedThisFrame ||
             gamepad.dpad.left.wasPressedThisFrame ||
             gamepad.dpad.right.wasPressedThisFrame ||
             gamepad.leftStick.ReadValue().sqrMagnitude > StickNavigationThresholdSqr))
        {
            return true;
        }

        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame);
    }

    /// <summary>
    /// IME変換中でない状態での Enter / NumpadEnter 確定入力か
    /// </summary>
    public static bool WasImeSafeEnterPressedThisFrame()
    {
        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) &&
               string.IsNullOrEmpty(Input.compositionString);
    }

    /// <summary>
    /// EventSystem で現在有効かつ操作可能なUI要素が選択されているか
    /// </summary>
    public static bool IsCurrentSelectionValid()
    {
        if (EventSystem.current == null) return false;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy) return false;
        return !selected.TryGetComponent<Selectable>(out var sel) || sel.IsInteractable();
    }

    /// <summary>
    /// 負の値を含む任意のインデックスから配列内のスプライトを循環取得する（ポートレート表示共通処理）
    /// </summary>
    public static Sprite SelectCyclicSprite(Sprite[] sprites, int index)
    {
        if (sprites == null || sprites.Length == 0) return null;
        int normalized = ((index % sprites.Length) + sprites.Length) % sprites.Length;
        return sprites[normalized];
    }
}
