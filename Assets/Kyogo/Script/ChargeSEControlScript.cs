using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChargeSEControlScript : MonoBehaviour
{
    [SerializeField, Header("オブジェクト")]
    private GameObject playerObj;
    [SerializeField, Header("音源取得 0番に上昇音、1-3番、小中大に発射音")]//0番に上昇音、1-3番に発射音を入れています。1に小さいもの、2に中ぐらいのもの、3に大きいものを入れています。(6/14 清水)
    private AudioClip[] audioClips;
    [SerializeField, Header("audioSource")]
    private AudioSource audioSource;

    [SerializeField, Header("発射音区分下限値")]//SmallはMedium以下の全てなのでなし
    float Shotgun_Shot_Large;
    [SerializeField]
    float Shotgun_Shot_Medium;

    //Script
    InputPlayerShot _inputPlayerShot;
    private bool charge_wait = false; // 前フレームのチャージ状態を記録
    private readonly string FIREBUTTONNAME = "Fire";


    void Start()
    {
        _inputPlayerShot = playerObj.GetComponent<InputPlayerShot>();
    }

    void Update()
    {
        float rightTriggerValue = Gamepad.current.rightTrigger.ReadValue();//ゲームパッドのRTの押し込み具合を見ている
        bool isrightTriggerInput = rightTriggerValue >= 0.9f;//RTが強く（90%以上)押されているのかを判定する
        bool canShooting = (isrightTriggerInput || Input.GetButton(FIREBUTTONNAME)) ;//True=Charge中

        // // チャージ開始時
        // if (!charge_wait && canShooting && !_inputPlayerShot.IsDecChargeTime)
        // {
        //         audioSource.clip = audioClips[0];  // チャージ音
        //         audioSource.loop = false;
        //         audioSource.Play();
        // }
        if (_inputPlayerShot.ChargeValue <= 0f && canShooting && !_inputPlayerShot.IsDecCharge)
        {
            audioSource.clip = audioClips[0];
            audioSource.loop = false;
            audioSource.Play();
        }

        
        if (charge_wait && !canShooting && !_inputPlayerShot.IsDecCharge)
        {
            audioSource.Stop();
            //Debug.Log("発射！");
            float currentCharge = _inputPlayerShot.ChargeValue;
            //Debug.Log("現在のチャージ量: " + currentCharge / 2);
            if (currentCharge / 2 > Shotgun_Shot_Large)
            {
                audioSource.PlayOneShot(audioClips[3]); // 大きい発射音
            }
            else if (currentCharge / 2 > Shotgun_Shot_Medium)
            {
                audioSource.PlayOneShot(audioClips[2]); // 中くらいの発射音
            }
            else
            {
                audioSource.PlayOneShot(audioClips[1]);
            }


        }
        charge_wait = canShooting;
    }
}