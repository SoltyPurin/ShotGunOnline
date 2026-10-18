using System.Collections.Generic;
using Unity.Services.Multiplayer;

/// <summary>
/// セッション内のプレイヤープロパティ（Ready状態など）と開始条件判定のドメインルールを集約するヘルパークラス (SRP)
/// UI層や通信層に文字列キー（"IsReady"）や判定ロジックが散재するのを防ぎます。
/// </summary>
public static class SessionPlayerPropertyHelper
{
    public const string ReadyPropertyKey = "IsReady";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    /// <summary>
    /// 指定したプレイヤーが準備完了状態かどうかを取得する
    /// </summary>
    public static bool IsPlayerReady(IReadOnlyPlayer player)
    {
        if (player?.Properties != null && player.Properties.TryGetValue(ReadyPropertyKey, out var prop))
        {
            return prop.Value == TrueValue;
        }
        return false;
    }

    /// <summary>
    /// 準備完了状態の更新用プロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, PlayerProperty> CreateReadyProperties(bool isReady)
    {
        var value = isReady ? TrueValue : FalseValue;
        return new Dictionary<string, PlayerProperty>
        {
            { ReadyPropertyKey, new PlayerProperty(value, VisibilityPropertyOptions.Member) }
        };
    }

    /// <summary>
    /// ホストがゲームを開始可能かどうか（2人以上かつホスト以外の全員が準備完了）を判定する
    /// </summary>
    public static bool CanHostStartGame(ISession session)
    {
        if (session == null || session.Players == null || session.Players.Count <= 1)
        {
            return false;
        }

        foreach (var player in session.Players)
        {
            if (player.Id == session.Host) continue; // ホスト自身はスキップ

            if (!IsPlayerReady(player))
            {
                return false;
            }
        }

        return true;
    }
}

