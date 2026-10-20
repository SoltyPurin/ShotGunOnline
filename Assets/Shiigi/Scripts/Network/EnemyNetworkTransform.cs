using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// 雑魚敵用のサーバー権限NetworkTransform。
/// ローカルシミュレーション有効時、クライアントではサーバーから届いた(補間済みの)位置を
/// transformへ反映せず ServerPosition として保持だけする。
/// 実際の表示位置はEnemyMove側のクライアント予測移動が決定する。
/// </summary>
[DisallowMultipleComponent]
public class EnemyNetworkTransform : NetworkTransform
{
    /// <summary>
    /// クライアントでローカルシミュレーションを行うか
    /// </summary>
    public bool LocalSimulation { get; set; } = false;

    /// <summary>
    /// サーバーから届いた補間済みの位置(クライアントのローカルシミュレーション時のみ有効)
    /// </summary>
    public Vector2 ServerPosition { get; private set; }

    private bool _hasInitialPosition = false;

    protected override bool OnIsServerAuthoritative()
    {
        return true;
    }

    public override void OnNetworkDespawn()
    {
        _hasInitialPosition = false;
        base.OnNetworkDespawn();
    }

    public override void OnUpdate()
    {
        // サーバー(ホスト)、またはローカルシミュレーションを行わない場合は通常のNetworkTransformと同じ挙動
        if (!IsSpawned || CanCommitToTransform || !LocalSimulation)
        {
            base.OnUpdate();
            return;
        }

        Vector3 localPosition = transform.position;
        base.OnUpdate();
        ServerPosition = transform.position;

        if (!_hasInitialPosition)
        {
            // 初回はサーバーの位置をそのまま採用する
            _hasInitialPosition = true;
            return;
        }

        // サーバー位置の反映を取り消し、ローカルシミュレーションの位置を維持する
        transform.position = localPosition;
    }
}
