using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///ウェーブ中に出てくるUIのアニメーションや文言を管理するクラス
/// </summary>
public class WaveUIManager : MonoBehaviour
{
    [SerializeField, Header("表示用テキスト")]
    private Text[] _waveTexts = new Text[3];

    [SerializeField, Header("テキストのアニメータ")]
    private Animator[] _animators = new Animator[2];
    [SerializeField, Header("フェードのアニメータ")]
    private Animator _fadeAnimator = default;

    [SerializeField, Header("敵の数を表示するテキスト")]
    private Text _enemyCountText = default;

    private int _firstEnemyCount = 0;

    public void PopText(string text, int count)
    {
        if (_waveTexts[count] != null)
        {
            _waveTexts[count].text = text;
            _animators[count].SetTrigger("Pop");
        }
    }

    public void ReMoveRank()
    {
        _waveTexts[2].text = " ";
    }

    public void TextChange(string text, int count)
    {
        if (_waveTexts[count] != null)
        {
            _waveTexts[count].text = text;
        }
    }

    public void HiddenText(int count)
    {
        if (_animators[count] != null)
        {
            _animators[count].SetTrigger("Hidden");
        }
    }

    public void FadeOutImage()
    {
        if(_fadeAnimator != null)
        {
            _fadeAnimator.SetTrigger("FadeOut");
        }
    }

    public void FadeInImage()
    {
        if (_fadeAnimator != null)
        {
            _fadeAnimator.SetTrigger("FadeIn");
        }
    }

    public void SetFirstEnemyCounter(int count)
    {
        if(_enemyCountText != null)
        {
            _firstEnemyCount = count;
            _enemyCountText.text = "ノコリ : " +count.ToString();
        }
    }

    public void SetEnemyCounter(int count)
    {
        if(_enemyCountText != null)
        {
            _enemyCountText.text = "ノコリ : " + count.ToString();
        }
    }

    public void BossMoveName()
    {

    }
}
