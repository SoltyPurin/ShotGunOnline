using System;

/// <summary>
/// 頭上ネームプレート等へプレイヤー名・ローカル判定・表示可否を提供するインターフェース (DIP / OCP)。
/// 新しいプレイヤー種別が追加されても UI 側を修正せずに対応可能にします。
/// </summary>
public interface IPlayerNameSource
{
    string PlayerName { get; }
    bool IsLocalControl { get; }
    bool IsNameplateVisible { get; }
    event Action<string> OnPlayerNameChanged;
}
