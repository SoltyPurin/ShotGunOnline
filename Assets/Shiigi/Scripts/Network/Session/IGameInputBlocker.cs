using System;
using UnityEngine;

/// <summary>
/// 切断エラー時などにゲームプレイ入力を一括遮断するためのインターフェース。
/// </summary>
public interface IGameInputBlocker
{
    void BlockAllGameInputs();
}

/// <summary>
/// シーン内のプレイヤー操作コンポーネントおよびポーズ入力を安全に停止する既定実装。
/// </summary>
public sealed class SceneGameInputBlocker : IGameInputBlocker
{
    private const string PlayerTag = "Player";

    public void BlockAllGameInputs()
    {
        try
        {
            var playerObjs = GameObject.FindGameObjectsWithTag(PlayerTag);
            for (int i = 0; i < playerObjs.Length; i++)
            {
                var player = playerObjs[i];
                if (player == null) continue;

                if (player.TryGetComponent<PlayerStateManager>(out var stateManager))
                {
                    try
                    {
                        if (stateManager.IsOwner)
                        {
                            stateManager.MovieState();
                        }
                    }
                    catch
                    {
                        // 切断時のNetworkVariable書き込み例外を安全に吸収
                    }
                }

                if (player.TryGetComponent<PlayerMove>(out var move)) move.enabled = false;
                if (player.TryGetComponent<InputPlayerShot>(out var shot)) shot.enabled = false;
                if (player.TryGetComponent<PlayerAiming>(out var aiming)) aiming.enabled = false;
                if (player.TryGetComponent<InputChangeState>(out var ult)) ult.enabled = false;
                if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }

            var pauseInputs = UnityEngine.Object.FindObjectsByType<InputPause>();
            for (int i = 0; i < pauseInputs.Length; i++)
            {
                if (pauseInputs[i] != null)
                {
                    pauseInputs[i].enabled = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MSM]: ゲーム入力無効化中に警告: {ex.Message}");
        }
    }
}
