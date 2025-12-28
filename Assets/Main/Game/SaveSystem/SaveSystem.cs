using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the player's GameObject here, or it will try to find GameObject with tag 'Player'")]
    [SerializeField] private GameObject player;

    private static PlayerData dataToLoad = null;
    private static bool isLoadingGame = false;

    private IPlayerRepository _playerRepository;
    private SavePlayerUseCase _savePlayerUseCase;
    private LoadPlayerUseCase _loadPlayerUseCase;

    void Awake()
    {
        _playerRepository = new BinaryPlayerRepository();
        _savePlayerUseCase = new SavePlayerUseCase(_playerRepository);
        _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player GameObject (with tag 'Player') not found! Save/Load functionality might not work correctly.", this);
            }
        }
    }

    void Start()
    {
        if (isLoadingGame && dataToLoad != null)
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            if (player != null)
            {
                ApplyLoadedData();
                Debug.Log("Loaded data applied to player.");
            }
            else
            {
                Debug.LogError("Player not found in the new scene after loading. Could not apply data.");
            }

            dataToLoad = null;
            isLoadingGame = false;
        }
        else if (isLoadingGame && dataToLoad == null)
        {
            Debug.LogWarning("SaveSystem: isLoadingGame was true, but dataToLoad is null. Load process might have failed or no save existed.");
            isLoadingGame = false;
        }
    }

    public void SaveGame()
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot save game: player GameObject is null.");
            return;
        }

        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        ManaSystem manaSystem = player.GetComponent<ManaSystem>();
        Transform playerTransform = player.transform;

        _savePlayerUseCase.Execute(healthSystem, manaSystem, playerTransform);

        Debug.Log("SaveGame called. Data save requested.");
    }

    public void LoadGame()
    {
        if (!_playerRepository.HasSave())
        {
            Debug.Log("No save data found to load.");
            return;
        }

        PlayerData loadedData = _loadPlayerUseCase.Execute();

        if (loadedData != null)
        {
            dataToLoad = loadedData;
            isLoadingGame = true;

            Debug.Log("LoadGame called. Data loaded into static fields. Reloading scene...");

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.LogError("LoadGame failed: Could not load data from repository.");
            dataToLoad = null;
            isLoadingGame = false;
        }
    }

    public void DeleteSave()
    {
        _playerRepository.DeleteSave();
        Debug.Log("Save file deletion requested.");
    }

    private void ApplyLoadedData()
    {
        if (player == null || dataToLoad == null)
        {
            Debug.LogError("ApplyLoadedData called but player or dataToLoad is null.");
            return;
        }

        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.SetHealth(dataToLoad.health);
        }
        else { Debug.LogWarning("HealthSystem component not found on player when applying loaded data."); }

        ManaSystem manaSystem = player.GetComponent<ManaSystem>();
        if (manaSystem != null)
        {
            manaSystem.SetMana(dataToLoad.mana);
        }
        else { Debug.LogWarning("ManaSystem component not found on player when applying loaded data."); }

        player.transform.position = dataToLoad.GetPosition();
    }

    public static bool IsLoading()
    {
        return isLoadingGame;
    }

    public bool HasSaveData()
    {
        return _playerRepository.HasSave();
    }
}