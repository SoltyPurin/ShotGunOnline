using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ロビー（集会所）でのプレイヤー移動・向き・アニメーションに関する共通処理。
/// オフライン分身(LobbyLocalAvatar)とネットワークプレイヤー(LobbyNetworkPlayer)の両方から利用します。
/// </summary>
public static class LobbyMovement
{
    /// <summary>
    /// メニュー表示中などに移動入力を止めるためのフラグ（メニュー側から設定する想定）
    /// </summary>
    public static bool IsInputBlocked { get; set; }

    private const float StickDeadZoneSqr = 0.01f;
    private const float FacingThreshold = 0.05f;

    // 向きビットフラグ（ネットワーク同期用 1byte）
    public const byte FacingBackBit = 1 << 0;
    public const byte FacingLeftBit = 1 << 1;

    /// <summary>
    /// 移動入力を取得する（PlayerMove と同じく 左スティック → WASD/矢印キー の順で参照）
    /// </summary>
    public static Vector2 ReadMoveInput()
    {
        if (IsInputBlocked) return Vector2.zero;

        Vector2 input = Vector2.zero;

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input = gamepad.leftStick.ReadValue();
        }

        if (input.sqrMagnitude < StickDeadZoneSqr)
        {
            input = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    /// <summary>
    /// 入力の強さ（0～1）をアニメーション速度として返す
    /// </summary>
    public static float ToAnimSpeed(Vector2 input)
    {
        return Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y));
    }

    /// <summary>
    /// 移動入力から新しい向きビット（正面/背面・右/左）を更新して返す
    /// </summary>
    public static byte UpdateFacingMask(Vector2 input, byte currentMask)
    {
        bool isBack = (currentMask & FacingBackBit) != 0;
        bool isLeft = (currentMask & FacingLeftBit) != 0;

        if (input.y > FacingThreshold) isBack = true;
        else if (input.y < -FacingThreshold) isBack = false;

        if (input.x < -FacingThreshold) isLeft = true;
        else if (input.x > FacingThreshold) isLeft = false;

        byte result = 0;
        if (isBack) result |= FacingBackBit;
        if (isLeft) result |= FacingLeftBit;
        return result;
    }

    /// <summary>
    /// 向きビットに応じて正面/背面スプライトの表示切り替えと左右反転を反映する
    /// </summary>
    public static void ApplyFacing(SpriteRenderer frontSprite, SpriteRenderer backSprite, byte facingMask)
    {
        bool isBack = (facingMask & FacingBackBit) != 0;
        bool isLeft = (facingMask & FacingLeftBit) != 0;
        Quaternion rot = isLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

        if (frontSprite != null)
        {
            frontSprite.enabled = !isBack;
            frontSprite.transform.localRotation = rot;
        }

        if (backSprite != null)
        {
            backSprite.enabled = isBack;
            backSprite.transform.localRotation = rot;
        }
    }

    /// <summary>
    /// 走る/待機アニメーションを反映する（PlayerAnimation が未設定なら何もしない）
    /// </summary>
    public static void ApplyAnimation(PlayerAnimation animation, float animSpeed)
    {
        if (animation == null) return;

        if (animSpeed > 0.01f)
        {
            animation.Run(animSpeed);
        }
        else
        {
            animation.Wait();
        }
    }

    /// <summary>
    /// 子階層から PlayerAnimation / FrontSprite / BackSprite を自動検索して補完する
    /// </summary>
    public static void AutoResolveVisuals(
        Transform root,
        ref PlayerAnimation animation,
        ref SpriteRenderer frontSprite,
        ref SpriteRenderer backSprite)
    {
        if (animation == null)
        {
            animation = root.GetComponentInChildren<PlayerAnimation>(true);
        }

        if (frontSprite == null || backSprite == null)
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (frontSprite == null && sr.name == "FrontSprite") frontSprite = sr;
                else if (backSprite == null && sr.name == "BackSprite") backSprite = sr;
            }
        }
    }
}
