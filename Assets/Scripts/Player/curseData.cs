using UnityEngine;


[CreateAssetMenu(menuName = "Sveliaty/Card/Curse Data")]
public class CurseData : ScriptableObject
{
    public int id;
    public string curseName;
    public string description;
    public CurseType type;
    public Sprite icon;
    
    // Activación
    public CurseActivationType activationType;
    public bool requiresPlayerActivation; // Para cartas guardadas
    public bool mustActivateOnTurnOne; // Para "No recibir daño"
    
    // Efecto
    public CurseEffect effectType;
    public int effectValue; // Cantidad de HP, cartas, etc.
    public int duration; // Combates/turnos que dura (0 = instant, -1 = hasta usar)
    
    // Restricciones de modo
    public bool passiveOnly;
    public bool rpgOnly;
    
    // Para efectos especiales
    public bool affectsVictoryCondition;
    public bool affectsEnemyHealth;
    public float enemyHealthMultiplier; // 0.5 para "pierde 50% HP"
}