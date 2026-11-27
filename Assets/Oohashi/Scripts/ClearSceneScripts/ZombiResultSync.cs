using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZombiResultSync : NetworkBehaviour
{
    private struct ZombieScore
    {
        public int normal;
        public int middle;
        public int boss;
        public int totalEnemy;
        public int damage;
        public int totalDamage;
    }

    private Dictionary<ulong, ZombieScore> _scores = new();

    private int _expectedPlayerCount;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _expectedPlayerCount =
                NetworkManager.Singleton.ConnectedClientsIds.Count;

            // ホスト自身のスコアを登録
            RegisterScore(
                NetworkManager.ServerClientId,
                ScoreData._scoreData
            );
        }
        else
        {
            // クライアント自身のスコアをホストへ送信
            ScoreData data = ScoreData._scoreData;

            if (data == null) return;

            SubmitZombieScoreServerRpc(
                data.GetEnemyKillCount.normalEnemyCount,
                data.GetEnemyKillCount.middleEnemyCount,
                data.GetEnemyKillCount.bossEnemyCount,
                data.TotalEnemyCount,
                data.DamageTakenCount,
                data.TotalTakenDamage
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitZombieScoreServerRpc(
        int normal,
        int middle,
        int boss,
        int totalEnemy,
        int damage,
        int totalDamage,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        ZombieScore score = new ZombieScore
        {
            normal = normal,
            middle = middle,
            boss = boss,
            totalEnemy = totalEnemy,
            damage = damage,
            totalDamage = totalDamage
        };

        _scores[clientId] = score;

        CheckAllScoresReceived();
    }

    private void RegisterScore(ulong clientId, ScoreData data)
    {
        if (data == null) return;

        _scores[clientId] = new ZombieScore
        {
            normal = data.GetEnemyKillCount.normalEnemyCount,
            middle = data.GetEnemyKillCount.middleEnemyCount,
            boss = data.GetEnemyKillCount.bossEnemyCount,
            totalEnemy = data.TotalEnemyCount,
            damage = data.DamageTakenCount,
            totalDamage = data.TotalTakenDamage
        };

        CheckAllScoresReceived();
    }

    private void CheckAllScoresReceived()
    {
        if (!IsServer) return;

        if (_scores.Count < _expectedPlayerCount) return;

        int normal = 0;
        int middle = 0;
        int boss = 0;
        int totalEnemy = 0;
        int damage = 0;
        int totalDamage = 0;

        foreach (ZombieScore score in _scores.Values)
        {
            normal += score.normal;
            middle += score.middle;
            boss += score.boss;
            totalEnemy += score.totalEnemy;
            damage += score.damage;
            totalDamage += score.totalDamage;
        }

        SyncZombieScoreClientRpc(
            normal,
            middle,
            boss,
            totalEnemy,
            damage,
            totalDamage
        );
    }

    [ClientRpc]
    private void SyncZombieScoreClientRpc(
        int normal,
        int middle,
        int boss,
        int totalEnemy,
        int damage,
        int totalDamage)
    {
        ScoreData data = ScoreData._scoreData;

        if (data == null) return;

        data.SetZombieScoreFromHost(
            normal,
            middle,
            boss,
            totalEnemy
        );

        Debug.Log($"全プレイヤー合計討伐数: {normal + middle + boss}");
    }
}
