using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerVibelation : MonoBehaviour
{
    //レバブルの値を保存するスクリプト
    private float _vibeValue = default;
    //パッドを保存する変数
    private Gamepad _gamepad = default;

    private void Start()
    {
        if(Gamepad.current == null)
        {
            //パッドが接続されてなかったらリターン
            return;
        }
        _gamepad = Gamepad.current;
        _gamepad.SetMotorSpeeds(0, 0);
    }

    /// <summary>
    /// レバブルするメソッド
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    public void ViblationPortocol(float chargeTime)
    {
        //1以内に値を収めるため2で割る
        _vibeValue = chargeTime / 2;
        //1以内に収めた値でレバブル
        _gamepad.SetMotorSpeeds(_vibeValue, _vibeValue);
        //モーター停止のコルーチン呼び出し
        StartCoroutine(ViblationStop());
    }
    /// <summary>
    /// レバブルを止めるコルーチン
    /// </summary>
    /// <returns>0.3秒待ってから停止</returns>
    private IEnumerator ViblationStop()
    {
        yield return new WaitForSeconds(0.3f);
        _gamepad.SetMotorSpeeds(0, 0);
    }
    /// <summary>
    /// ウルトのレバブルのコルーチン、各秒数待機して実行
    /// </summary>
    /// <returns>左右でわけてレバブル</returns>
    public IEnumerator UltVibeProtocol()
    {
        yield return null; // 1フレームだけ待ってから
        _gamepad.SetMotorSpeeds(1f,1f);
        yield return new WaitForSeconds(0.1f);
        _gamepad.SetMotorSpeeds(1, 0f);
        yield return new WaitForSeconds(0.5f);
        _gamepad.SetMotorSpeeds(0f, 1);
        yield return new WaitForSeconds(0.5f);
        _gamepad.SetMotorSpeeds(0.3f, 0.3f);
        yield return new WaitForSeconds(0.9f);
        _gamepad.SetMotorSpeeds(0, 0);
    }
    /// <summary>
    /// コントローラーのレバブルの左右を決める
    /// </summary>
    /// <param name="collisionEnemyPos">ぶつかってきた敵のオブジェクト</param>
    public void ViblartionSettingLeftAndRight(Vector2 collisionEnemyPos)
    {
        float enemyPosX = collisionEnemyPos.x;
        float playerPosX = transform.position.x;
        bool isEnemyPosRight = enemyPosX > playerPosX;

        if (isEnemyPosRight)
        {
            StartCoroutine(DamageVibeProtocol(0, 1));
        }
        else
        {
            StartCoroutine(DamageVibeProtocol(1, 0));
        }
    }
    /// <summary>
    /// 与えられた方のモーターを起動してレバブル
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    private IEnumerator DamageVibeProtocol(float left, float right)
    {
        _gamepad.SetMotorSpeeds(left, right);
        yield return new WaitForSeconds(0.5f);
        _gamepad.SetMotorSpeeds(0, 0);
    }

}
