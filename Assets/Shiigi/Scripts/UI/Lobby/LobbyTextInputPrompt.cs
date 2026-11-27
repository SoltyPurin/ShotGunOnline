using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 入力デバイスに応じて仮想キーボードまたは直接入力ダイアログを起動する。
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
