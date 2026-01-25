using UnityEngine;

[CreateAssetMenu(menuName = "Sveliaty/Enemy Tier Data")]
public class EnemyTierData : ScriptableObject
{
    public EnemyTier enemyTier; // Tier del enemigo
    
    public Sprite sprite; // Sprite representativo del enemigo

    public int healthThreshold; // Umbral de salud del enemigo
    public int diceCount; // Cantidad de dados correspiondientes al enemigo
    public int maximunDiceThrow; // Cantidad de tiradas de dados para el enemigo
    public int failureDamage; // Castigo al usuario al perder el combate
}