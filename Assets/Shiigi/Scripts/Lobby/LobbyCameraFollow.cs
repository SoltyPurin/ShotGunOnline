using UnityEngine;

/// <summary>
/// ロビー用の簡易カメラ追従。
/// 本編の FollowingCameraToBurrel は銃身やボスへの依存があるため、ロビーではこちらを使用します。
/// </summary>
public class LobbyCameraFollow : MonoBehaviour
{
    [Header("追従の滑らかさ（大きいほど速く追従）")]
    [SerializeField] private float _followSpeed = 8f;

    private Transform _target;

    /// <summary>
    /// 追従対象を設定する
    /// </summary>
    /// <param name="target">追従対象</param>
    /// <param name="snap">true の場合、補間せず即座に移動する</param>
    public void SetTarget(Transform target, bool snap = false)
    {
        _target = target;
        if (snap && _target != null)
        {
            transform.position = GetTargetPosition();
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        transform.position = Vector3.Lerp(transform.position, GetTargetPosition(), _followSpeed * Time.deltaTime);
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 pos = _target.position;
        pos.z = transform.position.z;
        return pos;
    }
}
