using System;
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
    public int RPGLife; // Vida del enemigo en modo RPG Tradicional

    public String GetEnemyTier()
    {
        if(enemyTier == EnemyTier.Tier_1)
        {
            return "Enemy Tier 1";
        }
        else if(enemyTier == EnemyTier.Tier_2)
        {
            return "Enemy Tier 2";
        }
        else if(enemyTier == EnemyTier.Tier_3)
        {
            return "Enemy Tier 3";
        }
        return "Tier_1";
    }
}