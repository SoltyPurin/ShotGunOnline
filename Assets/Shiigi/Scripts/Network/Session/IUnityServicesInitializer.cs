using Cysharp.Threading.Tasks;

/// <summary>
/// Unity Gaming Services (UGS) の初期化および認証を担うインターフェース
/// </summary>
public interface IUnityServicesInitializer
{
    bool IsInitialized { get; }
    UniTask<bool> InitializeAsync();
}

