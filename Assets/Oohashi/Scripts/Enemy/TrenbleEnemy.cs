using UnityEngine;
using DG.Tweening;

public class TrenbleEnemy : MonoBehaviour
{
    private float _trenblePower = 3;

    public void TrenbleProtocol(float durationTime)
    {
        transform.DOShakePosition(duration: durationTime,              // —h‚ê‚éŠÔi‚±‚±‚ÍŒÅ’è‚ÅOKj
        strength: _trenblePower,    // —h‚ê‚Ì‹­‚³
        vibrato: 50,                // —h‚ê‚Ì×‚©‚³
        randomness: 90,             // —h‚ê‚é•ûŒü‚Ì‚Î‚ç‚Â‚«
        snapping: false,
        fadeOut: true);
    }

    public void NormalTrenbleProtocol(float durationTime,float strength)
    {
        transform.DOShakePosition(duration: durationTime,              // —h‚ê‚éŠÔi‚±‚±‚ÍŒÅ’è‚ÅOKj
        strength: strength,    // —h‚ê‚Ì‹­‚³
        vibrato: 90,                // —h‚ê‚Ì×‚©‚³
        randomness: 90,             // —h‚ê‚é•ûŒü‚Ì‚Î‚ç‚Â‚«
        snapping: false,
        fadeOut: true);
    }
}
