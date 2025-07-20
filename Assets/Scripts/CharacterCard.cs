//using DG.Tweening;
//using System.Collections;
//using UnityEngine;


//public class CharacterCard : Card
//{
//    [Header("Character Reference")]
//    [SerializeField] private BaseCharacter _character;

//    // Property to safely access the character component
//    public BaseCharacter Character
//    {
//        get
//        {
//            if (_character == null)
//                _character = GetComponent<BaseCharacter>();
//            return _character;
//        }
//    }

//    protected override void Initialize()
//    {
//        base.Initialize();

//        if (_character == null)
//            _character = GetComponent<BaseCharacter>();

//        CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;

//        if (Character != null && cardVisual != null)
//        {
//            characterVisual.OnLoadCharacter(BaseCharacter);

//            if (characterVisual != null)
//                characterVisual.OnLoadCharacter(Character);
//        }

//    }

//    // Override Start to use the character card visual prefab
//    void Start()
//    {
//        Initialize();
//    }


//    public void OnCharacterDataChange()
//    {
//        if (cardVisual != null && Character != null)
//        {
//            CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;
//            if (characterVisual != null)
//                characterVisual.OnChangeData(Character.HP, Character.ATK);
//        }
//    }


//    public void OnCharacterDeath()
//    {
//        if (cardVisual != null)
//        {
//            CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;
//            if (characterVisual != null)
//            {
//                characterVisual.PlayExplosionEffect();
//            }
//            Destroy(cardVisual.gameObject);
//            Destroy(gameObject, 1f);
//        }
//    }

//    public Coroutine Attack(CharacterCard target)
//    {
//        // Start the coroutine and return it so the caller can yield on it
//        return StartCoroutine(AttackCoroutine(target));
//    }

//    public IEnumerator AttackCoroutine(CharacterCard target)
//    {
//        Tween attackTween = null;
//        CharacterCardVisual targetCardVisual = null;

//        // Play attack animation through the visual
//        if (cardVisual != null && target is Component targetComponent)
//        {
//            System.Action hitCallback = () =>
//            {
//                // Check if the target still exists
//                if (targetComponent.transform == null) return;

//                targetCardVisual = target.cardVisual as CharacterCardVisual;

//                // Use AttackedEffect instead of just shaking position
//                targetCardVisual.AttackedEffect(1.0f, () =>
//                {
//                    // Any additional effects after the attacked animation
//                });
//            };

//            CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;
//            if (characterVisual != null)
//            {
//                attackTween = characterVisual.Attack(targetComponent.transform, hitCallback);
//            }
//        }

//        // Wait for the attack to complete if the tween was created successfully
//        if (attackTween != null)
//        {
//            yield return attackTween.WaitForCompletion();

//            if (Character != null)
//                Character.Attack(target.GetComponent<ICharacter>());
//        }
//    }

//    public void LoadEnemyData(EnemyCardData enemyCardData)
//    {
//        EnemyCharacter enemyCharacter = Character as EnemyCharacter;
//        if (enemyCharacter != null)
//        {
//            enemyCharacter.SetData(enemyCardData);

//            // Update the visual
//            if (cardVisual != null)
//            {
//                CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;
//                if (characterVisual != null)
//                    characterVisual.OnLoadCharacter(enemyCharacter);
//            }
//        }
//    }

//    public void LoadHeroData(HeroCardData heroData)
//    {
//        HeroesCharacter heroCharacter = Character as HeroesCharacter;
//        if (heroCharacter != null)
//        {
//            heroCharacter.SetData(heroData);

//            // Update the visual
//            if (cardVisual != null)
//            {
//                CharacterCardVisual characterVisual = cardVisual as CharacterCardVisual;
//                if (characterVisual != null)
//                    characterVisual.OnLoadCharacter(heroCharacter);
//            }
//        }
//    }
//}