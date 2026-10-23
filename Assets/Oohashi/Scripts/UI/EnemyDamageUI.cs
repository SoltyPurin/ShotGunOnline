using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDamageUI : MonoBehaviour
{
    [SerializeField] protected Slider _hpSlider;

    /// <summary>
    /// スライダーの最大値と現在値をHPと一致させる
    /// </summary>
    /// <param name="maxHP">設定するHPの最大値</param>
    public void Initialize(float maxHP)
    {
        if (_hpSlider == null) return;
        _hpSlider.maxValue = maxHP;
        _hpSlider.value = maxHP;
    }

    /// <summary>
    /// 残HPゲージを更新するメソッド
    /// </summary>
    /// <param name="currentHP">現在のHP</param>
    public void UpdateHP(float currentHP)
    {
        if (_hpSlider == null) return;
        // 最大値を超えている場合や最大値が未設定の場合は安全に拡張
        if (currentHP > _hpSlider.maxValue || _hpSlider.maxValue <= 0)
        {
            _hpSlider.maxValue = currentHP;
        }
        _hpSlider.value = currentHP;
    }

    /// <summary>
    /// 最大値と現在値を同時に更新するメソッド
    /// </summary>
    public void UpdateHP(float currentHP, float maxHP)
    {
        if (_hpSlider == null) return;
        if (maxHP > 0)
        {
            _hpSlider.maxValue = maxHP;
        }
        _hpSlider.value = currentHP;
    }
}