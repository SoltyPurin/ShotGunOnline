using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
//このクラスでやること
//子オブジェクトとして登録した感圧板から全てtrueが帰ってくるかを監視する
//全てのtrueが帰ってきたらクリアオブジェクトを作動させる
public class KanatubanManager : NetworkBehaviour
{

    private List<ClearFunctionBaseClass> _clearFunctionObject = new List<ClearFunctionBaseClass>();

    private List<DetectOnPlayer> _pressurePlateList = new List<DetectOnPlayer>();

    private void Awake()
    {
        _pressurePlateList.AddRange(GetComponentsInChildren<DetectOnPlayer>());
        _clearFunctionObject.AddRange(GetComponentsInChildren<ClearFunctionBaseClass>());
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        foreach (DetectOnPlayer doPlayer in _pressurePlateList)
        {
            if (doPlayer != null)
            {
                doPlayer.OnPlayerOnPlate += CheckAllChildState;
            }
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        foreach (DetectOnPlayer doPlayer in _pressurePlateList)
        {
            if (doPlayer != null)
            {
                doPlayer.OnPlayerOnPlate -= CheckAllChildState;
            }
        }

    }
    private void CheckAllChildState(bool newState)
    {
        if (!IsServer)
        {
            return;
        }
        bool isAllTrue = _pressurePlateList.TrueForAll(plate => plate.IsStepped);
        if (isAllTrue)
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                foreach (DetectOnPlayer plate in _pressurePlateList)
                {
                    if (plate != null)
                    {
                        plate.ClearJudgementConfirmed();
                    }
                }
                foreach(ClearFunctionBaseClass clFunc in _clearFunctionObject)
                {
                    if(clFunc != null)
                    {
                        clFunc.Function();
                    }
                }
            }
        }
    }

    //親オブジェクトの中に入ってる感圧板が全部trueを返したら突破ということになる
    //親オブジェクトは子オブジェクトが全部trueになってるかを確認するクラスをアタッチする?
    //こっちで監視するのは親オブジェクトがtrueを返すか否かを見る？
    //よくよく考えたら全体的な監視者いらなくね？
    //親オブジェクトの直下に感圧板とクリアした時に動かすオブジェクトの二つを登録してそれでやればよくない？
   }
