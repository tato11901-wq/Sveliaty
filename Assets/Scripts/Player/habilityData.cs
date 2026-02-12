using UnityEngine;

[CreateAssetMenu(menuName = "Sveliaty/Card/Ability Data")]
public class AbilityData : ScriptableObject
{
    public int id;
    public string abilityName;
    public string description;
    public AffinityType affinityType; // Fuerza, Agilidad, Destreza
    public Sprite icon;
    
    // Costos
    public int cardCost; // Cuántas cartas consume (0 para básicos)
    public int healthCost; // HP que consume (ej: Puño Sangriento: 5)
    public int turnCost; // Turnos que consume (default: 1)
    
    // Modificadores de dados
    public int diceModifier; // +/- dados (ej: Puño Cargado: +1, Corte Vampírico: -1)
    public int diceMaxValue; // Override del tipo de dado (ej: Golpes Rápidos: 6)
    public float diceMultiplier; // Multiplicador de cantidad de dados (ej: Golpes Rápidos: 2.0)
    public int diceAddition; // Suma fija de dados (ej: Golpes Rápidos: +1)
    
    // Modificadores de multiplicador
    public float affinityMultiplierBonus; // Bonus al multiplicador (ej: Puño Sangriento: +0.5)
    public float cardMultiplier; // Multiplicador de cartas (ej: Corte Profundo éxito: 2.0)
    
    // Probabilidades
    public bool hasSuccessChance;
    public float successChance; // % de éxito (ej: Corte Profundo: 60)
    
    // Efectos condicionales
    public bool hasOnKillEffect;
    public int onKillHealthReward; // HP al matar (ej: Corte Vampírico: +5)
    
    public bool hasOnFailEffect;
    public int onFailHealthPenalty; // HP al fallar (ej: Corte Profundo: -10)
    public int onFailTurnPenalty; // Turnos perdidos al fallar (ej: Corte Profundo: -1)
    
    // Efectos especiales
    public bool canAvoidTurnConsumption; // Para Golpe Fantasma
    public float avoidTurnChance; // 50% para Golpe Fantasma
    public int avoidTurnFailPenalty; // -5 HP si falla
    
    // Requisitos de desbloqueo
    public int unlockRequirement; // Cantidad de cartas necesarias para desbloquear
    public bool isBasicAbility; // true para básicos (siempre desbloqueados)
}