using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;

/// <summary>
/// セッション内のプロパティ（通信プロトコル・Ready状態・Relay接続状態など）と開始条件判定のドメインルールを集約するヘルパークラス (SRP)
/// UI層や通信層に文字列キー（"RelayProtocol", "IsReady", "IsRelayConnected"）や判定ロジックが散在するのを防ぎます。
/// </summary>
public static class SessionPlayerPropertyHelper
{
    public const string RelayProtocolPropertyKey = "RelayProtocol";
    public const string ReadyPropertyKey = "IsReady";
    public const string RelayConnectedPropertyKey = "IsRelayConnected";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    /// <summary>
    /// ルーム作成時に公開する通信プロトコルのセッションプロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, SessionProperty> CreateSessionProtocolProperties(RelayProtocol protocol)
    {
        return new Dictionary<string, SessionProperty>
        {
            { RelayProtocolPropertyKey, new SessionProperty(protocol.ToString(), VisibilityPropertyOptions.Public) }
        };
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）から設定されている通信プロトコルを取得する（未設定・不正時は既定値を返す）
    /// </summary>
    public static RelayProtocol GetSessionRelayProtocol(ISessionInfo sessionInfo, RelayProtocol defaultProtocol = RelayProtocol.DTLS)
    {
        if (sessionInfo?.Properties != null &&
            sessionInfo.Properties.TryGetValue(RelayProtocolPropertyKey, out var prop) &&
            !string.IsNullOrEmpty(prop?.Value) &&
            Enum.TryParse<RelayProtocol>(prop.Value, ignoreCase: true, out var parsed) &&
            (parsed == RelayProtocol.DTLS || parsed == RelayProtocol.UDP || parsed == RelayProtocol.WSS))
        {
            return parsed;
        }

        return defaultProtocol;
    }

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
    /// 指定したプレイヤーのRelay接続が完了しているかどうかを取得する
    /// </summary>
    public static bool IsPlayerRelayConnected(IReadOnlyPlayer player, bool isHost = false)
    {
        if (isHost) return true; // ホストはセッション作成完了時点でRelay接続済み
        if (player?.Properties != null && player.Properties.TryGetValue(RelayConnectedPropertyKey, out var prop))
        {
            return prop.Value == TrueValue;
        }
        return false;
    }

    /// <summary>
    /// Relay接続完了状態の更新用プロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, PlayerProperty> CreateRelayConnectedProperties(bool isConnected)
    {
        var value = isConnected ? TrueValue : FalseValue;
        return new Dictionary<string, PlayerProperty>
        {
            { RelayConnectedPropertyKey, new PlayerProperty(value, VisibilityPropertyOptions.Member) }
        };
    }

    /// <summary>
    /// ホストがゲームを開始可能かどうか（2人以上かつホスト以外の全員がRelay接続済み＆準備完了）を判定する
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

            // Relay接続が完了していないプレイヤーがいる場合は開始不可
            if (!IsPlayerRelayConnected(player, isHost: false))
            {
                return false;
            }

            if (!IsPlayerReady(player))
            {
                return false;
            }
        }

        return true;
    }
}
