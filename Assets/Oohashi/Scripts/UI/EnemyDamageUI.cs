using UnityEngine;
using UnityEngine.UI;

public class EnemyDamageUI : MonoBehaviour
{
    [SerializeField]protected Slider _hpSlider;


    /// <summary>
    /// 最初にスライダーの最大値とHPを設定する
    /// </summary>
    /// <param name="maxHP">最初のHPを最大値とする</param>
    public void Initialize( float maxHP)
    {
        _hpSlider.maxValue = maxHP;
        _hpSlider.value = maxHP;
    }

    /// <summary>
    /// 体力ゲージを更新するメソッド
    /// </summary>
    /// <param name="currentHP">現在のHP</param>
    public void UpdateHP(float currentHP)
    {
        _hpSlider.value = currentHP;
    }

}
