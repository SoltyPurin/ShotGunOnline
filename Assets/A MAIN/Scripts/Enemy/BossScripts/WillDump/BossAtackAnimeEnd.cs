using UnityEngine;

public class BossAtackAnimeEnd : BossColliderTriggerSW
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("衝撃波のオブジェクト")]
    protected GameObject _shockWave;
    [SerializeField, Header("ジャンプ攻撃の当たり判定")]
    private GameObject _jumpAtackArea;
    [SerializeField, Header("カメラ")]
    private GameObject _camera;
    [SerializeField, Header("ジャンプ攻撃で出る火の玉")]
    private Sprite _bulletSprite;

    private BossStateManagement _stateManagement = default;

    //---int,floatなどの数値---------------------------------
    private float[] _shockLangeList = { 0, 45, 90, 135, 180, -45, -90, -135 };
    private Vector3 _bulletSize = new Vector3(3, 3, 1);
    protected Vector2 _jumpAtackWaveDirection = Vector2.zero;


    #endregion

    private void Start()
    {
        _stateManagement = GetComponent<BossStateManagement>();
    }

    //Animationの終了後にState遷移
    public void JumpAttackEnd()
    {

        _jumpAtackArea.SetActive(false);
        _jumpAtackWaveDirection = (_stateManagement.Player.transform.position - transform.position).normalized;

        for (int i = 0; i < _shockLangeList.Length; i++)
        {
            Vector2 rotatedDir = Quaternion.AngleAxis(_shockLangeList[i], Vector3.forward) * _jumpAtackWaveDirection;
            CreateBullet(rotatedDir);
        }
        _bossCollider.isTrigger = false;
        _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
    }
    public void SpecialMoveEnd()
    {
        _bossCollider.isTrigger = false;
        _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
    }
    public void StunEnd()
    {
        _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
    }

    private void CreateBullet(Vector2 rotatedDir)
    {
        EnemyShotBullet shotBullet = Instantiate(_shockWave,
            transform.position,
            Quaternion.identity).GetComponent<EnemyShotBullet>();

        shotBullet.transform.localScale = _bulletSize;

        SpriteRenderer shotRenderer = shotBullet.GetComponent<SpriteRenderer>();
        shotRenderer.sprite = _bulletSprite;

        shotBullet.DirectionSetting(this.gameObject,rotatedDir);
    }
}
