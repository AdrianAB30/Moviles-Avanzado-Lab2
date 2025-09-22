using UnityEngine;

public class PlayerData
{
    public string accountID;
    public Vector3 position;
    public int health;
    public int baseAttack;

    public PlayerData(string ID, Vector3 pos, int hp, int atk)
    {
        accountID = ID;
        position = pos;
        health = hp;
        baseAttack = atk;
    }
}
