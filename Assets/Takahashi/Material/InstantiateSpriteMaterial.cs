using UnityEngine;

/// <summary>
/// スプライトのマテリアルを複製するためだけのスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class InstantiateSpriteMaterial : MonoBehaviour
{
    [SerializeField, Header("スプライトレンダラー")]
    private SpriteRenderer _spriteRenderer = default;

    void Start()
    {
        //スプライトレンダラーに割り当てられたマテリアルを複製、そして割り当て
        _spriteRenderer.material = Instantiate(_spriteRenderer.material);
    }
}
