using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicalAttackSystem : MonoBehaviour
{
    [SerializeField] private float damage;

    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed;
    [SerializeField] private Vector3 fireballSpawnOffset;
    [SerializeField] private float fireballLifetime;

    [SerializeField] private List<LayerMask> targetLayers;

    private void SpawnFireball()
    {
        if (fireballPrefab == null) return;

        GameObject fireball = Instantiate(fireballPrefab, transform.position + fireballSpawnOffset, transform.rotation * Quaternion.Euler(-90f, 0f, 0f));

        Fireball fireballScript = fireball.GetComponent<Fireball>();
        fireballScript.SetDamage(damage);
        fireballScript.SetMasks(targetLayers);
        fireballScript.SetLifetime(fireballLifetime);
        fireballScript.StartCountdown();

        Rigidbody fireballRb = fireball.GetComponent<Rigidbody>();
        fireballRb.AddForce(transform.forward * fireballSpeed, ForceMode.Impulse);
    }

    public void OnMagicalAttackEnter()
    {
        SpawnFireball();
    }
}
