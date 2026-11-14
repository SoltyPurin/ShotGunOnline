using Cysharp.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

/// <summary>
/// Unity Gaming Services の初期化と匿名サインインのみを担当するクラス (SRP)
/// ビルド種別（Editor/Development Build か Release か）に応じて UGS の環境（development / product）を自動分離します。
/// </summary>
public class UnityServicesInitializer : IUnityServicesInitializer
{
    public const string DevelopmentEnvironment = "development";
    public const string ProductEnvironment = "production";

    public bool IsInitialized { get; private set; }

    public async UniTask<bool> InitializeAsync()
    {
        if (IsInitialized) return true;

        try
        {
            var options = new InitializationOptions();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string env = DevelopmentEnvironment;
#else
            string env = ProductEnvironment;
#endif
            options.SetEnvironmentName(env);

            Debug.Log($"[UGS] Unity Services 初期化開始 (Environment: {env})");
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unity Servicesの初期化に失敗: {ex.Message}");
            return false;
        }
    }
}

