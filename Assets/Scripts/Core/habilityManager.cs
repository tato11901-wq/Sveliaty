using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public AbilityDatabase abilityDatabase;
    
    // Habilidades desbloqueadas
    private HashSet<int> unlockedAbilityIds = new HashSet<int>();
    
    // Sistema de gasto de cartas (configurable)
    public enum CardSpendingMode 
    { 
        Absolute,      // Opción 1: Gasto permanente
        PerInstance,   // Opción 2: Recupera al ganar
        Relative       // Opción 3: Cuenta el máximo histórico
    }
    public CardSpendingMode spendingMode = CardSpendingMode.PerInstance;
    
    // Para Opción 2 y 3
    private Dictionary<AffinityType, int> cardsSpentThisCombat = new Dictionary<AffinityType, int>();
    private Dictionary<AffinityType, int> maxCardsEverHad = new Dictionary<AffinityType, int>(); // Opción 3
    
    void Start()
    {
        InitializeAbilities();
    }
    
    void InitializeAbilities()
    {
        // Desbloquear habilidades básicas
        foreach (var ability in abilityDatabase.allAbilities)
        {
            if (ability.isBasicAbility)
            {
                unlockedAbilityIds.Add(ability.id);
            }
        }
        
        // Inicializar tracking
        cardsSpentThisCombat[AffinityType.Fuerza] = 0;
        cardsSpentThisCombat[AffinityType.Agilidad] = 0;
        cardsSpentThisCombat[AffinityType.Destreza] = 0;
        
        UpdateMaxCards();
    }
    
    public List<AbilityData> GetAvailableAbilities(AffinityType type)
    {
        return abilityDatabase.GetAbilitiesByAffinity(type)
            .Where(a => unlockedAbilityIds.Contains(a.id))
            .ToList();
    }
    
    public bool CanUseAbility(AbilityData ability, int currentHealth, int remainingTurns)
    {
        // Verificar vida suficiente
        if (ability.healthCost > currentHealth) return false;
        
        // Verificar turnos suficientes
        if (ability.turnCost > remainingTurns) return false;
        
        // Verificar cartas suficientes
        if (ability.cardCost > PlayerCombatData.cards[ability.affinityType]) return false;
        
        return true;
    }
    
    public void SpendCards(AffinityType type, int amount)
    {
        PlayerCombatData.cards[type] -= amount;
        cardsSpentThisCombat[type] += amount;
        
        // Actualizar máximo histórico (Opción 3)
        UpdateMaxCards();
    }
    
    public void OnCombatWon()
    {
        if (spendingMode == CardSpendingMode.PerInstance)
        {
            // Opción 2: Recuperar cartas gastadas
            PlayerCombatData.cards[AffinityType.Fuerza] += cardsSpentThisCombat[AffinityType.Fuerza];
            PlayerCombatData.cards[AffinityType.Agilidad] += cardsSpentThisCombat[AffinityType.Agilidad];
            PlayerCombatData.cards[AffinityType.Destreza] += cardsSpentThisCombat[AffinityType.Destreza];
        }
        
        // Resetear contador de gasto
        cardsSpentThisCombat[AffinityType.Fuerza] = 0;
        cardsSpentThisCombat[AffinityType.Agilidad] = 0;
        cardsSpentThisCombat[AffinityType.Destreza] = 0;
    }
    
    public void CheckUnlocks()
    {
        foreach (var ability in abilityDatabase.allAbilities)
        {
            if (ability.isBasicAbility) continue;
            if (unlockedAbilityIds.Contains(ability.id)) continue;
            
            // Verificar si cumple requisito de desbloqueo
            int currentCards = spendingMode == CardSpendingMode.Relative 
                ? maxCardsEverHad[ability.affinityType]
                : PlayerCombatData.cards[ability.affinityType];
            
            if (currentCards >= ability.unlockRequirement)
            {
                UnlockAbility(ability.id);
            }
        }
    }
    
    void UnlockAbility(int abilityId)
    {
        unlockedAbilityIds.Add(abilityId);
        Debug.Log($"🎉 Habilidad desbloqueada: {abilityDatabase.GetAbilityById(abilityId).abilityName}");
    }
    
    void UpdateMaxCards()
    {
        foreach (AffinityType type in System.Enum.GetValues(typeof(AffinityType)))
        {
            int current = PlayerCombatData.cards[type];
            if (!maxCardsEverHad.ContainsKey(type) || current > maxCardsEverHad[type])
            {
                maxCardsEverHad[type] = current;
            }
        }
    }
    
    public int GetFinalCardCount(AffinityType type)
    {
        return spendingMode switch
        {
            CardSpendingMode.Relative => maxCardsEverHad[type],
            _ => PlayerCombatData.cards[type]
        };
    }
}