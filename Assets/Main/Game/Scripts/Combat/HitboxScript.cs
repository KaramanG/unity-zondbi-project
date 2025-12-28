using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxScript : MonoBehaviour
{
    [SerializeField] private PhysicalAttackSystem sourceAttack;
    [SerializeField] private List<LayerMask> targetLayers;

    private Collider hitboxCollider;
    private List<Collider> hitList;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        hitList = new List<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitboxCollider.enabled) { return; }

        bool layerIsTarget = false;
        foreach (LayerMask mask in targetLayers)
        {
            if ((mask.value & (1 << other.gameObject.layer)) != 0)
            {
                layerIsTarget = true;
                break;
            }
        }
        if (!layerIsTarget) { return; }

        if (hitList.Contains(other)) { return; }

        HealthSystem targetHealth = other.GetComponent<HealthSystem>();
        if (targetHealth == null) return;

        float damageAmount = 0f;
        if (sourceAttack != null)
        {
            damageAmount = sourceAttack.GetDamage();
        }
        else
        {
            Debug.LogWarning("SourceAttack не назначен в HitboxScript на " + gameObject.name);
            return;
        }

        targetHealth.TakeDamage(damageAmount);

        BossAI bossAI = other.GetComponent<BossAI>();
        if (bossAI != null)
        {
            bossAI.NotifyDamageTaken(damageAmount);
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Mobs"))
        {
            MobAI mobAI = other.GetComponent<MobAI>();
            if (mobAI != null)
            {
                mobAI.TakeStun();
            }
        }

        hitList.Add(other);
    }

    public void EnableHitbox()
    {
        hitList.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }
}