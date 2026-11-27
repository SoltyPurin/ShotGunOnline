using System;

/// <summary>
/// ネームプレート表示用のプレイヤー名・ローカル判定・表示可否を提供するインターフェース。
/// </summary>
public interface IPlayerNameSource
{
    string PlayerName { get; }
    bool IsLocalControl { get; }
    bool IsNameplateVisible { get; }
    event Action<string> OnPlayerNameChanged;
}
