using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    private NavMesh2DAgent _agent; //NavMeshAgent2Dを使用するための変数
    private Transform _target; //追跡するターゲット

    void Start()
    {
        _agent = GetComponent<NavMesh2DAgent>(); //agentにNavMeshAgent2Dを取得
        _target = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if(_target == null)
        {
            return;
        }
        _agent.destination = _target.position; //agentの目的地をtargetの座標にする
        //agent.SetDestination(target.position); //こっちの書き方でもオッケー
    }
}
