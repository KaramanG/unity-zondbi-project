using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class BinaryPlayerRepository : IPlayerRepository
{
    private readonly string _saveFileName = "playerData.sav";
    private string _saveFilePath;

    public BinaryPlayerRepository()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
    }

    public void Save(PlayerData data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;

        try
        {
            stream = new FileStream(_saveFilePath, FileMode.Create);
            formatter.Serialize(stream, data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data using BinaryFormatter: {e.Message}");
        }
        finally
        {
            if (stream != null) { stream.Close(); }
        }
    }

    public PlayerData Load()
    {
        if (!File.Exists(_saveFilePath))
        {
            return null;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;
        PlayerData loadedData = null;

        try
        {
            stream = new FileStream(_saveFilePath, FileMode.Open);
            loadedData = formatter.Deserialize(stream) as PlayerData;

            if (loadedData == null)
            {
                Debug.LogError($"Failed to deserialize player data from {_saveFilePath}. File might be corrupted.");
                DeleteSave();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player data using BinaryFormatter: {e.Message}");
            DeleteSave();
            loadedData = null;
        }
        finally
        {
            if (stream != null) { stream.Close(); }
        }

        return loadedData;
    }

    public bool HasSave()
    {
        return File.Exists(_saveFilePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                File.Delete(_saveFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete player Binary save file: {e.Message}");
            }
        }
    }
}