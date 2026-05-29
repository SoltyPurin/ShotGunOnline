using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleWaitTimer : MonoBehaviour
{
    [SerializeField, Header("何秒待機したらムービーに切り替えるか")]
    private float _waitLimitTime = 30;

    [SerializeField, Header("フェードアウトのスクリプト")]
    private TitleFadeOut _fadeOut = default;

    private float _initWaitTime = 0;


    private void Update()
    {
        if (!IsInputDetected())
        {
            _initWaitTime += Time.deltaTime;
        }
        else
        {
            _fadeOut.CanFadeStart = false;
            _initWaitTime = 0;
        }

        if (_initWaitTime >= _waitLimitTime)
        {
            _fadeOut.CanFadeStart = true;
        }
    }

    bool IsInputDetected()
    {
        // キー入力
        if (Input.anyKey) return true;

        // スティック入力
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f) return true;

        return false;
    }
}
