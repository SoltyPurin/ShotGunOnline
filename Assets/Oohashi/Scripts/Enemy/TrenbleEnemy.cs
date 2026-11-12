using UnityEngine;
using DG.Tweening;

public class TrenbleEnemy : MonoBehaviour
{
    private float _trenblePower = 3;

    public void TrenbleProtocol(float durationTime)
    {
        transform.DOShakePosition(duration: durationTime,              // 揺れる時間（ここは固定でOK）
        strength: _trenblePower,    // 揺れの強さ
        vibrato: 50,                // 揺れの細かさ
        randomness: 90,             // 揺れる方向のばらつき
        snapping: false,
        fadeOut: true).SetLink(gameObject);
    }

    public void NormalTrenbleProtocol(float durationTime,float strength)
    {
        transform.DOShakePosition(duration: durationTime,              // 揺れる時間（ここは固定でOK）
        strength: strength,    // 揺れの強さ
        vibrato: 90,                // 揺れの細かさ
        randomness: 90,             // 揺れる方向のばらつき
        snapping: false,
        fadeOut: true).SetLink(gameObject);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
