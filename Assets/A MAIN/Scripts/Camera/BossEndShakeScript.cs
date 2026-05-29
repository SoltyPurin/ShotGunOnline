using UnityEngine;

public class BossEndShakeScript : MonoBehaviour
{
    private bool _beShake = false;
    [SerializeField, Header("ÉJÉÅÉâÇóhÇÁÇ∑íl")]
    private float _cameraChakeValue = 1.5f;

    private void FixedUpdate()
    {
        if (_beShake)
        {
            CameraShake();
        }
    }
    private void CameraShake()
    {
        float horizontalShakeValue = Random.Range(-_cameraChakeValue, _cameraChakeValue);
        float verticalShakeValue = Random.Range(-_cameraChakeValue, _cameraChakeValue);
        Vector3 initPos = this.transform.position;
        initPos.x += horizontalShakeValue;
        initPos.y += verticalShakeValue;
        this.transform.position = initPos;
    }
}
