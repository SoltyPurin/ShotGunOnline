using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ウェーブ内に登場するギミックに継承させるスクリプト
/// </summary>
public class WaveGimmick : NetworkBehaviour
{
    [SerializeField, Header("アニメータ")]
    private WaveGimmickSpawn _waveGimmickSpawn = default;

    // ★ ネットワーク上に生成された際、ホスト・クライアントの両方で自動発火する
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        GimmickPopAnm();
    }
    /// <summary>
    /// ギミックの出現アニメーションの再生メソッド。
    /// 再生させたい場合、animator内でPopという名前のTriggerを作り、開始条件をそれに設定しておこう。
    /// </summary>
    public void GimmickPopAnm()
    {
        if( _waveGimmickSpawn != null)
        {
            _waveGimmickSpawn.StartSpawn();
        }
    }

    /// <summary>
    /// ギミックの消滅アニメーションの再生メソッド。
    /// 再生させたい場合、animator内でDestroyという名前のTriggerを作り、開始条件をそれに設定しておこう。
    /// </summary>
    public void GimmickDestroyAnm()
    {
        
    }
}
