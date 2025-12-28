using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerRepository
{
    void Save(PlayerData data);

    PlayerData Load();

    bool HasSave();

    void DeleteSave();
}