using System;

/// <summary>
/// ゲーム全体でローカルプレイヤーのプロフィール（プレイヤー名等）を保持・更新通知するストア。
/// </summary>
public static class PlayerProfileStore
{
    private static IPlayerProfileRepository _repository = new PlayerPrefsProfileRepository();
    private static PlayerProfile _current;

    /// <summary>プロフィールが更新されたときに発火するイベント</summary>
    public static event Action<PlayerProfile> OnProfileChanged;

    /// <summary>現在のローカルプレイヤープロフィール（初回アクセス時に自動ロード）</summary>
    public static PlayerProfile Current
    {
        get
        {
            EnsureLoaded();
            return _current;
        }
    }

    /// <summary>現在のローカルプレイヤー名</summary>
    public static string PlayerName => Current.PlayerName;

    /// <summary>
    /// プレイヤー名を更新して永続化する。空文字や変更なしの場合は false を返す。
    /// </summary>
    public static bool SetPlayerName(string rawName)
    {
        EnsureLoaded();

        string sanitized = PlayerProfile.SanitizePlayerName(rawName);
        if (string.IsNullOrEmpty(sanitized))
        {
            return false;
        }

        if (string.Equals(_current.PlayerName, sanitized, StringComparison.Ordinal))
        {
            return false;
        }

        _current.PlayerName = sanitized;
        _repository.Save(_current);
        OnProfileChanged?.Invoke(_current.Clone());
        return true;
    }

    /// <summary>
    /// 保存先リポジトリを差し替える（将来のオンライン保存移行やテスト用）
    /// </summary>
    public static void SetRepository(IPlayerProfileRepository repository, bool reloadImmediately = true)
    {
        _repository = repository ?? new PlayerPrefsProfileRepository();
        if (reloadImmediately)
        {
            _current = null;
            EnsureLoaded();
        }
    }

    private static void EnsureLoaded()
    {
        if (_current != null) return;
        _current = _repository.Load() ?? new PlayerProfile(PlayerProfile.GenerateDefaultPlayerName());
    }
}
