using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;

/// <summary>
/// 公開ルーム一覧上の参加可否ステータス種別
/// </summary>
public enum RoomAvailabilityStatus
{
    Waiting,
    Full,
    InGame
}

/// <summary>
/// セッション内のプロパティ（通信プロトコル・Ready状態・Relay接続状態など）と開始・参加条件判定のドメインルールを集約するヘルパークラス (SRP)。
/// UI層や通信層に文字列キーや判定ロジックが散在するのを防ぎます。
/// </summary>
public static class SessionPlayerPropertyHelper
{
    public const string RelayProtocolPropertyKey = "RelayProtocol";
    public const string GameStatusPropertyKey = "GameStatus";
    public const string GameStatusWaitingValue = "Waiting";
    public const string GameStatusInGameValue = "InGame";
    public const string ReadyPropertyKey = "IsReady";
    public const string RelayConnectedPropertyKey = "IsRelayConnected";
    public const string PlayerNamePropertyKey = "PlayerName";
    public const string SkinIdPropertyKey = "SkinId";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    /// <summary>
    /// ルーム作成時に公開する通信プロトコル・ゲーム状態のセッションプロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, SessionProperty> CreateSessionProtocolProperties(RelayProtocol protocol)
    {
        return CreateSessionProperties(protocol, isInGame: false);
    }

    /// <summary>
    /// ルーム作成時に公開するセッションプロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, SessionProperty> CreateSessionProperties(RelayProtocol protocol, bool isInGame = false)
    {
        var gameStatusValue = isInGame ? GameStatusInGameValue : GameStatusWaitingValue;
        return new Dictionary<string, SessionProperty>
        {
            { RelayProtocolPropertyKey, new SessionProperty(protocol.ToString(), VisibilityPropertyOptions.Public) },
            { GameStatusPropertyKey, new SessionProperty(gameStatusValue, VisibilityPropertyOptions.Public) }
        };
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）の現在参加人数を取得する
    /// </summary>
    public static int GetCurrentPlayerCount(ISessionInfo sessionInfo)
    {
        if (sessionInfo == null) return 0;
        return Math.Max(0, sessionInfo.MaxPlayers - sessionInfo.AvailableSlots);
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）が試合中かどうかを返す
    /// </summary>
    public static bool IsSessionInGame(ISessionInfo sessionInfo)
    {
        if (sessionInfo?.Properties != null &&
            sessionInfo.Properties.TryGetValue(GameStatusPropertyKey, out var prop) &&
            !string.IsNullOrEmpty(prop?.Value))
        {
            return string.Equals(prop.Value, GameStatusInGameValue, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）の空き・試合状態を判定して返す
    /// </summary>
    public static RoomAvailabilityStatus GetRoomAvailabilityStatus(ISessionInfo sessionInfo)
    {
        if (IsSessionInGame(sessionInfo)) return RoomAvailabilityStatus.InGame;
        if (sessionInfo == null || sessionInfo.AvailableSlots <= 0) return RoomAvailabilityStatus.Full;
        return RoomAvailabilityStatus.Waiting;
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）に参加可能かどうかを判定する（ドメインルールの一元化）
    /// </summary>
    public static bool CanJoinSession(ISessionInfo sessionInfo)
    {
        return sessionInfo != null &&
               !string.IsNullOrEmpty(sessionInfo.Id) &&
               !sessionInfo.IsLocked &&
               sessionInfo.AvailableSlots > 0 &&
               !IsSessionInGame(sessionInfo);
    }

    /// <summary>
    /// ルーム状態に応じた表示用ラベル文字列を取得する
    /// </summary>
    public static string GetRoomStatusDisplayText(RoomAvailabilityStatus status)
    {
        return status switch
        {
            RoomAvailabilityStatus.InGame => "試合中",
            RoomAvailabilityStatus.Full => "満員",
            _ => "待機中"
        };
    }

    /// <summary>
    /// ルーム情報（ISessionInfo）から表示用のゲーム状態文字列を取得する
    /// </summary>
    public static string GetSessionGameStatusDisplayText(ISessionInfo sessionInfo)
    {
        return IsSessionInGame(sessionInfo) ? "試合中" : "待機中";
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
        if (isHost) return true;
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
    /// プレイヤー名の更新用プロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, PlayerProperty> CreatePlayerNameProperties(string playerName)
    {
        string sanitized = PlayerProfile.SanitizePlayerName(playerName);
        return new Dictionary<string, PlayerProperty>
        {
            { PlayerNamePropertyKey, new PlayerProperty(sanitized, VisibilityPropertyOptions.Member) }
        };
    }

    /// <summary>
    /// スキンIDの更新用プロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, PlayerProperty> CreateSkinIdProperties(int skinId)
    {
        int normalized = Math.Max(0, skinId);
        return new Dictionary<string, PlayerProperty>
        {
            { SkinIdPropertyKey, new PlayerProperty(normalized.ToString(), VisibilityPropertyOptions.Member) }
        };
    }

    /// <summary>
    /// 指定したプレイヤーのスキンIDを取得する（未設定・不正時はフォールバック値を返す）
    /// </summary>
    public static int GetPlayerSkinId(IReadOnlyPlayer player, int fallbackSkinId = 0)
    {
        if (player?.Properties != null &&
            player.Properties.TryGetValue(SkinIdPropertyKey, out var prop) &&
            !string.IsNullOrWhiteSpace(prop?.Value) &&
            int.TryParse(prop.Value, out int parsed) &&
            parsed >= 0)
        {
            return parsed;
        }

        return Math.Max(0, fallbackSkinId);
    }

    /// <summary>
    /// Relay接続完了状態・プレイヤー名・スキンIDをまとめて保存する初期プロパティ辞書を生成する
    /// </summary>
    public static Dictionary<string, PlayerProperty> CreateInitialConnectedPlayerProperties(string playerName, int skinId = 0)
    {
        string sanitized = PlayerProfile.SanitizePlayerName(playerName);
        int normalizedSkinId = Math.Max(0, skinId);
        return new Dictionary<string, PlayerProperty>
        {
            { RelayConnectedPropertyKey, new PlayerProperty(TrueValue, VisibilityPropertyOptions.Member) },
            { PlayerNamePropertyKey, new PlayerProperty(sanitized, VisibilityPropertyOptions.Member) },
            { SkinIdPropertyKey, new PlayerProperty(normalizedSkinId.ToString(), VisibilityPropertyOptions.Member) }
        };
    }

    /// <summary>
    /// 指定したプレイヤーの表示名を取得する（未設定時はプレイヤーID短縮表示にフォールバック）
    /// </summary>
    public static string GetPlayerName(IReadOnlyPlayer player)
    {
        if (player?.Properties != null &&
            player.Properties.TryGetValue(PlayerNamePropertyKey, out var prop) &&
            !string.IsNullOrWhiteSpace(prop?.Value))
        {
            string sanitized = PlayerProfile.SanitizePlayerName(prop.Value);
            if (!string.IsNullOrEmpty(sanitized))
            {
                return sanitized;
            }
        }

        string fallbackId = player?.Id ?? "Unknown";
        if (fallbackId.Length > 8)
        {
            return fallbackId.Substring(0, 8) + "...";
        }
        return fallbackId;
    }

    /// <summary>
    /// ホストがゲームを開始可能かどうか（2人以上かつホスト以外の全員がRelay接続済み＆準備完了）を判定する
    /// </summary>
    public static bool CanHostStartGame(ISession session)
    {
        if (session?.Players == null || session.Players.Count <= 1)
        {
            return false;
        }

        var players = session.Players;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.Id == session.Host) continue;

            if (!IsPlayerRelayConnected(player, isHost: false) || !IsPlayerReady(player))
            {
                return false;
            }
        }

        return true;
    }
}
