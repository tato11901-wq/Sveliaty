using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Sveliaty/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    [Header("Configuración")]
    [Tooltip("Lista de todos los enemigos disponibles")]
    public List<EnemyData> allEnemies = new List<EnemyData>();

    [Header("Probabilidades de Tier")]
    [Range(0, 100)]
    [Tooltip("Probabilidad de Tier 1")]
    public float tier1Probability = 60f;

    [Range(0, 100)]
    [Tooltip("Probabilidad de Tier 2")]
    public float tier2Probability = 30f;

    [Range(0, 100)]
    [Tooltip("Probabilidad de Tier 3")]
    public float tier3Probability = 10f;

    /// <summary>
    /// Obtiene un enemigo aleatorio con un tier aleatorio basado en probabilidades
    /// </summary>
    public (EnemyData enemy, EnemyTier tier) GetRandomEnemy()
    {
        if (allEnemies == null || allEnemies.Count == 0)
        {
            Debug.LogError("❌ No hay enemigos en la base de datos");
            return (null, EnemyTier.Tier_1);
        }

        // Seleccionar enemigo aleatorio
        EnemyData randomEnemy = allEnemies[Random.Range(0, allEnemies.Count)];

        // Seleccionar tier basado en probabilidades
        EnemyTier randomTier = GetRandomTier();

        return (randomEnemy, randomTier);
    }

    /// <summary>
    /// Selecciona un tier aleatorio basado en probabilidades
    /// </summary>
    EnemyTier GetRandomTier()
    {
        float total = tier1Probability + tier2Probability + tier3Probability;
        float randomValue = Random.Range(0f, total);

        if (randomValue < tier1Probability)
        {
            return EnemyTier.Tier_1;
        }
        else if (randomValue < tier1Probability + tier2Probability)
        {
            return EnemyTier.Tier_2;
        }
        else
        {
            return EnemyTier.Tier_3;
        }
    }

    /// <summary>
    /// Validación en el Inspector
    /// </summary>
    void OnValidate()
    {
        float total = tier1Probability + tier2Probability + tier3Probability;
        
        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            Debug.LogWarning($"⚠️ EnemyDatabase: Las probabilidades suman {total:F1}% (deberían sumar 100%)");
        }

        // Advertir si la lista está vacía
        if (allEnemies == null || allEnemies.Count == 0)
        {
            Debug.LogWarning("⚠️ EnemyDatabase: No hay enemigos en la lista");
        }
    }
}