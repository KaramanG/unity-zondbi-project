using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    public float health;
    public float mana;
    public float positionX;
    public float positionY;
    public float positionZ;

    public PlayerData(float currentHealth, float currentMana, Vector3 position)
    {
        this.health = currentHealth;
        this.mana = currentMana;
        this.positionX = position.x;
        this.positionY = position.y;
        this.positionZ = position.z;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }

    public PlayerData() { }
}