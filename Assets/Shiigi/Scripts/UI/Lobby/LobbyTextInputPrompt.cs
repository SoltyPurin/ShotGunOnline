using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 現在の入力デバイス（ゲームパッド or キーボード/マウス）に応じて最適なテキスト入力UI
/// （仮想キーボード または 直接テキスト入力ダイアログ）を選択・起動するファサードクラス (DIP / OCP / SRP)。
/// </summary>
public static class LobbyTextInputPrompt
{
    public static UniTask<(bool Confirmed, string Text)> OpenAsync(
        Canvas parentCanvas,
        string initialText,
        string title,
        int maxLength,
        LobbyVirtualKeyboardUI virtualKeyboardPrefab,
        LobbyTextInputDialogUI textInputDialogPrefab = null,
        bool showPresetsOnVirtualKeyboard = false,
        string[] customPresets = null)
    {
        LobbyInputDeviceTracker.EnsureInitialized();

        if (!LobbyInputDeviceTracker.ShouldUseVirtualKeyboard && textInputDialogPrefab != null)
        {
            return LobbyTextInputDialogUI.OpenAsync(
                parentCanvas,
                initialText: initialText,
                title: title,
                maxLength: maxLength,
                prefab: textInputDialogPrefab);
        }

        return LobbyVirtualKeyboardUI.OpenAsync(
            parentCanvas,
            initialText: initialText,
            title: title,
            maxLength: maxLength,
            prefab: virtualKeyboardPrefab,
            showPresets: showPresetsOnVirtualKeyboard,
            customPresets: customPresets);
    }
}
