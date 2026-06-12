using UnityEngine;

public class NetWorkGameStarter : MonoBehaviour
{
    [SerializeField, Header("Žæ“¾Œn")]
    private WaveManager _waveManager = default;

    private void Start()
    {
        _waveManager.StartWave(0);
    }
}
