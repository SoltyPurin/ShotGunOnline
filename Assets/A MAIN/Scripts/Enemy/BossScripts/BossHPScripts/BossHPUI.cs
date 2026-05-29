using UnityEngine;

public class BossHPUI : EnemyDamageUI
{
    [SerializeField, Header("ウェーブ取得用のスクリプト")]
    private WaveManager _waveManager = default;

    [SerializeField, Header("ボスのHP表示用のキャンバス")]
    private GameObject _canvas = default;

    [SerializeField, Header("ボス")] 
    private GameObject _bossObject = default;

    [SerializeField, Header("ボスのHP管理")] 
    private BossHP _bossHP = default;

    private bool _isSearchBossWave = false;

    private void Start()
    {
        _canvas.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!_isSearchBossWave && _waveManager.IsBossWave)
        {
            _bossObject = GameObject.FindWithTag("Boss");
            if(_bossObject != null)
            {
                _isSearchBossWave = true;
                _canvas.SetActive(true);
                _bossHP = _bossObject.GetComponent<BossHP>();
                Initialize();
            }
        }
        else if(_bossObject != null)
        {
            _hpSlider.value = _bossHP.BossHPVariable;
        }
    }


    private void Initialize()
    {
        _hpSlider.maxValue = _bossHP.BossHPVariable;
    }
}
