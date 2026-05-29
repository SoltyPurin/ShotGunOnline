using UnityEngine;

/// <summary>
/// ボスの影の位置保持のメソッド
/// </summary>
public class BossShadowPos : MonoBehaviour
{
    [SerializeField] private GameObject _boss;
    void Update()
    {
        transform.position = new Vector2(_boss.transform.position.x,transform.position.y);
    }
}
