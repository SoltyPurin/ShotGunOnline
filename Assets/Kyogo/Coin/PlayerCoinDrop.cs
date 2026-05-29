using UnityEngine;

public class PlayerCoinDrop : MonoBehaviour
{
    [SerializeField]
    GameObject Coin;
    //CoinDrips Coinが落ちる数
    public void CoinDrop(int CoinDrops)
    {
        if (!this.gameObject.activeInHierarchy)
        {
            return;
        }
        //10を超えた場合エラーを返す
        if ( CoinDrops < 1 || CoinDrops > 10)
        {
            Debug.Log("生成数が1~10の範囲を超えたので制限しました。");
        }
        //1~10の範囲で制限をかけている
        CoinDrops = Mathf.Clamp(CoinDrops, 1, 10);

        //指定回数Coinを生成
        for (int i = 0; i < CoinDrops; ++i)
        {
            GameObject coin = Instantiate(Coin);
            coin.transform.position = this.transform.position;
        }
    }
}
