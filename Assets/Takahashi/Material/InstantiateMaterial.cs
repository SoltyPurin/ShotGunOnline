using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// イメージのマテリアルを複製するスクリプト
/// </summary>
public class InstantiateMaterial : MonoBehaviour
{
    [SerializeField, Header("マテリアルが付いているコンポーネント")]
    private Image _image = default;

    private void Awake()
    {
        _image.material = Instantiate(_image.material);
    }
}
