using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpriteReset : MonoBehaviour
{
    private Vector3 _originPos;
    [SerializeField]
    private SpriteRenderer _spriteRenderer;
    private Sprite _originSprite;
    private void Start()
    {
        _originPos = transform.position;
        _originSprite = _spriteRenderer.sprite;
    }
    public void SpriteReset()
    {
        transform.position = _originPos;
        _spriteRenderer.sprite = _originSprite;
    }
}
