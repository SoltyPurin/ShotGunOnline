using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// 各クライアントが自機の移動権限を持つようにする(Unity公式サンプル)
/// URL: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/5294b15d98f934ba4cca11d3d269d9d74229ab99/com.unity.netcode.gameobjects/Components/NetworkTransform.cs#L3182
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; 
    }
}
