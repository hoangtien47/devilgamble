using UnityEngine;

namespace Map
{
    public enum NodeType
    {
        MinorEnemy,
        EliteEnemy,
        RestSite,
        Treasure,
        Store,
        Boss,
        Mystery
    }
    public enum PassiveType
    {
        None,
        IncreaseAttack,
        HealSelf,
        Poison,
    }
}

namespace Map
{
    [CreateAssetMenu]
    public class NodeBlueprint : ScriptableObject
    {
        public Sprite sprite;
        public NodeType nodeType;
        public EnemyCardScriptable enemyCharacter;
        [Header("Enemy/Boss Stats")]
        [Tooltip("HP for Enemy or Boss nodes")]
        public int hp;
        [Tooltip("ATK for Enemy or Boss nodes")]
        public int atk;
        [Tooltip("Turn to act for Enemy or Boss nodes")]
        public int turn;
        [Tooltip("Passive for Enemy or Boss nodes")]
        public PassiveType passiveType;
    }
}