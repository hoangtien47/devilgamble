using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroData", menuName = "Game/Hero Data")]
public class HeroesData : ScriptableObject
{
    public string id;
    public string heroName;
    public int maxHealth;
    public int attackPower;
    public Sprite picSprite;

    //[Header("Skills")]
    //public List<SkillData> skills;
}