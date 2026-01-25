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
}

[System.Serializable]
public class AffinityRelation
{
    public AffinityType type;
    public AffinityMultiplier multiplier;
}