using UnityEngine;

public class PlayerScanProtocol : MonoBehaviour
{
    [SerializeField]
    private BossWaveCreate _waveCreate;

    private GameObject _camera = default;

    private FollowingCameraToBurrel _cameraMove = default;

    private EnemyCountAndCreateTime _time = default;

    private bool _createSW = true;

    #region íËêî
    private readonly string WAVESCORE = "WaveScore";
    #endregion
    private void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();
        _time = GameObject.FindWithTag(WAVESCORE).GetComponent<EnemyCountAndCreateTime>();
        _time.TimerStop();
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && _createSW)
        {
            _waveCreate._waveSW = true;
            _createSW = false;
            this.enabled = false;
            _cameraMove.IsMovie = true;
            _time.TimerStart();
        }
    }

}
