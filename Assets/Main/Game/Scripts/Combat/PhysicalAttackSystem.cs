using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalAttackSystem : MonoBehaviour
{
    [SerializeField] private float damage;

    private HitboxScript hitboxScript;

    private void Awake()
    {
        hitboxScript = GetComponentInChildren<HitboxScript>();
    }
    public float GetDamage()
    {
        return damage;
    }

    public void OnPhysicalAttackEnter()
    {
        hitboxScript.EnableHitbox();
    }
    public void OnPhysicalAttackExit()
    {
        hitboxScript.DisableHitbox();
    }
}
