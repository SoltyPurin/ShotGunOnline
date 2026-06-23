using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public void StartHost()
    {
        Unity.Netcode.NetworkManager.Singleton.StartHost();
    }

    public void StartCliant()
    {
        Unity.Netcode.NetworkManager.Singleton.StartClient();
    }
}
