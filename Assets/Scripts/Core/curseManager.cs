using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CurseManager : MonoBehaviour
{
    [Header("References")]
    public CurseDatabase curseDatabase;
    
    [Header("Curse Event Settings")]
    [Range(0, 100)]
    public float baseCurseChance = 5f; // 5% base
    [Range(0, 100)]
    public float curseChancePerTurn = 5f; // +5% por turno
    [Range(0, 100)]
    public float spiritCurseChance = 80f; // Alta para espíritus
    
    // Inventario de maldiciones activas
    private List<CurseInstance> activeCurses = new List<CurseInstance>();
    
    // Contadores
    private int combatsWithoutRewards = 0;
    
    // Eventos
    public event Action<CurseData> OnCurseObtained;
    public event Action<CurseData> OnCurseActivated;
    public event Action<List<CurseData>> OnCurseChoiceEvent; // Para elegir 1 de 3
    
    public bool ShouldTriggerCurseEvent(int turnsUsed, bool isSpirit)
    {
        if (isSpirit)
        {
            return UnityEngine.Random.Range(0f, 100f) < spiritCurseChance;
        }
        
        float chance = baseCurseChance + (turnsUsed * curseChancePerTurn);
        return UnityEngine.Random.Range(0f, 100f) < chance;
    }
    
    public void TriggerCurseChoiceEvent()
    {
        List<CurseData> options = curseDatabase.GetThreeRandomCurses();
        Debug.Log($"🎴 Opciones generadas: {options.Count}");
        
        if (OnCurseChoiceEvent == null)
        {
            Debug.LogError("❌ OnCurseChoiceEvent no tiene suscriptores!");
            return;
        }
        
        Debug.Log($"✅ Invocando evento con {OnCurseChoiceEvent.GetInvocationList().Length} suscriptores");
        OnCurseChoiceEvent?.Invoke(options);
    }
    
    public void ObtainCurse(CurseData curse)
    {
        Debug.Log($"🎴 Maldición obtenida: {curse.curseName}");
        
        // Si es instantánea, aplicar efecto inmediatamente
        if (curse.activationType == CurseActivationType.Instant)
        {
            ApplyInstantEffect(curse);
        }
        else
        {
            // Añadir al inventario
            activeCurses.Add(new CurseInstance(curse));
        }
        
        OnCurseObtained?.Invoke(curse);
    }
    
    void ApplyInstantEffect(CurseData curse)
    {
        switch (curse.effectType)
        {
            case CurseEffect.ModifyHealth:
                // CombatManager modificará la vida
                break;
            case CurseEffect.ModifyCards:
                if (curse.effectValue > 0)
                {
                    // Dar carta aleatoria
                    AffinityType randomType = GetRandomAffinityType();
                    PlayerCombatData.cards[randomType] += curse.effectValue;
                }
                else
                {
                    // Quitar carta aleatoria
                    RemoveRandomCard(Mathf.Abs(curse.effectValue));
                }
                break;
            case CurseEffect.GamblingDice:
                int roll = UnityEngine.Random.Range(1, 13);
                int effect = (roll % 2 == 0) ? roll : -roll;
                // CombatManager aplicará el efecto de vida
                Debug.Log($"🎲 Gambling: {roll} → {(effect > 0 ? "+" : "")}{effect} HP");
                break;
        }
    }
    
    // Métodos para aplicar efectos en diferentes fases
    public void OnPreCombat(EnemyInstance enemy)
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PreCombat) continue;
            
            switch (curse.data.effectType)
            {
                case CurseEffect.WeakenEnemy:
                    enemy.currentRPGHealth = Mathf.RoundToInt(enemy.currentRPGHealth * curse.data.enemyHealthMultiplier);
                    Debug.Log($"💀 Enemigo debilitado a {enemy.currentRPGHealth} HP");
                    break;
                case CurseEffect.InvertVictoryCondition:
                    // Este se maneja en CombatManager
                    Debug.Log($"🔄 Condición de victoria invertida");
                    break;
            }
            
            // Reducir duración
            if (curse.remainingDuration > 0)
            {
                curse.remainingDuration--;
                if (curse.remainingDuration == 0)
                {
                    activeCurses.Remove(curse);
                }
            }
        }
    }
    
    public void OnTurnStart()
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.TurnStart) continue;
            
            switch (curse.data.effectType)
            {
                case CurseEffect.NegateCards:
                    Debug.Log($"⚠️ Cartas negadas este turno");
                    break;
            }
            
            // Reducir duración
            if (curse.remainingDuration > 0)
            {
                curse.remainingDuration--;
                if (curse.remainingDuration == 0)
                {
                    activeCurses.Remove(curse);
                }
            }
        }
    }
    
    public void OnPostCombat(bool victory)
    {
        if (!victory) return;
        
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PostCombat) continue;
            
            // Reducir duración
            if (curse.remainingDuration > 0)
            {
                curse.remainingDuration--;
                if (curse.remainingDuration == 0)
                {
                    activeCurses.Remove(curse);
                }
            }
        }
    }
    
    // Métodos para activar maldiciones guardadas
    public bool CanActivateCurse(CurseData curse, int currentTurn)
    {
        if (!curse.requiresPlayerActivation) return false;
        
        var instance = activeCurses.FirstOrDefault(c => c.data.id == curse.id);
        if (instance == null) return false;
        
        // Verificar si debe activarse en turno 1
        if (curse.mustActivateOnTurnOne && currentTurn != 1) return false;
        
        return !instance.isActivated;
    }
    
    public void ActivateCurse(CurseData curse)
    {
        var instance = activeCurses.FirstOrDefault(c => c.data.id == curse.id);
        if (instance == null) return;
        
        instance.isActivated = true;
        OnCurseActivated?.Invoke(curse);
        
        // Aplicar efecto según el tipo
        switch (curse.effectType)
        {
            case CurseEffect.EscapeCombat:
                // CombatManager manejará el escape
                activeCurses.Remove(instance);
                break;
            case CurseEffect.NegateDamage:
                // Se mantiene activa y se elimina al final del combate
                break;
        }
    }
    
    // Verificadores para CombatManager
    public bool HasInvertedVictoryCondition()
    {
        return activeCurses.Any(c => 
            c.data.effectType == CurseEffect.InvertVictoryCondition && 
            c.remainingDuration != 0);
    }
    
    public bool HasNegatedCards()
    {
        return activeCurses.Any(c => 
            c.data.effectType == CurseEffect.NegateCards && 
            c.data.activationType == CurseActivationType.TurnStart &&
            c.remainingDuration != 0);
    }
    
    public bool HasDamageNegation()
    {
        return activeCurses.Any(c => 
            c.data.effectType == CurseEffect.NegateDamage && 
            c.isActivated);
    }
    
    public bool HasRewardBlock()
    {
        return combatsWithoutRewards > 0;
    }
    
    public void OnDefeat()
    {
        // Verificar si tiene escudo de derrota
        var shield = activeCurses.FirstOrDefault(c => 
            c.data.effectType == CurseEffect.NegateDamage && 
            c.remainingDuration == -1);
        
        if (shield != null)
        {
            Debug.Log($"🛡️ Escudo de derrota usado");
            activeCurses.Remove(shield);
        }
    }
    
    // Helpers
    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])System.Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }
    
    void RemoveRandomCard(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Obtener tipos con cartas disponibles
            var availableTypes = new List<AffinityType>();
            foreach (AffinityType type in System.Enum.GetValues(typeof(AffinityType)))
            {
                if (PlayerCombatData.cards[type] > 0)
                    availableTypes.Add(type);
            }
            
            if (availableTypes.Count == 0) break;
            
            AffinityType randomType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
            PlayerCombatData.cards[randomType]--;
            Debug.Log($"Perdiste 1 carta de {randomType}");
        }
    }
    
    public List<CurseInstance> GetActiveCurses() => activeCurses;
}