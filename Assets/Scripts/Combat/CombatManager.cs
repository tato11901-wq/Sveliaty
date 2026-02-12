using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SocialPlatforms.Impl;

public class CombatManager : MonoBehaviour
{
    [Header("Boss Rush Setup")]
    public EnemyDatabase enemyDatabase;

    [Header("Manager References")]
    public PlayerManager playerManager;
    public AbilityManager abilityManager;
    public CurseManager curseManager;

    [Header("Combat Settings")]
    private CombatMode combatMode = CombatMode.Passive;
    [Range(0, 100)] public float randomCardChance = 30f;

    // Estado del combate
    private EnemyInstance currentEnemy;
    private AffinityType selectedAttackType; // Tipo de ataque seleccionado en PlayerChooses/RPG
    private bool combatEnded = false;
    private bool waitingForCardSelection = false;
    private bool isProcessingPostCombat = false;

    // Contador de turnos
    private int turnsUsedThisCombat = 0;
    private int currentTurnNumber = 0;

    // Eventos para la UI
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float> OnAttackResult;
    public event Action<bool, int, AffinityType, int> OnCombatEnd;
    public event Action<int> OnAttemptsChanged;
    public event Action<int> OnWaitingForCardSelection;
    public event Action<int, int, int, int, EnemyInstance> GameOver;
    
    public event Action OnReadyForNextEnemy;

    public void StartNewRun(CombatMode mode)
    {
        Debug.Log("Iniciando nueva run en modo: " + mode);
        
        combatMode = mode;
        
        // Inicializar jugador
        if (playerManager != null)
        {
            playerManager.InitializeForNewRun(5);
        }
        else
        {
            Debug.LogError("PlayerManager no asignado en CombatManager");
        }
        
        StartRandomCombat();
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
            Debug.LogWarning("Tier " + tier + " no encontrado para " + enemyData.displayName);
            
            if (enemyData.enemyTierData != null && enemyData.enemyTierData.Length > 0)
            {
                tierData = enemyData.enemyTierData[0];
                Debug.Log("Usando " + tierData.enemyTier + " en su lugar");
            }
            else
            {
                Debug.LogError(enemyData.displayName + " no tiene ningun tier configurado");
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

        Debug.Log("BOSS RUSH: " + currentEnemy.enemyData.displayName + " (" + tierData.enemyTier + ")");

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

        selectedAttackType = type;
        Debug.Log("Ataque seleccionado: " + type);
    }

    AffinityType GetAttackType()
    {
        return combatMode == CombatMode.Passive 
            ? currentEnemy.enemyData.affinityType 
            : selectedAttackType;
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
        if (playerManager == null) return;
        
        currentTurnNumber++;
        
        if (curseManager != null)
        {
            curseManager.OnTurnStart();
        }
        
        if (abilityManager != null && !abilityManager.CanUseAbility(ability, playerManager.GetCurrentLife(), currentEnemy.attemptsRemaining))
        {
            Debug.LogWarning("No puedes usar esta habilidad");
            return;
        }
        
        // Aplicar costos
        playerManager.ModifyHealth(-ability.healthCost);
        if (abilityManager != null && ability.cardCost > 0)
        {
            abilityManager.SpendCards(ability.affinityType, ability.cardCost);
        }
        
        // Verificar probabilidad de exito
        bool success = true;
        if (ability.hasSuccessChance)
        {
            success = UnityEngine.Random.Range(0f, 100f) < ability.successChance;
            
            if (!success)
            {
                playerManager.ModifyHealth(-ability.onFailHealthPenalty);
                currentEnemy.attemptsRemaining -= (1 + ability.onFailTurnPenalty);
                OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
                
                Debug.Log(ability.abilityName + " FALLO");
                
                if (currentEnemy.attemptsRemaining <= 0)
                {
                    EndCombat(false, playerManager.GetScore(), 1f);
                }
                return;
            }
        }
        
        // Calcular dados
        int diceCount = CalculateDiceCount(ability);
        int diceMax = ability.diceMaxValue > 0 ? ability.diceMaxValue : 12;
        int roll = RollDice(diceCount, diceMax);
        
        // Calcular bonus de cartas
        AffinityType attackType = ability.affinityType;
        int cardBonus = playerManager.GetCards(attackType);
        
        if (ability.cardMultiplier != 0)
        {
            cardBonus = Mathf.RoundToInt(cardBonus * ability.cardMultiplier);
        }
        
        if (curseManager != null && curseManager.HasNegatedCards())
        {
            cardBonus = -cardBonus;
            Debug.Log("Tus cartas estan negadas");
        }
        
        // Calcular multiplicador de afinidad
        float multiplier = GetAffinityMultiplier(attackType);
        multiplier += ability.affinityMultiplierBonus;
        
        if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
        {
            AffinityDiscoveryTracker.RegisterDiscovery(currentEnemy.enemyData.id, attackType);
        }
        
        // Calcular total segun modo
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
            playerManager.ModifyHealth(ability.onKillHealthReward);
            Debug.Log("Recuperaste " + ability.onKillHealthReward + " HP");
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
        basicAbility.abilityName = "Ataque Basico";
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
                playerManager.AddScore(CalculateScorePerCombat(multiplier));
                EndCombat(true, playerManager.GetScore(), multiplier);
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
                playerManager.AddScore(CalculateScorePerCombat(multiplier));
                EndCombat(true, playerManager.GetScore(), multiplier);
                return true;
            }
        }
        
        currentEnemy.attemptsRemaining--;
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
        
        if (currentEnemy.attemptsRemaining <= 0)
        {
            EndCombat(false, playerManager.GetScore(), multiplier);
        }
        
        return victory;
    }

    bool ConsumeTurn(AbilityData ability)
    {
        if (ability.canAvoidTurnConsumption)
        {
            if (UnityEngine.Random.Range(0f, 100f) < ability.avoidTurnChance)
            {
                Debug.Log("No se consumio el turno");
                return false;
            }
            else
            {
                playerManager.ModifyHealth(-ability.avoidTurnFailPenalty);
                Debug.Log("Detectado: -" + ability.avoidTurnFailPenalty + " HP");
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
            Debug.LogWarning("Ya se esta procesando el fin de combate");
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

            // Registrar estadisticas
            playerManager.RegisterEnemyDefeated();
            playerManager.RegisterCombatWon();

            if (combatMode == CombatMode.Passive)
            {
                rewardCard = GetRandomAffinityType();
                playerManager.AddCards(rewardCard, 1);
                OnCombatEnd?.Invoke(victory, finalScore, rewardCard, 0);
            }
            else if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
            {
                if (lastMultiplier >= 1.5f) 
                {
                    waitingForCardSelection = true;
                    OnWaitingForCardSelection?.Invoke(finalScore);
                    Debug.Log("Debilidad explotada! Elige tu carta.");
                }
                else 
                {
                    if (UnityEngine.Random.Range(0, 100) < randomCardChance)
                    {
                        rewardCard = GetRandomAffinityType();
                        playerManager.AddCards(rewardCard, 1);
                        Debug.Log("Victoria normal. Suerte: Obtienes carta de " + rewardCard);
                        OnCombatEnd?.Invoke(true, finalScore, rewardCard, 0);
                    }
                    else
                    {
                        Debug.Log("Victoria normal. No obtienes carta.");
                        OnCombatEnd?.Invoke(true, finalScore, default, 0); 
                    }
                }
            }
        }
        else
        {
            playerManager.RegisterCombatLost();

            bool hasShield = curseManager != null && curseManager.HasDamageNegation();
            
            if (hasShield)
            {
                Debug.Log("Daño negado por maldicion");
                OnCombatEnd?.Invoke(false, finalScore, default, 0);
            }
            else
            {
                playerManager.ModifyHealth(-currentEnemy.enemyTierData.failureDamage);
                
                if (playerManager.IsAlive())
                {
                    OnCombatEnd?.Invoke(false, finalScore, rewardCard, currentEnemy.enemyTierData.failureDamage);
                }
                else
                {
                    GameOver?.Invoke(
                        finalScore, 
                        playerManager.GetCards(AffinityType.Fuerza), 
                        playerManager.GetCards(AffinityType.Agilidad), 
                        playerManager.GetCards(AffinityType.Destreza), 
                        currentEnemy
                    );
                }
            }
            
            if (curseManager != null)
            {
                curseManager.OnDefeat();
            }
        }
    }

    public bool ShouldShowCurseEvent()
    {
        if (curseManager == null) return false;
        
        bool isSpirit = currentEnemy.enemyData.isSpirit;
        return curseManager.ShouldTriggerCurseEvent(turnsUsedThisCombat, isSpirit);
    }

    public void TriggerCurseEventFromUI()
    {
        if (curseManager != null)
        {
            curseManager.TriggerCurseChoiceEvent();
        }
    }

    public void ContinueToNextEnemy()
    {
        Debug.Log("Continuando al siguiente enemigo");
        isProcessingPostCombat = false;
        StartRandomCombat();
    }
    
    public void EndRun()
    {
        GameOver?.Invoke(
            playerManager.GetScore(), 
            playerManager.GetCards(AffinityType.Fuerza), 
            playerManager.GetCards(AffinityType.Agilidad), 
            playerManager.GetCards(AffinityType.Destreza), 
            currentEnemy
        );
    }

    public void SelectRewardCard(AffinityType selectedCard)
    {
        if (!waitingForCardSelection)
        {
            Debug.LogWarning("No hay seleccion de carta pendiente");
            return;
        }

        playerManager.AddCards(selectedCard, 1);
        
        Debug.Log("Obtienes 1 carta de " + selectedCard + "! Total: " + playerManager.GetCards(selectedCard));
        
        waitingForCardSelection = false;
        
        OnCombatEnd?.Invoke(true, playerManager.GetScore(), selectedCard, 0);
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
            : selectedAttackType;

        return playerManager.GetCards(type);
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
        return playerManager.GetCards(type);
    }

    public bool HasActiveEnemy()
    {
        return currentEnemy != null;
    }

    // GETTERS
    public EnemyInstance GetCurrentEnemy() => currentEnemy;
    public bool IsCombatEnded() => combatEnded;
    public CombatMode GetCombatMode() => combatMode;
    public int GetPlayerLife() => playerManager != null ? playerManager.GetCurrentLife() : 0;
    public int GetPlayerMaxLife() => playerManager != null ? playerManager.GetMaxLife() : 100;
    public int GetCurrentTurnNumber() => currentTurnNumber;
}