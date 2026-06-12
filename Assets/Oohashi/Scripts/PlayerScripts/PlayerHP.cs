using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerHP : MonoBehaviour
{
    private int _hpValue = 5;

    private GameObject _playerObject = default;

    private PlayerMove _playerMove = default;

    private InputPlayerShot _inputShot = default;

    private readonly string TITLE = "Title";
    private void Start()
    {
        if (!_playerObject)
        {
            //NetworkManager.Singleton.OnServerStarted += 
        }
    }

    private void GetPlayerProtocol()
    {
        // ネットワーク上の全プレイヤーオブジェクトを探す
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            // マルチプレイの場合、自分自身のキャラクター（LocalPlayer）のみをカメラの追従対象にする
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _playerObject = GameObject.FindWithTag("Player");
                _playerMove = _playerObject.GetComponent<PlayerMove>();
                _inputShot = _playerObject.GetComponent<InputPlayerShot>();
                _hpValue = SaveHardOptionSetting._heartValue;

                break;
            }
        }


    }
    public void TakeDamage()
    {
        if (_playerObject == null) 
        {
            GetPlayerProtocol();
            return;
        }
        if (SceneManager.GetActiveScene().name == "HPHonpen")
        {
            Debug.Log("被弾");
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
