using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelSprites : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite = default;
    private void Delete()
    {
        _sprite.enabled = false;
    }
}
