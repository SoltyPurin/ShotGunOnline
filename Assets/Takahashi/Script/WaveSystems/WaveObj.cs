using UnityEngine;

/// <summary>
/// ウェーブ内に登場する敵に継承させるスクリプト
/// </summary>
public class WaveObj : MonoBehaviour
{
    [SerializeField, Header("取得")]
    private EnemySpawnAnim _enemySpawnAnim;
    /// <summary>
    /// オブジェクトの生存判定用メソッド
    /// </summary>
    /// <returns>生きてるならtrue,死んでるならfalse</returns>
    public bool IsEnd()
    {       
        if (gameObject)
        {
            return this == null || gameObject == null;
        }

        return true;
    }

    public void PopAnim()
    {
        if(_enemySpawnAnim != null)
        {
            _enemySpawnAnim.StartAnim();
        }
    }
}
