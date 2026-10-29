using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;

/// <summary>
/// ロビーセッションの作成・検索・参加・退出・準備状態を抽象化するインターフェース (ISP / DIP)
/// </summary>
public interface ILobbySessionService
{
    ISession CurrentSession { get; }
    event Action OnSessionChanged;

    UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers);
    UniTask<List<ISessionInfo>> SearchRoomSessionsAsync();
    UniTask<bool> JoinRoomSessionAsync(ISessionInfo targetRoom);
    UniTask LeaveRoomSessionAsync();
    UniTask SetReadyStatusAsync(bool isReady);
}

