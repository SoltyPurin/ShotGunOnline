using DG.Tweening;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DetectOnPlayer : NetworkBehaviour
{
    public event Action<bool> OnPlayerOnPlate;

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

    private NetworkVariable<bool> _isCleared = new NetworkVariable<bool>(false);


    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isStepped.OnValueChanged += OnSteppedChanged;
        _isCleared.OnValueChanged += OnClearedChanged;

        RefreshSprite();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isStepped.OnValueChanged -= OnSteppedChanged;
        _isCleared.OnValueChanged -= OnClearedChanged;

    }
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCleared.Value)
        {
            return;
        }
        Debug.Log("なにかが触れたよ");
        RequestChangeState(true);
    }

    public virtual void OnTriggerExit2D(Collider2D collision)
    {

        if (_isCleared.Value)
        {
            return;
        }
        RequestChangeState(false);
    }
    private void RequestChangeState(bool state)
    {
        Debug.Log("関数呼び出し");
        if (!IsSpawned || _isCleared.Value)
        {
            return;
        }
        Debug.Log("ステート変更が呼び出された");
        if (IsServer)
        {
            SetStateOnServer(state);
        }
        else
        {
            SetStateServerRpc(state);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetStateServerRpc(bool state)
    {
        SetStateOnServer(state);
    }

    private void SetStateOnServer(bool state)
    {

        if (!IsServer || _isCleared.Value)
        {
            return;
        }

        if (_isStepped.Value == state)
        {
            return;
        }

        _isStepped.Value = state;
        Debug.Log("現在のステートは" +  _isStepped.Value);
    }
    private void OnSteppedChanged(bool previous, bool current)
    {
        RefreshSprite();
        OnPlayerOnPlate?.Invoke(current);
    }

    private void OnClearedChanged(bool previous, bool current)
    {
        RefreshSprite();
    }
    private void RefreshSprite()
    {
        if (_spriteRenderer == null)
        {
            return;
        }
        Debug.Log("見た目変更");
        // クリア済みなら離れても踏まれた表示を維持
        if (_isCleared.Value || _isStepped.Value)
        {
            _spriteRenderer.sprite = _steppedOnSprite;
        }
        else
        {
            _spriteRenderer.sprite = _notSteppedOnSprite;
        }
    }

    public virtual void ClearJudgementConfirmed()
    {
        if (!IsServer || _isCleared.Value)
        {
            return;
        }
        _isCleared.Value = true;
        RefreshSprite();
    }

}
