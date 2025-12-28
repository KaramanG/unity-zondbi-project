using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePlayerUseCase
{
    private readonly IPlayerRepository _playerRepository;

    public SavePlayerUseCase(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public void Execute(HealthSystem health, ManaSystem mana, Transform playerTransform)
    {
        PlayerData dataToSave = new PlayerData(
            health != null ? health.GetHealth() : 0f,
            mana != null ? mana.GetMana() : 0f,
            playerTransform != null ? playerTransform.position : Vector3.zero
        );

        _playerRepository.Save(dataToSave);
    }
}