using Unity.Netcode;
using UnityEngine;

public class PlayerCoinDrop : NetworkBehaviour
{
    [SerializeField]
    private GameObject Coin;

    /// <summary>
    /// ソウルをドロップ生成する
    /// </summary>
    /// <param name="coinDrops">ドロップ数</param>
    /// <param name="targetPlayer">ソウルが向かうターゲットプレイヤー（未指定の場合は最寄りプレイヤー）</param>
    public void CoinDrop(int coinDrops, GameObject targetPlayer = null)
    {
        if (!IsServer)
        {
            return;
        }
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (coinDrops < 1 || coinDrops > 10)
        {
            Debug.Log("[PlayerCoinDrop] 生成数が1~10の範囲を超えたので制限しました。");
        }
        coinDrops = Mathf.Clamp(coinDrops, 1, 10);

        // 指定回数Coinをネットワーク上に生成
        for (int i = 0; i < coinDrops; ++i)
        {
            if (Coin == null) break;

            GameObject coin = Instantiate(Coin, transform.position, Quaternion.identity);

            // NetworkObject としてスポーン（全クライアントへ同期）
            var netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            var coinCtrl = coin.GetComponent<CoinControl>();
            if (coinCtrl != null)
            {
                coinCtrl.SetTargetPlayer(targetPlayer);
            }
        }
    }
}
