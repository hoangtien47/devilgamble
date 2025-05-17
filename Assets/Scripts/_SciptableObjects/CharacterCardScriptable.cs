using UnityEngine;

public class CharacterCardScriptable : ScriptableObject
{
    // Hero card parameters
    public string id;
    public string Name;
    public string Description;
    public Sprite Sprite;

    public int health;
    public int attack;
    public int defense;
    public int energyForUlt;

    public string passiveName;
    public string passiveDescription;

    public string ultName;
    public string ultDescription;
    public AnimationClip ultAnimation;

}
