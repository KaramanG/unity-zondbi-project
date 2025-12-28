using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float _health;

    private float health
    {
        get
        {
            return _health;
        }
        set
        {
            float clampedValue = Mathf.Clamp(value, 0f, maxHealth);

            if (Mathf.Approximately(_health, clampedValue)) return;

            _health = clampedValue;
            OnHealthChanged?.Invoke(_health, maxHealth);

            if (_health <= 0 && !isDead)
            {
                InstanceDie();
            }
        }
    }

    public UnityEvent OnPlayerDied;
    public UnityEvent<float, float> OnHealthChanged;

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private bool isPlayer;

    private bool isDead;
    private Animator animator;

    private void Awake()
    {
        if (!(SaveSystem.IsLoading() && isPlayer))
        {
            health = maxHealth;
        }

        isDead = false;

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar();
        }

        animator = GetComponent<Animator>();
    }

    public float GetHealth() { return health; }
    public float GetMaxHealth() { return maxHealth; }
    public bool IsDead() { return isDead; }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar();
        }
    }

    private void InstanceDie()
    {
        if (isDead) return;

        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        OnPlayerDied?.Invoke();
    }

    public void SetHealth(float newHealth)
    {
        health = newHealth;

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        health += amount;
    }
}