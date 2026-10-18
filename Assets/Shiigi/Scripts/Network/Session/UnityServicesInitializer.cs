using Cysharp.Threading.Tasks;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Unity Gaming Services の初期化と匿名サインインのみを担当するクラス (SRP)
/// </summary>
public class UnityServicesInitializer : IUnityServicesInitializer
{
    public bool IsInitialized { get; private set; }

    public async UniTask<bool> InitializeAsync()
    {
        if (IsInitialized) return true;

        try
        {
            await UnityServices.InitializeAsync();
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

