using UnityEngine;

public enum SkillType
{
    Attack,
    Buff,
    Heal
}
public class Skill : MonoBehaviour
{
    public SkillType skillType;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Call this to activate the skill on a target
    public void ActivateSkill(ICharacter user, ICharacter target)
    {
        switch (skillType)
        {
            case SkillType.Attack:
                // Implement attack skill logic here
                break;
            case SkillType.Buff:
                // Implement buff skill logic here
                break;
            case SkillType.Heal:
                // Implement heal skill logic here
                break;
            default:
                Debug.LogError("Unknown skill type!");
                break;
        }
    }
    
}
