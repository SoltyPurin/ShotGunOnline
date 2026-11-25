using System;
using System.Text;

/// <summary>
/// プレイヤーのプロフィール情報（名前・将来のスキン/カラー等）を保持するデータモデル。
/// 端末ローカル保存（JSON）および他プレイヤーとの共有の基盤として利用します。
/// </summary>
[Serializable]
public class PlayerProfile
{
    public const int MaxPlayerNameLength = 12;

    public string PlayerName = string.Empty;

    public PlayerProfile()
    {
    }

    public PlayerProfile(string playerName)
    {
        PlayerName = SanitizePlayerName(playerName);
    }

    public PlayerProfile Clone()
    {
        return new PlayerProfile
        {
            PlayerName = PlayerName
        };
    }

    /// <summary>
    /// 入力文字列から制御文字・改行・前後空白を除去し、最大文字数に収めて返す
    /// </summary>
    public static string SanitizePlayerName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string trimmed = rawName.Trim();
        var sb = new StringBuilder(trimmed.Length);
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (char.IsControl(c)) continue;
            sb.Append(c);
            if (sb.Length >= MaxPlayerNameLength) break;
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// 未設定時のデフォルト名（例: Player_4821）を生成する
    /// </summary>
    public static string GenerateDefaultPlayerName()
    {
        int suffix = UnityEngine.Random.Range(1000, 10000);
        return $"Player_{suffix}";
    }
}
