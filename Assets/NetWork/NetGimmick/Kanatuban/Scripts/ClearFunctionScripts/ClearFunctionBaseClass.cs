using Unity.Netcode;
using UnityEngine;

public class ClearFunctionBaseClass : NetworkBehaviour
{
    [Header("このオブジェクトは絶対にPlateParentオブジェクトの子オブジェクトにしてください")]
    [Header("絶対にこのオブジェクトにNetworkObjectクラスをアタッチしないでください")]
    [Header("もしNetworkObjectをアタッチしてしまったら絶対に削除してください")]
    [Space(10)]
    [SerializeField] private bool _dummy;



    [SerializeField, Header("クリアしたら消えるオブジェクト？")]
    private bool _isClearedVanishObj = false;

    [SerializeField, Header("(感圧板と同期させて動かしたい場合)同期する感圧板")]
    private DetectOnPlayer _pressurePlate = default;

    private SpriteRenderer _sprite = default;
    private BoxCollider2D _collider = default;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //感圧板と同期させるオブジェクトの場合は公開してあるboolを購読する
        if (_pressurePlate != null)
        {
            _pressurePlate.OnPlayerOnPlate += SyncPressurePlateFunction;
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (_pressurePlate != null)
        {
            _pressurePlate.OnPlayerOnPlate -= SyncPressurePlateFunction;
        }

    }

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider2D>();

        if (!_isClearedVanishObj)
        {
            _sprite.enabled = false;
            _collider.enabled = false;
        }
    }
    /// <summary>
    /// 感圧板が全部押された時に作動するメソッド
    /// </summary>
    public virtual void Function()
    {
        // サーバーから全クライアント端末へ実行を呼びかける
        if (IsServer)
        {
            FunctionClientRpc();
        }
    }

    /// <summary>
    /// 全端末（ホスト・クライアント）で一斉に消去/出現処理を実行する ClientRpc
    /// </summary>
    [ClientRpc]
    private void FunctionClientRpc()
    {
        ExecuteFunction();
    }

    private void ExecuteFunction()
    {
        // クリアした時に消えるオブジェクトだったらスプライトとコライダーを無効化
        if (_isClearedVanishObj)
        {
            _sprite.enabled = false;
            _collider.enabled = false;
        }
        // そうでなければ出現
        else
        {
            _sprite.enabled = true;
            _collider.enabled = true;
        }
    }
    /// <summary>
    /// 特定の感圧板が押された時等に作動するメソッド
    /// </summary>
    /// <param name="isPress"></param>
    public virtual void SyncPressurePlateFunction(bool isPress)
    {
        //押された時
        if (isPress)
        {
            if (_isClearedVanishObj)
            {
                _sprite.enabled = false;
                _collider.enabled = false;
            }
            else
            {
                _sprite.enabled = true;
                _collider.enabled = true;
            }
        }
        //離したとき
        else
        {
            if (_isClearedVanishObj)
            {
                _sprite.enabled = true;
                _collider.enabled = true;
            }
            else
            {
                _sprite.enabled = false;
                _collider.enabled = false;
            }

        }
    }
}
