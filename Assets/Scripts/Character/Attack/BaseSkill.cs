using MasterStylizedProjectile;
using UnityEngine;

public enum SkillType
{
    Attack,
    Buff,
    Heal
}
public abstract class BaseSkill : MonoBehaviour
{
    public SkillType skillType;
    [SerializeField] protected SkillData skillData;
    [SerializeField] public Transform StartNodeTrans;
    [SerializeField] public Transform EndNodeTrans;
    public EffectsGroup CurEffect => skillData.Effects;

    public SkillData SkillData => skillData;

    public virtual void UseSkill()
    {

    }
}
