using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScan : MonoBehaviour
{
    [SerializeField]
    private BossWaveCreate _waveCreate;
    private GameObject _player = default;
    private PlayerStateManager _playerStateManager = default;

    private GameObject _camera = default;
    private FollowingCameraToBurrel _cameraMove = default;

    private bool _createSW = true;


    private void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();

        _player = GameObject.FindGameObjectWithTag("Player");
        _playerStateManager = _player.GetComponent<PlayerStateManager>();
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && _createSW)
        {
            _waveCreate._waveSW = true;
            _createSW = false;
            this.enabled = false;
            _cameraMove.IsMovie = true;
        }
    }

}
