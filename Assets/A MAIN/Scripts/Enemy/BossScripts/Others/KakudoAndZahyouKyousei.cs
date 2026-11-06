using UnityEngine;

public class KakudoAndZahyouKyousei : MonoBehaviour
{
    private BossStateManagement _state = default;
    private Transform _rootBossTransform = default;

    private void Awake()
    {
        if (transform.parent != null && transform.parent.parent != null)
        {
            _rootBossTransform = transform.parent.parent;
            _state = _rootBossTransform.GetComponent<BossStateManagement>();
        }
        else
        {
            _rootBossTransform = transform.root;
            if (_rootBossTransform != null)
            {
                _state = _rootBossTransform.GetComponent<BossStateManagement>();
            }
        }
    }

    public void RotateReset()
    {
        this.transform.localRotation = Quaternion.identity;
        if (_rootBossTransform != null)
        {
            _rootBossTransform.rotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        this.transform.localPosition = Vector3.zero;

        // ボス本体（ルート）の傾きを補正（2DのためX/Y/Z回転は常に0に保つ）
        if (_rootBossTransform != null && _state != null && _state.CurrentState != BossStateManagement.BossState.Drop)
        {
            if (_rootBossTransform.rotation != Quaternion.identity)
            {
                _rootBossTransform.rotation = Quaternion.identity;
            }
        }

        // スプライト自身のX/Z回転の傾きを防止（Y軸の左右反転は保持）
        Vector3 curEuler = transform.localEulerAngles;
        if (curEuler.x != 0f || curEuler.z != 0f)
        {
            transform.localEulerAngles = new Vector3(0f, curEuler.y, 0f);
        }
    }
}
