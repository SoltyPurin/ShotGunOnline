using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class EnedMovieScript : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("ボス")]
    private GameObject _boss = default;
    private GameObject _player = default;
    private GameObject _camera = default;
    private GameObject _mainCanvas = default;
    private GameObject _mainGage = default;

    private GameObject[] _enemyObjects = default;
    private GameObject[] _bombObjects = default;

    [SerializeField, Header("ボスのState管理")]
    private BossStateManagement _stateManagement = default;
    [SerializeField, Header("ボスのHP管理")]
    private BossHP _bossHP = default;
    private BossJumpAtackShake _shake = default;
    private FollowingCameraToBurrel _cameraMove = default;
    private SoulKeep _coinKeep = default;
    private PlayerMove _playerMove = default;

    private Animator _canvasAnime = default;

    //---string------------------------------------------------
    private readonly string PLAYERTAGNAME = "Player";

    #endregion

    private void Start()
    {

        //_player = GameObject.FindWithTag(PLAYERTAGNAME);
        //_coinKeep = _player.GetComponent<SoulKeep>();
        //_playerMove = _player.GetComponent<PlayerMove>();
        //_playerMove.enabled = false;
        bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isOnline)
        {
            // --- 【オンライン時】自分のローカルプレイヤーを確実に取得 ---
            GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYERTAGNAME);
            foreach (GameObject p in players)
            {
                NetworkObject netObj = p.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsLocalPlayer)
                {
                    _player = p;
                    break;
                }
            }
        }
        else
        {
            // --- 【オフライン時】従来の方式で取得 ---
            _player = GameObject.FindWithTag(PLAYERTAGNAME);
            _coinKeep = _player.GetComponent<SoulKeep>();
            _playerMove = _player.GetComponent<PlayerMove>();
            _playerMove.enabled = false;
        }


        _mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
        _canvasAnime = _mainCanvas.GetComponent<Animator>();

        _camera = GameObject.FindWithTag("MainCamera");
        _shake = _camera.GetComponent<BossJumpAtackShake>();

        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();
        _cameraMove.IsEnd = true;
        if (_stateManagement != null)
        {
            _stateManagement.enabled = false;
        }

        // 【追加】同期と物理を一時オフにする
        DisableBossNetworkAndPhysics();
        _cameraMove.IsMovie = true;
        _cameraMove.IsBossWave = false;
    }

    public void PopEndReset()
    {
        _bossHP._isInvincible = false;
        _stateManagement.enabled = true;
        _cameraMove.IsMovie = false;
        _playerMove.enabled = true;
        _cameraMove.IsBossWave = true;
        EnableBossNetworkAndPhysics();
    }

    private void DisableBossNetworkAndPhysics()
    {
        if (_boss != null)
        {
            // NetworkTransformをオフにして、Timelineの移動を邪魔させない
            var netTrans = _boss.GetComponent<NetworkTransform>();
            if (netTrans != null) netTrans.enabled = false;

            // 物理演算を一時的にKinematic（キネマティック）にして、勝手に落下・移動するのを防ぐ
            var rb = _boss.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // 【追加】ムービー終了時の同期・物理の復旧処理
    private void EnableBossNetworkAndPhysics()
    {
        if (_boss != null)
        {
            // 同期を再開する
            var netTrans = _boss.GetComponent<NetworkTransform>();
            if (netTrans != null) netTrans.enabled = true;

            // 物理演算をDynamicに戻して、通常の戦闘ができるようにする
            var rb = _boss.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    public void EnemyDelete()
    {
        _cameraMove.IsBossWave = false;
        if (_coinKeep)
        {
            _coinKeep.AdditionCoin();
        }

        _enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in _enemyObjects)
        {
            GameObject.Destroy(enemy);
        }

        _bombObjects = GameObject.FindGameObjectsWithTag("Bomb");
        foreach (GameObject enemy in _bombObjects)
        {
            GameObject.Destroy(enemy);
        }
    }
    public void MovieEnd()
    {
        Destroy(_boss);
    }

    public void BeShake()
    {
        _shake.ShakeStart();
    }

    public void FinShake()
    {
        _shake.ShakeEnd();
    }

    public void CanvasSetfalse()
    {
        _canvasAnime.SetTrigger("Hide");
        //_mainCanvas.SetActive(false);
    }

    public void CanvasSettrue()
    {
        _canvasAnime.SetTrigger("Show");
        //_mainCanvas.SetActive(true);
    }

    public void CanvasSetFalseAndGage()
    {

        _canvasAnime.SetTrigger("Hide");
        _mainGage = GameObject.FindGameObjectWithTag("MainGage");
        _mainGage.SetActive(false);

        //_mainCanvas.SetActive(false);
    }
}
