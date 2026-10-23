using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MultiPlayResultImageChanger : MonoBehaviour
{
    [SerializeField, Header("一人の時のスプライト")]
    private Sprite _onePlayerSprite = default;
    [SerializeField,Header("二人の時のスプライト")]
    private Sprite _twoPlayerSprite = default;
    [SerializeField,Header("三人の時のスプライト")]
    private Sprite _threePlayerSprite = default;
    [SerializeField,Header("四人の時のスプライト")]
    private Sprite _fourPlayerSprite = default;

    [SerializeField, Header("差し替えるイメージ")]
    private Image _changeingImage = default;
    public void ChangeResutlImage(int playerCount)
    {
        switch(playerCount)
        {
            case 1:
                _changeingImage.sprite = _onePlayerSprite;
                break;

            case 2:
                _changeingImage.sprite = _twoPlayerSprite;
                break;

            case 3:
                _changeingImage.sprite = _threePlayerSprite;
                break;

            case 4:
                _changeingImage.sprite = _fourPlayerSprite;
                break;
        }
    }
}
