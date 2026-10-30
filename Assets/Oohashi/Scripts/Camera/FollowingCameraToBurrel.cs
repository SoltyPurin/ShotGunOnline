using UnityEngine;
using Unity.Netcode;

public class FollowingCameraToBurrel : MonoBehaviour
{
    #region 変数
    [SerializeField] private GameObject _burrel = default;
    [SerializeField] private GameObject _player = default;
    private GameObject _boss = default;
    private GameObject _midiumBoss = default;
    private BossStateManagement.BossState _bossState = default;
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

    /// <summary>
    /// 自プレイヤーがスポーンした瞬間に直接カメラターゲットを登録する
    /// </summary>
    public void SetTargetPlayer(GameObject player)
    {
        if (player == null) return;

        _player = player;
        _playerState = _player.GetComponent<PlayerStateManager>();

        // 銃身（GunBurrel）を探す
        Transform burrelTransform = _player.transform.Find("Burrel/GunBurrel");
        if (burrelTransform == null)
        {
            // フォールバック：子オブジェクトから再帰的に GunBurrel または Burrel を探す
            foreach (var child in _player.GetComponentsInChildren<Transform>())
            {
                if (child.name == "GunBurrel" || child.name == "Burrel")
                {
                    burrelTransform = child;
                    break;
                }
            }
        }

        if (burrelTransform != null)
        {
            _burrel = burrelTransform.gameObject;
        }
        else
        {
            // 最悪見つからなくてもプレイヤー自身をターゲットにする
            _burrel = _player;
        }

        // カメラ位置をプレイヤーの現在位置へ即時移動（初期瞬間移動）
        Vector3 initialPos = _burrel.transform.position;
        initialPos.z = transform.position.z;
        transform.position = initialPos;

        Debug.Log($"[FollowingCameraToBurrel] 自プレイヤー ({_player.name}) をカメラターゲットに設定しました (初期位置: {initialPos})");
    }

    private void GetPlayer()
    {
        // プレイヤーが見当たらなかったら探索
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject p in players)
        {
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            // ネットワーク対応のプレイヤーが居たら代入
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _player = p;
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
            return; // プレイヤーがいない時は早期リターン
        }

        if (_playerState == null)
        {
            _playerState = _player.GetComponent<PlayerStateManager>();
        }

        if (!_burrel)
        {
            Transform burrelTransform = _player.transform.Find("Burrel/GunBurrel");
            if (burrelTransform != null) _burrel = burrelTransform.gameObject;
            return;
        }
        if (_playerState.PlayerState == PlayerState.Movie && !_isMovie && !_isEndMovie)
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
                _boss = GameObject.FindGameObjectWithTag("Boss") ?? GameObject.FindGameObjectWithTag("BossSprite");
            }
            if (_boss == null)
            {
                _isEndMovie = false;
                return;
            }
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _movieSize, Time.deltaTime * _zoomSpeed);

            Vector3 targetPosition = _boss.transform.position;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);
        }
        //else if (_isMovie)
        //{
        //    _boss = GameObject.FindGameObjectWithTag("BossSprite");

        //    if (_boss == null)
        //    {
        //        return;
        //    }

        //    Vector3 targetPosition = _boss.transform.position;
        //    targetPosition.z = transform.position.z;

        //    transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

        //}
        else if (_isBossWave && !_isMovie) // ボスのウェーブであればボスとプレイヤーにカメラを合わせる
        {
            if (_boss == null)
            {
                _boss = GameObject.FindGameObjectWithTag("Boss") ?? GameObject.FindGameObjectWithTag("BossSprite");
            }

            // ボスが存在しない（既に倒されて破棄された）場合、ボス追従を解除して通常カメラに戻す
            if (_boss == null)
            {
                _boss = GameObject.FindGameObjectWithTag("BossSprite");
                _bossState = _boss.GetComponent<BossStateManagement>().CurrentState;
            }

            if (_bossState != BossStateManagement.BossState.Death)
            {
                // ボスが死亡している場合はカメラを移動しない
                return;
            }

            // ボスのステートを確認（死亡ステートなら追従を解除して通常カメラに戻す）
            var bossStateMgmt = _boss.GetComponent<BossStateManagement>() ?? _boss.GetComponentInParent<BossStateManagement>();
            if (bossStateMgmt != null && bossStateMgmt.CurrentState == BossStateManagement.BossState.Death)
            {
                _isBossWave = false;
                _boss = null;
                return;
            }

            // ボスとプレイヤーの中心点を探す
            Vector3 targetPosition = (_player.transform.position + _boss.transform.position) / 2f;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

            // ボスとプレイヤーとの距離に応じてカメラの描画の大きさを変える
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
            if (_midiumBoss == null)
            {
                // 中ボスがいないならフラグを下ろして通常カメラに戻す
                _isMidiumBoss = false;
                return;
            }
            //ボスとプレイヤーの中心点を探す
            Vector3 targetPosition = (_player.transform.position + _midiumBoss.transform.position) / 2f;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _bossFollowSpeed * Time.fixedDeltaTime);

            //ボスとプレイヤーとの距離に応じてカメラの描画の大きさを変える
            float distance = Vector3.Distance(_player.transform.position, _midiumBoss.transform.position);
            float targetSize = Mathf.Clamp(distance * _zoomFactor, _minSize, _maxSize);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, targetSize, Time.deltaTime * _zoomSpeed);
        }
        else
        {
            Vector3 targetPosition = _burrel.transform.position;
            targetPosition.z = transform.position.z;

            transform.position = Vector3.Lerp(transform.position, targetPosition, _followSpeed * Time.fixedDeltaTime);
            if (_camera != null)
            {
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _minSize, Time.deltaTime * _zoomSpeed);
            }
        }
    }

    public void MovieMove(Vector2 pos)
    {
        Vector3 targetPosition = pos;
        targetPosition.z = transform.position.z;

        transform.position = targetPosition;
    }
}

