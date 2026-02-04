using UnityEngine;

[CreateAssetMenu(menuName = "Sveliaty/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public int id;
    public string displayName;

    [Header("Modo Passive")]
    [Tooltip("Afinidad que suma automáticamente en modo Passive")]
    public AffinityType affinityType;

    [Header("Modo PlayerChooses")]
    [Tooltip("Relaciones de debilidad/resistencia del enemigo")]
    public AffinityRelation[] affinityRelations;
    
    public EnemyTierData[] enemyTierData;
    
    [Header("Sistema de Maldiciones")]
    [Tooltip("Si es true, este enemigo tiene alta probabilidad de maldecir")]
    public bool isSpirit = false;
}

[System.Serializable]
public class AffinityRelation
{
    public AffinityType type;
    public AffinityMultiplier multiplier;
}