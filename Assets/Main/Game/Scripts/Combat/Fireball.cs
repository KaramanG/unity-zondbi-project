using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    private float damage;
    private List<LayerMask> targetLayers;
    private float lifeTime;

    void OnTriggerEnter(Collider other)
    {
        if (targetLayers == null) return;

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

        HealthSystem targetHealth = other.GetComponent<HealthSystem>();
        targetHealth.TakeDamage(damage);
        Animator targetAnimator = other.GetComponent<Animator>();
        targetAnimator.SetTrigger("Stun");
        Destroy(gameObject);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    public void SetMasks(List<LayerMask> newMasks)
    {
        targetLayers = newMasks;
    }
    public void SetLifetime(float newLifetime)
    {
        lifeTime = newLifetime;
    }
    public void StartCountdown()
    {
        Destroy(gameObject, lifeTime);
    }
}
