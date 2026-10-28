using Unity.Netcode;
using UnityEngine;

public class DetectOnPlayer : NetworkBehaviour
{
    private SpriteRenderer _spriteRenderer = default;

    [SerializeField, Header("踏まれてない時のスプライト")]
    private Sprite _notSteppedOnSprite = default;
    [SerializeField, Header("踏まれてる時のスプライト")]
    private Sprite _steppedOnSprite = default;

    private NetworkVariable<bool> _isStepped = new NetworkVariable<bool>(false);
    public bool IsStepped
    {
        get { return _isStepped.Value; }
    }

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = _notSteppedOnSprite;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _spriteRenderer.sprite = _steppedOnSprite;
        _isStepped.Value = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _isStepped.Value = false;
        _spriteRenderer.sprite = _notSteppedOnSprite;
    }

    private void Update()
    {
        Debug.Log("今感圧版は踏まれてるか" + _isStepped.Value);
    }
}
