using UnityEngine;

public class KaisinYure : MonoBehaviour
{
    private GameObject _camera = default;
    private BossJumpAtackShake _cameraShake = default;

    private void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _cameraShake = _camera.GetComponent<BossJumpAtackShake>();
    }

    public void DelKaisin()
    {
        gameObject.SetActive(false);
    }

    public void KaisinShake()
    {
        _cameraShake.Shake();
    }
}
