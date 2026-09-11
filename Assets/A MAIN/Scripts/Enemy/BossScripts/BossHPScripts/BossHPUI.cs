using Unity.Netcode;
using UnityEngine;

public class BossHPUI : EnemyDamageUI
{
    [SerializeField, Header("ウェーブ取得用のスクリプト")]
    private WaveManager _waveManager = default;

    [SerializeField, Header("ボスのHP表示用のキャンバス")]
    private GameObject _canvas = default;

    private GameObject _bossObject = default;
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
            if (_bossObject != null)
            {
                _isSearchBossWave = true;
                _canvas.SetActive(true);
                _bossHP = _bossObject.GetComponent<BossHP>();

                // 初期化とイベント登録
                Initialize();
            }
        }
    }

    private void Initialize()
    {
        _hpSlider.maxValue = _bossHP.EnemyHP.Value;
        _hpSlider.value = _bossHP.EnemyHP.Value;

        // 値が変更された時だけ自動で発火するイベントを登録
        _bossHP.EnemyHP.OnValueChanged += OnHPChanged;
    }

    // HPが変化した時だけ実行される軽量な処理
    private void OnHPChanged(int previousValue, int newValue)
    {
        UpdateHP(newValue);
    }

    private void OnDestroy()
    {
        // オブジェクト破棄時にイベント解除（メモリリーク防止）
        if (_bossHP != null && _bossHP.EnemyHP != null)
        {
            _bossHP.EnemyHP.OnValueChanged -= OnHPChanged;
        }
    }
}
