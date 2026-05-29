using UnityEngine;
using DG.Tweening;

public class PlayerTrenble : MonoBehaviour
{
    [SerializeField, Header("揺れる時間")]
    private float _durationTime = 0.2f;
    [SerializeField, Header("揺れの強さ")]
    private float _trenblePower = 2;
    [SerializeField, Header("揺れの細かさ")]
    private int _vibrato = 50;
    [SerializeField, Header("揺れる方向のバラツキ")]
    private float _randomness = 90;
    //[SerializeField, Header("揺らすオブジェクト")]
    //private GameObject _player = default;
    public void DamageTrenble()
    {
        transform.DOShakePosition(duration: _durationTime,        
        strength: _trenblePower,   
        vibrato: _vibrato,                
        randomness: _randomness,           
        snapping: false,
        fadeOut: true);
    }

    public void RushTrenble()
    {
        transform.DOShakePosition(duration: 0.5f,
            strength: 3,
            vibrato: 90,
            randomness: 90,
            snapping: false,
            fadeOut: true);
    }
}
