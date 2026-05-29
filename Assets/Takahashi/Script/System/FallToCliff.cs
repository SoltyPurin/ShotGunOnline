using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FallToCliff : MonoBehaviour
{
    private WaveTransition _waveTransition = default;

    private void Start()
    {
        _waveTransition = GameObject.FindWithTag("WaveManager").GetComponent<WaveTransition>();
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.scene.isLoaded)
        {
            return;
        }

        if (collision == null) return;

        if (_waveTransition.IsWaveChangeMoment)
        {
            return;
        }

        EntityFall entityFall = collision.GetComponent<EntityFall>();
        if (entityFall == null) return;


        //6î‘ÇÕEnemyÇÃÉåÉCÉÑÅ[î‘çÜ
        if (collision.gameObject.layer == 6)
        {
            StartCoroutine(DeathCheck(collision.gameObject,entityFall));
        }
        else if (collision.CompareTag("Player"))
        {
            PlayerDamageKnockBack damageKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            if (damageKnockBack != null && damageKnockBack.gameObject.activeInHierarchy)
            {
                entityFall.Fall();
                //StartCoroutine(damageKnockBack.StartCoroutine(DamagePerformance());
                //damageKnockBack.StartCoroutine("");
            }
        }
        else
        {
            
            StartCoroutine(StoneFall(collision.gameObject, entityFall));
        }

    }

    private IEnumerator StoneFall(GameObject collision,EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;
        StoneFallToCliff stoneFall = collision.GetComponent<StoneFallToCliff>();
        stoneFall.FallMethod();
        entityFall.Fall();

    }

    private IEnumerator DeathCheck(GameObject collision,EntityFall entityFall)
    {
        yield return new WaitForSeconds(0.1f);
        if (collision == null || !collision) yield break;
        EnemyTakeDamage enemyTakeDamage = collision.GetComponent<EnemyTakeDamage>();
        if(enemyTakeDamage == null)
        {
            yield break;
        }
        EnemyMove enemyMove = collision.GetComponent<EnemyMove>();
        if (enemyTakeDamage == null || enemyTakeDamage.IsDead) yield break;

        if (enemyMove.EnemyState == EnemyState.roadKill)
        {
            yield break;
        }

        BombProtocol bomb = collision.GetComponent<BombProtocol>();
        if (bomb != null && bomb.IsExplosion)
        {
            yield break;
        }
        entityFall.Fall();


    }
}
