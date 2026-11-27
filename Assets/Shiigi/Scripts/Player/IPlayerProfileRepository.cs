/// <summary>
/// プレイヤープロフィールの永続化先を抽象化するリポジトリインターフェース。
/// 現在は PlayerPrefs を利用し、将来的に UGS Cloud Save やオンラインDBへ差し替え可能にします。
/// </summary>
public interface IPlayerProfileRepository
{
    PlayerProfile Load();
    void Save(PlayerProfile profile);
}
