using System;

/// <summary>
/// インゲームの進行状態を参照・購読するためのインターフェース。
/// </summary>
public interface IGameStateProvider
{
    MP_GameManager.GameState CurrentState { get; }
    event Action<MP_GameManager.GameState, MP_GameManager.GameState> StateChanged;
}

