using System;
using UnityEngine;

/// <summary>
/// PlayerPrefs に JSON 形式で PlayerProfile を保存・読み込みするリポジトリ実装。
/// </summary>
public class PlayerPrefsProfileRepository : IPlayerProfileRepository
{
    private const string ProfilePrefsKey = "ShotGunOnline.PlayerProfile";

    public PlayerProfile Load()
    {
        PlayerProfile profile = null;

        if (PlayerPrefs.HasKey(ProfilePrefsKey))
        {
            string json = PlayerPrefs.GetString(ProfilePrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    profile = JsonUtility.FromJson<PlayerProfile>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerProfile] 保存データの読み込みに失敗したため初期化します: {ex.Message}");
                }
            }
        }

        if (profile == null)
        {
            return null;
        }

        profile.PlayerName = PlayerProfile.SanitizePlayerName(profile.PlayerName);
        return string.IsNullOrEmpty(profile.PlayerName) ? null : profile;
    }

    public void Save(PlayerProfile profile)
    {
        if (profile == null) return;

        string json = JsonUtility.ToJson(profile);
        PlayerPrefs.SetString(ProfilePrefsKey, json);
        PlayerPrefs.Save();
    }
}
