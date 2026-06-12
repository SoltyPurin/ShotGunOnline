using UnityEngine;
using Unity.Netcode;

public class FollowingCameraToBurrel : MonoBehaviour
{
    #region 変数群
    [SerializeField] private GameObject _burrel = default;
    [SerializeField] private GameObject _player = default;
    private GameObject _boss = default;
    private GameObject _midiumBoss = default;
    [SerializeField] private float _followSpeed = 2;
    [SerializeField] private float _bossFollowSpeed = 20.0f;
    [SerializeField] private float _zoomFactor = 0.5f;
    [SerializeField] private float _maxSize = 20.0f;
    [SerializeField] private float _minSize = 10.0f;
    [SerializeField] private float _movieSize = 8.0f;
    private float _zoomSpeed = 10.0f;
    private PlayerStateManager _playerState = default;

    private Camera _camera;
    private bool _isBossWave = false;
    public bool IsBossWave
    {
        set { _isBossWave = value; }
    }
    private bool _isEnd = false;
    public bool IsEnd
    {
        set { _isEnd = value; }
    }
    private bool _isMovie = false;
    public bool IsMovie
    {
        set { _isMovie = value; }
    }
    private bool _isEndMovie = false;
    public bool IsEndMovie
    {
        set { _isEndMovie = value; }
    }

    private bool _isMidiumBoss = false;
    public bool IsMidiumBoss
    {
        set { _isMidiumBoss= value; }
    }

    #endregion
    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
    }
    private void GetPlayer()
    {
        // ネットワーク上の全プレイヤーオブジェクトを探す
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject p in players)
        {
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            // マルチプレイの場合、自分自身のキャラクター（LocalPlayer）のみをカメラの追従対象にする
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _player = p;
                Debug.Log("ローカルプレイヤーのカメラターゲットへの登録に成功しました");
                _playerState = _player.GetComponent<PlayerStateManager>();
                break;
            }
        }
    }
    private void FixedUpdate()
    {
        if (_player == null)
        {
            GetPlayer();
            return; // 見つかるまでは以降のカメラ移動処理をスキップ
        }

        // 2. プレイヤーは見つかったが、コンポーネントのキャッシュがまだなら取得する
        if (_playerState == null)
        {
            _playerState = _player.GetComponent<PlayerStateManager>();
        }

        // 3. 銃銃身（Burrel）がなければ探す
        if (!_burrel)
        {
            Transform burrelTransform = _player.transform.Find("GunBurrel");
            if (burrelTransform != null) _burrel = burrelTransform.gameObject;
            return;
        }
        if (_playerState.PlayerState == PlayerState.Movie)
        {
            return;
        }
        if (!_burrel)
        {
            return;
        }

        if (_isEndMovie)
        {
            if (_boss == null)
            {
                return;
            }
            //カメラのズームを変える
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _movieSize, Time.deltaTime * _zoomSpeed);

            Vector3 targetPosition = _boss.transform.position;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);
        }
        else if (_isMovie)
        {
            _boss = GameObject.FindGameObjectWithTag("BossSprite");

            if (_boss == null)
            {
                return;
            }

            Vector3 targetPosition = _boss.transform.position;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

        }
        else if (_isBossWave && !_isMovie)//中ボスはこれ参考にしてね
        {

            if (_boss == null)
            {
                _boss = GameObject.FindGameObjectWithTag("BossSprite");
            }

            //カメラの中心をPlayerとBossの真ん中に固定
            Vector3 targetPosition = (_player.transform.position + _boss.transform.position) / 2f;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

            //Playerとの距離によってカメラのズームを変える
            float distance = Vector3.Distance(_player.transform.position, _boss.transform.position);
            float targetSize = Mathf.Clamp(distance * _zoomFactor, _minSize, _maxSize);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, targetSize, Time.deltaTime * _zoomSpeed);
        }
        else if (_isMidiumBoss)
        {
            if (_midiumBoss == null)
            {
                _midiumBoss = GameObject.FindWithTag("MediumArmor");
            }
            //カメラの中心をPlayerとBossの真ん中に固定
            Vector3 targetPosition = (_player.transform.position + _midiumBoss.transform.position) / 2f;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

            //Playerとの距離によってカメラのズームを変える
            float distance = Vector3.Distance(_player.transform.position, _midiumBoss.transform.position);
            float targetSize = Mathf.Clamp(distance * _zoomFactor, _minSize, _maxSize);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, targetSize, Time.deltaTime * _zoomSpeed);
        }
        else
        {
            Vector3 targetPosition = _burrel.transform.position;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _followSpeed * Time.fixedDeltaTime);
        }
    }

    public void MovieMove(Vector2 pos)
    {
        Vector3 targetPosition = pos;
        targetPosition.z = transform.position.z;

        transform.position = targetPosition;
    }
}

