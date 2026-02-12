using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public AbilityDatabase abilityDatabase;
    public PlayerManager playerManager;
    
    // Habilidades desbloqueadas
    private HashSet<int> unlockedAbilityIds = new HashSet<int>();
    
    // Sistema de gasto de cartas (configurable)
    public enum CardSpendingMode 
    { 
        Absolute,      // Opcion 1: Gasto permanente
        PerInstance,   // Opcion 2: Recupera al ganar
        Relative       // Opcion 3: Cuenta el maximo historico
    }
    public CardSpendingMode spendingMode = CardSpendingMode.PerInstance;
    
    // Para Opcion 2 y 3
    private Dictionary<AffinityType, int> cardsSpentThisCombat = new Dictionary<AffinityType, int>();
    private Dictionary<AffinityType, int> maxCardsEverHad = new Dictionary<AffinityType, int>(); // Opcion 3
    
    void Start()
    {
        InitializeAbilities();
    }
    
    void InitializeAbilities()
    {
        // Desbloquear habilidades basicas
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
        if (ability.cardCost > playerManager.GetCards(ability.affinityType)) return false;
        
        return true;
    }
    
    public void SpendCards(AffinityType type, int amount)
    {
        playerManager.RemoveCards(type, amount);
        cardsSpentThisCombat[type] += amount;
        
        // Actualizar maximo historico (Opcion 3)
        UpdateMaxCards();
    }
    
    public void OnCombatWon()
    {
        if (spendingMode == CardSpendingMode.PerInstance)
        {
            // Opcion 2: Recuperar cartas gastadas
            playerManager.AddCards(AffinityType.Fuerza, cardsSpentThisCombat[AffinityType.Fuerza]);
            playerManager.AddCards(AffinityType.Agilidad, cardsSpentThisCombat[AffinityType.Agilidad]);
            playerManager.AddCards(AffinityType.Destreza, cardsSpentThisCombat[AffinityType.Destreza]);
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
                : playerManager.GetCards(ability.affinityType);
            
            if (currentCards >= ability.unlockRequirement)
            {
                UnlockAbility(ability.id);
            }
        }
    }
    
    void UnlockAbility(int abilityId)
    {
        unlockedAbilityIds.Add(abilityId);
        Debug.Log("Habilidad desbloqueada: " + abilityDatabase.GetAbilityById(abilityId).abilityName);
    }
    
    void UpdateMaxCards()
    {
        foreach (AffinityType type in System.Enum.GetValues(typeof(AffinityType)))
        {
            int current = playerManager.GetCards(type);
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
            _ => playerManager.GetCards(type)
        };
    }
}