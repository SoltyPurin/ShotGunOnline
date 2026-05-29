using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleFadeOut : MonoBehaviour
{
    [SerializeField, Header("フェードアウト用のイメージ")]
    private Image _fadeOutImage = default;

    [SerializeField, Header("フェード完了にかかる時間")]
    private float _fadeDuration = 1;

    private Coroutine _fadeCoroutine = default;

    private bool _isPlayingOnce = false;

    private int _initAlphaValue = 0;

    private bool _canFadeStart = false;
    public bool CanFadeStart
    {
        set {  _canFadeStart = value; }
    }
    [SerializeField,Header("移行するシーンの名前")]
    private string _moveScene = "MovieScene";

    private void Update()
    {
        if (!_canFadeStart)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _isPlayingOnce = false;
            _initAlphaValue = 0;
            _fadeOutImage.color = new Color(0,0,0,_initAlphaValue);   
            return;
        }

        if (!_isPlayingOnce)
        {
            _fadeCoroutine = StartCoroutine(FadeOut());
            _isPlayingOnce = true;
        }

    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0.0f;                 // 経過時間を初期化
        Color startColor = _fadeOutImage.color;       // フェードパネルの開始色を取得
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1.0f); // フェードパネルの最終色を設定

        // フェードアウトアニメーションを実行
        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;                        // 経過時間を増やす
            float t = Mathf.Clamp01(elapsedTime / _fadeDuration);  // フェードの進行度を計算
            _fadeOutImage.color = Color.Lerp(startColor, endColor, t); // パネルの色を変更してフェードアウト
            yield return null;                                     // 1フレーム待機
        }

        _fadeOutImage.color = endColor;  // フェードが完了したら最終色に設定

        _fadeOutImage.color = new Color(0, 0, 0, 1);
        _fadeOutImage.raycastTarget = true;
        SceneManager.LoadScene(_moveScene);
    }
}
