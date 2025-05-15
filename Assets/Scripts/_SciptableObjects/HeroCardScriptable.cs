using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroCard", menuName = "Hero Card")]
public class HeroCardScriptable : ScriptableObject
{
    // Hero card parameters
    public string heroName;
    public string heroDescription;
    public Sprite heroSprite;

    // Hero stats
    public int health;
    public int attack;
    public int defense;

    //Hero normal attack
    public string attackName;
    public string attackDescription;

    // Hero skills
    public string skillName;
    public string skill1Description;
    public AnimationClip skill1Animation;

    public string ultName;
    public string ultDescription;
    public AnimationClip ultAnimation;

    // Additional parameters can be added as needed
}
