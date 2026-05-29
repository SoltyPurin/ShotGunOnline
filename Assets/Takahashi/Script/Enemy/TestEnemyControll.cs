using UnityEngine;

/// <summary>
/// テストエネミー用クラス
/// Kキーで殺せます。
/// </summary>
public class TestEnemyControll : WaveObj
{
    [SerializeField, Header("死ぬまでのカウントダウン")]
    private int _debugCount = 0;

    private int _count = 0;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && gameObject.activeInHierarchy)
        {
            _count++;
            if(_count >= _debugCount)
            {
                Destroy(gameObject);
            }
        }
    }
}
