using Cysharp.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

/// <summary>
/// Unity Gaming Services の初期化と匿名サインインを行う。
/// ビルド種別に応じて UGS 環境（development / production）を切り替える。
/// </summary>
public class UnityServicesInitializer : IUnityServicesInitializer
{
    public const string DevelopmentEnvironment = "development";
    public const string ProductEnvironment = "production";

    private UniTask<bool>? _initializeTask;

    public bool IsInitialized { get; private set; }

    public UniTask<bool> InitializeAsync()
    {
        if (IsInitialized) return UniTask.FromResult(true);

        // 多重初期化・多重匿名サインインによる競合を防ぐため、進行中タスクを共有する
        if (_initializeTask.HasValue)
        {
            return _initializeTask.Value;
        }

        var task = InitializeCoreAsync().Preserve();
        if (task.Status == UniTaskStatus.Pending)
        {
            _initializeTask = task;
        }
        return task;
    }

    private async UniTask<bool> InitializeCoreAsync()
    {
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
            Debug.LogError($"[UGS] Unity Servicesの初期化に失敗: {ex.Message}");
            return false;
        }
        finally
        {
            _initializeTask = null;
        }
    }
}

