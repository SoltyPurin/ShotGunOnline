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
    /// バイブするメソッド
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    public void ViblationPortocol(float chargeTime)
    {
        if (_gamepad == null) return;
        //1以内の値に込めるため2で割る
        _vibeValue = chargeTime / 2;
        //1以内の値でモータバイブ
        _gamepad.SetMotorSpeeds(_vibeValue, _vibeValue);
        //モータストップのコーチン呼び出し
        StartCoroutine(ViblationStop());
    }
    /// <summary>
    /// バイブ止めるコーチン
    /// </summary>
    /// <returns>0.3秒待ってから停止</returns>
    private IEnumerator ViblationStop()
    {
        if (_gamepad == null) yield break;
        yield return new WaitForSeconds(0.3f);
        _gamepad.SetMotorSpeeds(0, 0);
    }
    /// <summary>
    /// ウルトのバイブのコーチン、各待機時間経て実行
    /// </summary>
    /// <returns>撃ちわけてバイブ</returns>
    public IEnumerator UltVibeProtocol()
    {
        if (_gamepad == null) yield break;
        yield return null; // 1フレ待つ
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
    /// コントローラーのバイブの左右決める
    /// </summary>
    /// <param name="collisionEnemyPos">ぶつかってる敵のオブジェクト</param>
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
    /// 与えられたのモーター回してバイブ
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    private IEnumerator DamageVibeProtocol(float left, float right)
    {
        if (_gamepad == null) yield break;
        _gamepad.SetMotorSpeeds(left, right);
        yield return new WaitForSeconds(0.5f);
        _gamepad.SetMotorSpeeds(0, 0);
    }

}
