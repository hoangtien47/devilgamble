using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "EnemyManager", menuName = "Managers/Enemy Manager")]
public class EnemyMapManager : ScriptableObject
{
    [Header("Enemy Lists")]
    [SerializeField] private List<EnemyCardScriptable> minorEnemies = new List<EnemyCardScriptable>();
    [SerializeField] private List<EnemyCardScriptable> eliteEnemies = new List<EnemyCardScriptable>();
    [SerializeField] private List<EnemyCardScriptable> bossEnemies = new List<EnemyCardScriptable>();

    // Cached random for better randomization
    private System.Random random;

    private void OnEnable()
    {
        random = new System.Random(System.DateTime.Now.Millisecond);
    }

    /// <summary>
    /// Returns a random minor enemy from the minor enemies list
    /// </summary>
    public EnemyCardScriptable GetRandomMinorEnemy()
    {
        if (minorEnemies == null || minorEnemies.Count == 0)
        {
            Debug.LogWarning("No minor enemies in the list!");
            return null;
        }

        int index = random.Next(minorEnemies.Count);
        return minorEnemies[index];
    }

    /// <summary>
    /// Returns a random elite enemy from the elite enemies list
    /// </summary>
    public EnemyCardScriptable GetRandomEliteEnemy()
    {
        if (eliteEnemies == null || eliteEnemies.Count == 0)
        {
            Debug.LogWarning("No elite enemies in the list!");
            return null;
        }

        int index = random.Next(eliteEnemies.Count);
        return eliteEnemies[index];
    }

    /// <summary>
    /// Returns a random boss from the boss enemies list
    /// </summary>
    public EnemyCardScriptable GetRandomBoss()
    {
        if (bossEnemies == null || bossEnemies.Count == 0)
        {
            Debug.LogWarning("No boss enemies in the list!");
            return null;
        }

        int index = random.Next(bossEnemies.Count);
        return bossEnemies[index];
    }
    /// <summary>
    /// Validates that all enemy lists contain valid entries
    /// </summary>
    public bool ValidateEnemyLists()
    {
        bool isValid = true;

        if (minorEnemies.Count == 0)
        {
            Debug.LogError("Minor enemies list is empty!");
            isValid = false;
        }

        if (eliteEnemies.Count == 0)
        {
            Debug.LogError("Elite enemies list is empty!");
            isValid = false;
        }

        if (bossEnemies.Count == 0)
        {
            Debug.LogError("Boss enemies list is empty!");
            isValid = false;
        }

        // Check for null entries
        if (minorEnemies.Any(x => x == null) ||
            eliteEnemies.Any(x => x == null) ||
            bossEnemies.Any(x => x == null))
        {
            Debug.LogError("Found null entries in enemy lists!");
            isValid = false;
        }

        return isValid;
    }

#if UNITY_EDITOR
    // Editor-only validation
    private void OnValidate()
    {
        ValidateEnemyLists();
    }
#endif
}