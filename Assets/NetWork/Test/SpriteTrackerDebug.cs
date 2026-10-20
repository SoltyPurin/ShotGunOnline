using UnityEngine;

public class SpriteTrackerDebug : MonoBehaviour
{
    private GameObject _bossObj;
    private bool _wasEnabled = true;

    private void Awake()
    {
        _bossObj = GetComponent<GameObject>();
    }

    private void Update()
    {
        if (_bossObj == null) return;

        // SpriteRenderer が ON から OFF に変わった瞬間を検知
        if (_wasEnabled &&!_bossObj.activeInHierarchy)
        {
            Debug.LogError($"[ObjTracker] {gameObject.name} の GameObject が OFF になりました！", this);
            Debug.Break(); // Unity エディターを一時停止（Pause）する
        }

        _wasEnabled = _bossObj.activeInHierarchy;
    }
}
