using UnityEngine;

/// <summary>
/// 自動開始しないウェーブのスタート用スクリプト / 
/// ウェーブプレハブ内にコリジョンのTriggerをONにしたオブジェクトを配置し、それにアタッチする。
/// </summary>
public class WaveEvent : MonoBehaviour
{
    [SerializeField, Header("スクリプト取得系")]
    private Wave _wave = default;
    [SerializeField]
    private WaveManager _waveManager = default;

    private void Start()
    {
        //ウェーブマネージャーを取得
        _waveManager = GameObject.FindWithTag("WaveManager").GetComponent<WaveManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }

        //範囲内にプレイヤーが入ってきた場合、ウェーブを開始する。
        if (collision.CompareTag("Player"))
        {
            Event();
        }
    }

    /// <summary>
    /// ウェーブ開始用メソッド / override可能
    /// </summary>
    public virtual void Event()
    {
        _wave.StartWave();
        _waveManager.StartWaveProduction(_waveManager.WaveIndex);
        Destroy(this.gameObject);
    }
}
