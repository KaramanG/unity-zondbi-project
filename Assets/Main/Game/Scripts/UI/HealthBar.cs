using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private Image healthBarFill;

    private Camera mainCamera;

    private void Awake()
    {
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;

        mainCamera = Camera.main;
    }

    private void Update()
    {
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
        transform.rotation = targetRotation;
    }

    public void UpdateHealthBar()
    {
        healthBarFill.fillAmount = healthSystem.GetHealth() / healthSystem.GetMaxHealth();
    }
}