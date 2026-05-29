using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHP : MonoBehaviour
{
    private int _hpValue = 5;

    private GameObject _playerObject = default;

    private PlayerMove _playerMove = default;

    private InputPlayerShot _inputShot = default;

    private readonly string TITLE = "Title";    

    private void Start()
    {
        _playerObject = GameObject.FindWithTag("Player");
        _playerMove = _playerObject.GetComponent<PlayerMove>();
        _inputShot = _playerObject.GetComponent<InputPlayerShot>();
        _hpValue = SaveHardOptionSetting._heartValue;
    }
    public void TakeDamage()
    {
        if(SceneManager.GetActiveScene().name == "HPHonpen")
        {
            Debug.Log("”í’e");
            _hpValue--;
            if (_hpValue <= 0)
            {
                _inputShot.DeathMethod();
                _playerMove.DeathMethod();
                StartCoroutine(DeathMethod());
            }
        }
    }

    private IEnumerator DeathMethod()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(TITLE);
    }
}
