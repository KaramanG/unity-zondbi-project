using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadPlayerUseCase
{
    private readonly IPlayerRepository _playerRepository;

    public LoadPlayerUseCase(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public PlayerData Execute()
    {
        PlayerData loadedData = _playerRepository.Load();

        return loadedData;
    }

    public bool CanExecute()
    {
        return _playerRepository.HasSave();
    }
}