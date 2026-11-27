using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;

/// <summary>
/// ルーム検索・作成・参加のマッチング操作を担うインターフェース (ISP)。
/// </summary>
public interface IRoomMatchmakingService
{
    UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers, RelayProtocol protocol = RelayProtocol.DTLS);
    UniTask<List<ISessionInfo>> SearchRoomSessionsAsync();
    UniTask<bool> JoinRoomSessionAsync(ISessionInfo targetRoom);
}

/// <summary>
/// 参加中ルームセッションの参照・退出・準備完了操作を担うインターフェース (ISP)。
/// </summary>
public interface IRoomSessionControl
{
    ISession CurrentSession { get; }
    event Action OnSessionChanged;

    UniTask LeaveRoomSessionAsync();
    UniTask SetReadyStatusAsync(bool isReady);
}

/// <summary>
/// セッション内のプレイヤー属性・ルーム状態の同期を担うインターフェース (ISP)。
/// </summary>
public interface ISessionMetadataUpdater
{
    UniTask UpdatePlayerNameAsync(string playerName);
    UniTask UpdatePlayerSkinIdAsync(int skinId);
    UniTask UpdateSessionGameStatusAsync(bool isInGame);
}

/// <summary>
/// ロビーセッション機能全体を統合するファサードインターフェース (ISP / DIP)。
/// </summary>
public interface ILobbySessionService : IRoomMatchmakingService, IRoomSessionControl, ISessionMetadataUpdater
{
}
