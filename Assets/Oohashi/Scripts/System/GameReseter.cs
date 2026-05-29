using UnityEngine;
using UnityEngine.SceneManagement;

public class GameReseter : MonoBehaviour
{
    private static GameReseter _gameReset = default;
    private void Awake()
    {
        if(_gameReset != null && _gameReset != this)
        {
            Destroy(this);
            return;
        }

        _gameReset = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Reset();
        }
    }

    public void Reset()
    {
        SceneManager.LoadScene("Title");
    }
}
