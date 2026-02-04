using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SocialPlatforms.Impl;

public class CombatManager : MonoBehaviour
{
    [Header("Boss Rush Setup")]
    public EnemyDatabase enemyDatabase;

    [Header("New Systems")]
    public AbilityManager abilityManager;
    public CurseManager curseManager;

    [Header("Combat Settings")]
    private CombatMode combatMode = CombatMode.Passive;
    [Range(0, 100)] public float randomCardChance = 30f;

    // Estado del combate
    private EnemyInstance currentEnemy;
    private PlayerCombatData playerCombatData;
    private bool combatEnded = false;
    private bool waitingForCardSelection = false;
    private bool isProcessingPostCombat = false; // NUEVO: Flag para evitar múltiples llamadas

    // NUEVO: Contador de turnos
    private int turnsUsedThisCombat = 0;
    private int currentTurnNumber = 0;

    // Eventos para la UI
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float> OnAttackResult;
    public event Action<bool, int, AffinityType, int> OnCombatEnd;
    public event Action<int> OnAttemptsChanged;
    public event Action<int> OnWaitingForCardSelection;
    public event Action<int, int, int, int, EnemyInstance> GameOver;
    
    // NUEVO: Evento para notificar que se puede continuar al siguiente enemigo
    public event Action OnReadyForNextEnemy;

    public void StartNewRun(CombatMode mode)
    {
        Debug.Log($"Iniciando nueva run en modo nuevo: {mode}");
        
        combatMode = mode;
        InitializePlayerData();
        StartRandomCombat();
    }

    void InitializePlayerData()
    {
        playerCombatData = new PlayerCombatData();
        
        PlayerCombatData.cards[AffinityType.Fuerza] = 0;
        PlayerCombatData.cards[AffinityType.Agilidad] = 0;
        PlayerCombatData.cards[AffinityType.Destreza] = 0;

        for (int i = 0; i < 5; i++)
        {
            AffinityType randomType = GetRandomAffinityType();
            PlayerCombatData.cards[randomType]++;
            Debug.Log($"Carta inicial {i + 1}: {randomType}");
        }
        
        playerCombatData.playerLife = playerCombatData.playerMaxLife;
        playerCombatData.score = 0;
        
        Debug.Log($"Vida inicializada: {playerCombatData.playerLife}/{playerCombatData.playerMaxLife}");
    }

    public void StartRandomCombat()
    {
        if (enemyDatabase == null)
        {
            Debug.LogError("No hay EnemyDatabase asignado");
            return;
        }

        var (randomEnemy, randomTier) = enemyDatabase.GetRandomEnemy();

        if (randomEnemy == null)
        {
            Debug.LogError("No se pudo obtener enemigo aleatorio");
            return;
        }

        StartCombat(randomEnemy, randomTier);
    }

    void StartCombat(EnemyData enemyData, EnemyTier tier)
    {
        EnemyTierData tierData = GetTierData(enemyData, tier);

        if (tierData == null)
        {
            Debug.LogWarning($"Tier {tier} no encontrado para {enemyData.displayName}. Usando tier disponible.");
            
            if (enemyData.enemyTierData != null && enemyData.enemyTierData.Length > 0)
            {
                tierData = enemyData.enemyTierData[0];
                Debug.Log($"→ Usando {tierData.enemyTier} en su lugar");
            }
            else
            {
                Debug.LogError($"{enemyData.displayName} no tiene ningún tier configurado");
                return;
            }
        }
        
        currentEnemy = new EnemyInstance(enemyData, tierData);
        combatEnded = false;
        isProcessingPostCombat = false;

        turnsUsedThisCombat = 0;
        currentTurnNumber = 0;

        if (curseManager != null)
        {
            curseManager.OnPreCombat(currentEnemy);
        }

        Debug.Log($"\nBOSS RUSH: {currentEnemy.enemyData.displayName} ({tierData.enemyTier})");

        OnCombatStart?.Invoke(currentEnemy);
    }

    EnemyTierData GetTierData(EnemyData enemy, EnemyTier desiredTier)
    {
        if (enemy == null || enemy.enemyTierData == null)
            return null;

        foreach (EnemyTierData tierData in enemy.enemyTierData)
        {
            if (tierData.enemyTier == desiredTier)
                return tierData;
        }

        return null;
    }

    public void SelectAttackType(AffinityType type)
    {
        if (combatMode != CombatMode.PlayerChooses && combatMode != CombatMode.TraditionalRPG)
        {
            Debug.LogWarning("Solo puedes seleccionar ataque en modo PlayerChooses");
            return;
        }

        playerCombatData.selectedAttackType = type;
        Debug.Log($"Ataque seleccionado: {type}");
    }

    AffinityType GetAttackType()
    {
        return combatMode == CombatMode.Passive 
            ? currentEnemy.enemyData.affinityType 
            : playerCombatData.selectedAttackType;
    }

    float GetAffinityMultiplier(AffinityType attackType)
    {
        if (combatMode == CombatMode.Passive)
            return 1f;

        if (currentEnemy.enemyData.affinityRelations == null)
            return 1f;

        foreach (var relation in currentEnemy.enemyData.affinityRelations)
        {
            if (relation.type == attackType)
            {
                return relation.multiplier switch
                {
                    AffinityMultiplier.Weak => 1.5f,
                    AffinityMultiplier.Neutral => 1f,
                    AffinityMultiplier.Strong => 0.5f,
                    AffinityMultiplier.Immune => 0f,
                    _ => 1f
                };
            }
        }

        return 1f;
    }

    public void PlayerAttempt(AbilityData ability)
    {
        if (combatEnded) return;
        
        currentTurnNumber++;
        
        if (curseManager != null)
        {
            curseManager.OnTurnStart();
        }
        
        if (abilityManager != null && !abilityManager.CanUseAbility(ability, playerCombatData.playerLife, currentEnemy.attemptsRemaining))
        {
            Debug.LogWarning("No puedes usar esta habilidad");
            return;
        }
        
        playerCombatData.playerLife -= ability.healthCost;
        if (abilityManager != null && ability.cardCost > 0)
        {
            abilityManager.SpendCards(ability.affinityType, ability.cardCost);
        }
        
        bool success = true;
        if (ability.hasSuccessChance)
        {
            success = UnityEngine.Random.Range(0f, 100f) < ability.successChance;
            
            if (!success)
            {
                playerCombatData.playerLife -= ability.onFailHealthPenalty;
                currentEnemy.attemptsRemaining -= (1 + ability.onFailTurnPenalty);
                OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
                
                Debug.Log($"{ability.abilityName} FALLÓ");
                
                if (currentEnemy.attemptsRemaining <= 0)
                {
                    EndCombat(false, playerCombatData.score, 1f);
                }
                return;
            }
        }
        
        int diceCount = CalculateDiceCount(ability);
        int diceMax = ability.diceMaxValue > 0 ? ability.diceMaxValue : 12;
        int roll = RollDice(diceCount, diceMax);
        
        AffinityType attackType = ability.affinityType;
        int cardBonus = PlayerCombatData.cards[attackType];
        
        if (ability.cardMultiplier != 0)
        {
            cardBonus = Mathf.RoundToInt(cardBonus * ability.cardMultiplier);
        }
        
        if (curseManager != null && curseManager.HasNegatedCards())
        {
            cardBonus = -cardBonus;
            Debug.Log("Tus cartas están negadas");
        }
        
        float multiplier = GetAffinityMultiplier(attackType);
        multiplier += ability.affinityMultiplierBonus;
        
        if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
        {
            AffinityDiscoveryTracker.RegisterDiscovery(currentEnemy.enemyData.id, attackType);
        }
        
        int totalBase = 0;
        int totalFinal = 0;
        
        if (combatMode == CombatMode.TraditionalRPG)
        {
            totalBase = roll + cardBonus;
            totalFinal = Mathf.RoundToInt(totalBase * multiplier);
        }
        else if (combatMode == CombatMode.Passive)
        {
            totalBase = roll + cardBonus;
            totalFinal = totalBase;
        }
        else if (combatMode == CombatMode.PlayerChooses)
        {
            totalBase = roll;
            totalFinal = Mathf.RoundToInt(totalBase + (cardBonus * multiplier));
        }
        
        bool victory = EvaluateAttack(totalFinal, multiplier);
        
        OnAttackResult?.Invoke(roll, cardBonus, totalFinal, multiplier);
        
        if (victory && ability.hasOnKillEffect)
        {
            playerCombatData.playerLife += ability.onKillHealthReward;
            Debug.Log($"Recuperaste {ability.onKillHealthReward} HP");
        }
        
        bool consumedTurn = ConsumeTurn(ability);
        
        if (consumedTurn)
        {
            turnsUsedThisCombat++;
        }
    }

    public void PlayerAttempt()
    {
        AbilityData basicAbility = ScriptableObject.CreateInstance<AbilityData>();
        basicAbility.abilityName = "Ataque Básico";
        basicAbility.affinityType = GetAttackType();
        basicAbility.cardCost = 0;
        basicAbility.healthCost = 0;
        basicAbility.turnCost = 1;
        basicAbility.diceModifier = 0;
        basicAbility.diceMaxValue = 0;
        basicAbility.diceMultiplier = 0;
        basicAbility.diceAddition = 0;
        basicAbility.affinityMultiplierBonus = 0;
        basicAbility.cardMultiplier = 0;
        basicAbility.hasSuccessChance = false;
        basicAbility.hasOnKillEffect = false;
        basicAbility.hasOnFailEffect = false;
        basicAbility.canAvoidTurnConsumption = false;

        PlayerAttempt(basicAbility);
    }

    int CalculateDiceCount(AbilityData ability)
    {
        int baseCount = combatMode == CombatMode.TraditionalRPG 
            ? currentEnemy.currentRPGDiceCount 
            : currentEnemy.enemyTierData.diceCount;
        
        baseCount += ability.diceModifier;
        
        if (ability.diceMultiplier != 0)
        {
            baseCount = Mathf.RoundToInt(baseCount * ability.diceMultiplier);
        }
        baseCount += ability.diceAddition;
        
        return Mathf.Max(1, baseCount);
    }

    int RollDice(int diceCount, int maxValue)
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += UnityEngine.Random.Range(1, maxValue + 1);
        }
        return total;
    }

    bool EvaluateAttack(int totalFinal, float multiplier)
    {
        bool victory = false;
        
        if (combatMode == CombatMode.TraditionalRPG)
        {
            currentEnemy.currentRPGHealth -= totalFinal;
            
            if (currentEnemy.currentRPGHealth <= 0)
            {
                victory = true;
                playerCombatData.score += CalculateScorePerCombat(multiplier);
                EndCombat(true, playerCombatData.score, multiplier);
                return true;
            }
        }
        else
        {
            bool invertedCondition = curseManager != null && curseManager.HasInvertedVictoryCondition();
            
            bool normalSuccess = totalFinal >= currentEnemy.enemyTierData.healthThreshold;
            victory = invertedCondition ? (totalFinal <= currentEnemy.enemyTierData.healthThreshold) : normalSuccess;
            
            if (victory)
            {
                playerCombatData.score += CalculateScorePerCombat(multiplier);
                EndCombat(true, playerCombatData.score, multiplier);
                return true;
            }
        }
        
        currentEnemy.attemptsRemaining--;
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
        
        if (currentEnemy.attemptsRemaining <= 0)
        {
            EndCombat(false, playerCombatData.score, multiplier);
        }
        
        return victory;
    }

    bool ConsumeTurn(AbilityData ability)
    {
        if (ability.canAvoidTurnConsumption)
        {
            if (UnityEngine.Random.Range(0f, 100f) < ability.avoidTurnChance)
            {
                Debug.Log("No se consumió el turno");
                return false;
            }
            else
            {
                playerCombatData.playerLife -= ability.avoidTurnFailPenalty;
                Debug.Log($"Detectado: -{ability.avoidTurnFailPenalty} HP");
            }
        }
        
        currentEnemy.attemptsRemaining -= ability.turnCost;
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
        
        return true;
    }

    void EndCombat(bool victory, int finalScore, float lastMultiplier)
    {
        if (isProcessingPostCombat)
        {
            Debug.LogWarning("Ya se está procesando el fin de combate");
            return;
        }

        isProcessingPostCombat = true;
        combatEnded = true;
        AffinityType rewardCard = default;

        if (victory)
        {
            if (curseManager != null)
            {
                curseManager.OnPostCombat(true);
            }

            if (abilityManager != null)
            {
                abilityManager.OnCombatWon();
                abilityManager.CheckUnlocks();
            }

            if (combatMode == CombatMode.Passive)
            {
                rewardCard = GetRandomAffinityType();
                PlayerCombatData.cards[rewardCard]++;
                OnCombatEnd?.Invoke(victory, finalScore, rewardCard, 0);
            }
            else if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
            {
                if (lastMultiplier >= 1.5f) 
                {
                    waitingForCardSelection = true;
                    OnWaitingForCardSelection?.Invoke(finalScore);
                    Debug.Log("¡Debilidad explotada! Elige tu carta.");
                }
                else 
                {
                    if (UnityEngine.Random.Range(0, 100) < randomCardChance)
                    {
                        rewardCard = GetRandomAffinityType();
                        PlayerCombatData.cards[rewardCard]++;
                        Debug.Log($"Victoria normal. Suerte: Obtienes carta de {rewardCard}");
                        OnCombatEnd?.Invoke(true, finalScore, rewardCard, 0);
                    }
                    else
                    {
                        Debug.Log("Victoria normal. No obtienes carta (no explotaste debilidad).");
                        OnCombatEnd?.Invoke(true, finalScore, default, 0); 
                    }
                }
            }

            // NUEVO: NO llamar directamente a CheckCurseEvent, la UI lo manejará
        }
        else
        {
            bool hasShield = curseManager != null && curseManager.HasDamageNegation();
            
            if (hasShield)
            {
                Debug.Log("🛡️ Daño negado por maldición");
                OnCombatEnd?.Invoke(false, finalScore, default, 0);
            }
            else
            {
                playerCombatData.playerLife -= currentEnemy.enemyTierData.failureDamage;
                
                if (playerCombatData.playerLife > 0)
                {
                    OnCombatEnd?.Invoke(false, finalScore, rewardCard, currentEnemy.enemyTierData.failureDamage);
                }
                else
                {
                    GameOver?.Invoke(finalScore, PlayerCombatData.cards[AffinityType.Fuerza], 
                                   PlayerCombatData.cards[AffinityType.Agilidad], 
                                   PlayerCombatData.cards[AffinityType.Destreza], currentEnemy);
                }
            }
            
            if (curseManager != null)
            {
                curseManager.OnDefeat();
            }
        }
    }

    // NUEVO: Método público para que la UI verifique si hay evento de maldición
    public bool ShouldShowCurseEvent()
    {
        if (curseManager == null) return false;
        
        bool isSpirit = currentEnemy.enemyData.isSpirit;
        return curseManager.ShouldTriggerCurseEvent(turnsUsedThisCombat, isSpirit);
    }

    // NUEVO: Método público para activar el evento de maldición desde la UI
    public void TriggerCurseEventFromUI()
    {
        if (curseManager != null)
        {
            curseManager.TriggerCurseChoiceEvent();
        }
    }

    // NUEVO: Llamar esto cuando toda la secuencia post-combate termine
    public void ContinueToNextEnemy()
    {
        Debug.Log("🔄 Continuando al siguiente enemigo");
        isProcessingPostCombat = false;
        StartRandomCombat();
    }
    
    public void EndRun()
    {
        int finalScore = playerCombatData.score;
        int finalFuerza = PlayerCombatData.cards[AffinityType.Fuerza];
        int finalAgilidad = PlayerCombatData.cards[AffinityType.Agilidad];
        int finalDestreza = PlayerCombatData.cards[AffinityType.Destreza];

        GameOver?.Invoke(finalScore, finalFuerza, finalAgilidad, finalDestreza, currentEnemy);
    }

    public void SelectRewardCard(AffinityType selectedCard)
    {
        if (!waitingForCardSelection)
        {
            Debug.LogWarning("No hay selección de carta pendiente");
            return;
        }

        PlayerCombatData.cards[selectedCard]++;
        
        Debug.Log($"¡Obtienes 1 carta de {selectedCard}! Total: {PlayerCombatData.cards[selectedCard]}");
        
        waitingForCardSelection = false;
        
        OnCombatEnd?.Invoke(true, playerCombatData.score, selectedCard, 0);
    }

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])System.Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    public void NextEnemy()
    {
        StartRandomCombat();
    }

    public int GetCurrentCards()
    {
        if (currentEnemy == null) return 0;

        AffinityType type = combatMode == CombatMode.Passive 
            ? currentEnemy.enemyData.affinityType 
            : playerCombatData.selectedAttackType;

        return PlayerCombatData.cards[type];
    }

    public int CalculateScorePerCombat(float multiplier)
    {
        int Newscore = 0;

        if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_1)
        {
            Newscore = 1;
        }
        else if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_2)
        {
            Newscore = 2;
        }
        else if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_3)
        {
            Newscore = 3;
        }

        if(multiplier >= 1.5f)
        {
            Newscore += 1;
        }

        return Newscore;
    }

    public int GetCardsOfType(AffinityType type)
    {
        return PlayerCombatData.cards[type];
    }

    public bool HasActiveEnemy()
    {
        return currentEnemy != null;
    }

    // GETTERS
    public EnemyInstance GetCurrentEnemy() => currentEnemy;
    public bool IsCombatEnded() => combatEnded;
    public CombatMode GetCombatMode() => combatMode;
    public int GetPlayerLife() => playerCombatData?.playerLife ?? 0;
    public int GetPlayerMaxLife() => playerCombatData?.playerMaxLife ?? 100;
    public int GetCurrentTurnNumber() => currentTurnNumber;
}