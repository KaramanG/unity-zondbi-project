using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] idleSounds;
    [SerializeField] private AudioClip[] agroSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] stunSounds;

    [Header("Sound Settings")]
    [SerializeField] private float minIdleSoundInterval = 5f;
    [SerializeField] private float maxIdleSoundInterval = 10f;

    private AudioSource audioSource;
    private MobAI mobAI;
    private HealthSystem healthSystem;

    private float nextIdleSoundTime;
    private bool hasPlayedAgroSoundSinceLastIdle = false;
    private bool hasPlayedDeathSound = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        mobAI = GetComponent<MobAI>();
        healthSystem = GetComponent<HealthSystem>();

        if (mobAI == null)
        {
            Debug.LogError("MobAI component not found on " + gameObject.name + "! ZombieAudio will be disabled.", this);
            enabled = false;
            return;
        }
        if (healthSystem == null)
        {
            Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Death sound might not play correctly.", this);
        }
    }

    void Start()
    {
        SetNextIdleSoundTime();
    }

    void Update()
    {
        if (!mobAI.enabled && healthSystem != null && healthSystem.IsDead() && !hasPlayedDeathSound)
        {
            PlayDeathSoundInternal();
            return;
        }

        if (mobAI == null) return;

        HandleIdleSounds();
    }

    private void HandleIdleSounds()
    {
        if (Time.time >= nextIdleSoundTime && (healthSystem == null || !healthSystem.IsDead()) && mobAI.navMeshAgent != null && mobAI.navMeshAgent.velocity.sqrMagnitude < 0.1f)
        {
            PlayRandomSound(idleSounds);
            SetNextIdleSoundTime();
            hasPlayedAgroSoundSinceLastIdle = false;
        }
    }

    private void SetNextIdleSoundTime()
    {
        nextIdleSoundTime = Time.time + Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

    private void PlayRandomSound(AudioClip[] clips, float volume = 1.0f)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    public void PlayAgroSound()
    {
        if (!hasPlayedAgroSoundSinceLastIdle)
        {
            PlayRandomSound(agroSounds);
            hasPlayedAgroSoundSinceLastIdle = true;
        }
    }

    public void PlayAttackSound()
    {
        if (healthSystem != null && healthSystem.IsDead()) return;
        PlayRandomSound(attackSounds);
    }

    public void PlayDeathSound()
    {
        PlayDeathSoundInternal();
    }

    private void PlayDeathSoundInternal()
    {
        if (!hasPlayedDeathSound)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            PlayRandomSound(deathSounds);
            hasPlayedDeathSound = true;
        }
    }

    public void PlayStunSound()
    {
        if (healthSystem != null && healthSystem.IsDead()) return;
        PlayRandomSound(stunSounds);
    }
}