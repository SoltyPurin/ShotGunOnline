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
        _player = GameObject.FindWithTag(PLAYERTAGNAME);
        _coinKeep = _player.GetComponent<SoulKeep>();
        _playerMove = _player.GetComponent<PlayerMove>();
        _playerMove.enabled = false;

        _mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
        _canvasAnime = _mainCanvas.GetComponent<Animator>();

        _camera = GameObject.FindWithTag("MainCamera");
        _shake = _camera.GetComponent<BossJumpAtackShake>();

        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();
        _cameraMove.IsEnd = true;

    }

    public void PopEndReset()
    {
        _bossHP._isInvincible = false;
        _stateManagement.enabled = true;
        _cameraMove.IsMovie = false;
        _playerMove.enabled = true;
    }

    public void EnemyDelete()
    {
        _cameraMove.IsBossWave = false;
        _coinKeep.AdditionCoin();

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
