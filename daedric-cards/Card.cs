using DaggerfallWorkshop;
using System;
using UnityEngine;

[Serializable]
public class Card
{
    public int id;

    public string name;
    public string description;

    public int attack;
    public int health;
    public int cost;
    public SoundClips introSFX;
    public SoundClips deathSFX;

    public Sprite artwork;

    public Card(string[] row)
    {
        id = int.Parse(row[0]);
        name = row[1];
        description = row[2];
        attack = int.Parse(row[3]);
        health = int.Parse(row[4]);
        cost = int.Parse(row[5]);
        introSFX = (SoundClips)int.Parse(row[6]);
        deathSFX = (SoundClips)int.Parse(row[7]);

        artwork = DaedricCardsMod.mod.GetAsset<Sprite>(name);
    }
}
