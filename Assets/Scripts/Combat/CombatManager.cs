using UnityEngine;
using System;
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

    // NUEVO: Contador de turnos
    private int turnsUsedThisCombat = 0;
    private int currentTurnNumber = 0;

    // Eventos para la UI
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float> OnAttackResult; // (roll, bonus, total, multiplier)
    public event Action<bool, int, AffinityType, int> OnCombatEnd; // (victory, finalScore, cardReward, lifeLost)
    public event Action<int> OnAttemptsChanged; // (remainingAttempts)
    public event Action<int> OnWaitingForCardSelection; // (finalScore) - Nuevo evento
    public event Action<int, int, int, int, EnemyInstance> GameOver; // Final score, Final Cards(Fuerza, Agilidad, Destreza), defeated by this enemy

    /// <summary>
    /// Inicia una nueva run con el modo especificado
    /// Llamado por el GameManager al comenzar el juego
    /// </summary>
    public void StartNewRun(CombatMode mode)
    {
        Debug.Log($"Iniciando nueva run en modo nuevo: {mode}");
        
        // Establecer el modo de combate para toda la run
        combatMode = mode;
        
        // Reiniciar datos del jugador
        InitializePlayerData();
        
        // Iniciar primer combate
        StartRandomCombat();
    }

    void InitializePlayerData()
    {
        playerCombatData = new PlayerCombatData();
        
        // Resetear cartas a 0
        PlayerCombatData.cards[AffinityType.Fuerza] = 0;
        PlayerCombatData.cards[AffinityType.Agilidad] = 0;
        PlayerCombatData.cards[AffinityType.Destreza] = 0;

        //Añadir 5 cartas iniciales aleatorias

        for (int i = 0; i < 5; i++)
        {
            AffinityType randomType = GetRandomAffinityType();
            PlayerCombatData.cards[randomType]++;
            Debug.Log($"Carta inicial {i + 1}: {randomType}");
        }
        
        // Resetear vida y score
        playerCombatData.playerLife = playerCombatData.playerMaxLife;
        playerCombatData.score = 0;
        
        Debug.Log($"Vida inicializada: {playerCombatData.playerLife}/{playerCombatData.playerMaxLife}");
    }

    /// <summary>
    /// Inicia un combate con un enemigo aleatorio
    /// </summary>
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

    /// <summary>
    /// Inicia un combate
    /// </summary>
    void StartCombat(EnemyData enemyData, EnemyTier tier)
    {
        EnemyTierData tierData = GetTierData(enemyData, tier);

        // Si no existe el tier solicitado, usar el primer tier disponible
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

        //Resetear contador de turnos
        turnsUsedThisCombat = 0;
        currentTurnNumber = 0;

        // Aplicar efectos pre-combate de maldiciones
        if (curseManager != null)
        {
            curseManager.OnPreCombat(currentEnemy);
        }

        Debug.Log($"\nBOSS RUSH: {currentEnemy.enemyData.displayName} ({tierData.enemyTier})");

        // Notificar a la UI
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

    /// <summary>
    /// Seleccionar tipo de ataque (solo en modo PlayerChooses)
    /// </summary>
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

    /// <summary>
    /// NUEVO: Intento de ataque del jugador usando una habilidad específica
    /// </summary>
    public void PlayerAttempt(AbilityData ability)
    {
        if (combatEnded) { NextEnemy(); return; }
        
        currentTurnNumber++;
        
        // 1. FASE PRE-ATAQUE: Aplicar efectos de maldiciones
        if (curseManager != null)
        {
            curseManager.OnTurnStart();
        }
        
        // 2. Verificar si puede usar la habilidad
        if (abilityManager != null && !abilityManager.CanUseAbility(ability, playerCombatData.playerLife, currentEnemy.attemptsRemaining))
        {
            Debug.LogWarning("No puedes usar esta habilidad");
            return;
        }
        
        // 3. Aplicar costos
        playerCombatData.playerLife -= ability.healthCost;
        if (abilityManager != null && ability.cardCost > 0)
        {
            abilityManager.SpendCards(ability.affinityType, ability.cardCost);
        }
        
        // 4. Verificar probabilidad de éxito (si aplica)
        bool success = true;
        if (ability.hasSuccessChance)
        {
            success = UnityEngine.Random.Range(0f, 100f) < ability.successChance;
            
            if (!success)
            {
                // Aplicar penalización por fallo
                playerCombatData.playerLife -= ability.onFailHealthPenalty;
                currentEnemy.attemptsRemaining -= (1 + ability.onFailTurnPenalty);
                OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
                
                Debug.Log($" {ability.abilityName} FALLÓ");
                
                // Verificar si se acabaron los turnos
                if (currentEnemy.attemptsRemaining <= 0)
                {
                    EndCombat(false, playerCombatData.score, 1f);
                }
                return;
            }
        }
        
        // 5. Calcular dados
        int diceCount = CalculateDiceCount(ability);
        int diceMax = ability.diceMaxValue > 0 ? ability.diceMaxValue : 12;
        int roll = RollDice(diceCount, diceMax);
        
        // 6. Calcular bonus de cartas
        AffinityType attackType = ability.affinityType;
        int cardBonus = PlayerCombatData.cards[attackType];
        
        // Aplicar multiplicador de cartas (ej: Corte Profundo)
        if (ability.cardMultiplier != 0)
        {
            cardBonus = Mathf.RoundToInt(cardBonus * ability.cardMultiplier);
        }
        
        // Negar cartas si hay maldición activa
        if (curseManager != null && curseManager.HasNegatedCards())
        {
            cardBonus = -cardBonus;
            Debug.Log("Tus cartas están negadas");
        }
        
        // 7. Calcular multiplicador de afinidad
        float multiplier = GetAffinityMultiplier(attackType);
        multiplier += ability.affinityMultiplierBonus;
        
        // 8. Registrar descubrimiento
        if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
        {
            AffinityDiscoveryTracker.RegisterDiscovery(currentEnemy.enemyData.id, attackType);
        }
        
        // 9. Calcular total según modo
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

        // 11. Evaluar victoria/derrota  (Invertidos para que se vea la resta)
        bool victory = EvaluateAttack(totalFinal, multiplier);
        
        // 10. Notificar UI
        OnAttackResult?.Invoke(roll, cardBonus, totalFinal, multiplier);
        

        
        // 12. Aplicar efectos condicionales
        if (victory && ability.hasOnKillEffect)
        {
            playerCombatData.playerLife += ability.onKillHealthReward;
            Debug.Log($"Recuperaste {ability.onKillHealthReward} HP");
        }
        
        // 13. Consumir turnos (verificar efectos especiales)
        bool consumedTurn = ConsumeTurn(ability);
        
        if (consumedTurn)
        {
            turnsUsedThisCombat++;
        }
    }

    /// <summary>
    /// VERSIÓN LEGACY: Intento de ataque sin habilidad específica (para compatibilidad)
    /// </summary>
    public void PlayerAttempt()
    {
        // Crear habilidad básica temporal según el modo
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
        
        // Aplicar modificador fijo
        baseCount += ability.diceModifier;
        
        // Aplicar multiplicador y suma (ej: Golpes Rápidos)
        if (ability.diceMultiplier != 0)
        {
            baseCount = Mathf.RoundToInt(baseCount * ability.diceMultiplier);
        }
        baseCount += ability.diceAddition;
        
        return Mathf.Max(1, baseCount); // Mínimo 1 dado
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
            // Verificar condición de victoria (puede estar invertida por maldición)
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
        
        // No hubo victoria, reducir intentos
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
        // Verificar si tiene probabilidad de no consumir turno (Golpe Fantasma)
        if (ability.canAvoidTurnConsumption)
        {
            if (UnityEngine.Random.Range(0f, 100f) < ability.avoidTurnChance)
            {
                Debug.Log("👻 No se consumió el turno");
                return false;
            }
            else
            {
                playerCombatData.playerLife -= ability.avoidTurnFailPenalty;
                Debug.Log($"❌ Detectado: -{ability.avoidTurnFailPenalty} HP");
            }
        }
        
        // Consumir los turnos especificados
        currentEnemy.attemptsRemaining -= ability.turnCost;
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
        
        return true;
    }

void EndCombat(bool victory, int finalScore, float lastMultiplier)
{
    combatEnded = true;
    AffinityType rewardCard = default;

    if (victory)
    {
        // NUEVO: Aplicar efectos post-combate de maldiciones
        if (curseManager != null)
        {
            curseManager.OnPostCombat(true);
        }

        // NUEVO: Recuperar cartas si aplica
        if (abilityManager != null)
        {
            abilityManager.OnCombatWon();
        }

        // NUEVO: Verificar desbloqueos
        if (abilityManager != null)
        {
            abilityManager.CheckUnlocks();
        }

        if (combatMode == CombatMode.Passive)
        {
            // Modo Pasivo: siempre da carta aleatoria
            rewardCard = GetRandomAffinityType();
            PlayerCombatData.cards[rewardCard]++;
            OnCombatEnd?.Invoke(victory, finalScore, rewardCard, 0);
        }
        else if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
        {
            
            // Victoria por DEBILIDAD (1.5f o mayor): Elige carta
            if (lastMultiplier >= 1.5f) 
            {
                waitingForCardSelection = true;
                OnWaitingForCardSelection?.Invoke(finalScore);
                Debug.Log("¡Debilidad explotada! Elige tu carta.");
            }
            else 
            {
                // Victoria normal: Probabilidad de carta aleatoria
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

        // NUEVO: Verificar evento de maldición
        Debug.Log("🎴 Verificando evento de maldición...");
        CheckCurseEvent();
    }
    else
    {
        // Verificar si tiene negación de daño
        bool hasShield = curseManager != null && curseManager.HasDamageNegation();
        
        if (hasShield)
        {
            Debug.Log("Daño negado por maldición");
            OnCombatEnd?.Invoke(false, finalScore, default, 0);
        }
        else
        {
            // Derrota: aplicar daño al jugador
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

    void CheckCurseEvent()
    {
        if (curseManager == null)
        {
            Debug.LogWarning("⚠️ CurseManager es NULL");
            return;
        }

        bool isSpirit = currentEnemy.enemyData.isSpirit;
        
        Debug.Log($"🎲 Turnos usados: {turnsUsedThisCombat}, Es espíritu: {isSpirit}");
        
        if (curseManager.ShouldTriggerCurseEvent(turnsUsedThisCombat, isSpirit))
        {
            Debug.Log("✅ ¡Evento de maldición activado!");
            curseManager.TriggerCurseChoiceEvent();
        }
        else
        {
            Debug.Log("❌ No se activó evento de maldición");
        }
    }

    ///<summary>
    /// Game Over si la vida llega a 0
    ///</summary>
    
    public void EndRun()
    {
        int finalScore = playerCombatData.score;
        int finalFuerza = PlayerCombatData.cards[AffinityType.Fuerza];
        int finalAgilidad = PlayerCombatData.cards[AffinityType.Agilidad];
        int finalDestreza = PlayerCombatData.cards[AffinityType.Destreza];

        GameOver?.Invoke(finalScore, finalFuerza, finalAgilidad, finalDestreza, currentEnemy);
    }
    

    /// <summary>
    /// Método público para que la UI confirme la selección de carta
    /// </summary>
    public void SelectRewardCard(AffinityType selectedCard)
    {
        if (!waitingForCardSelection)
        {
            Debug.LogWarning("No hay selección de carta pendiente");
            return;
        }

        // Dar la carta seleccionada
        PlayerCombatData.cards[selectedCard]++;
        
        Debug.Log($"¡Obtienes 1 carta de {selectedCard}! Total: {PlayerCombatData.cards[selectedCard]}");
        
        // Resetear estado de espera
        waitingForCardSelection = false;
        
        // Notificar UI con la carta seleccionada
        OnCombatEnd?.Invoke(true, playerCombatData.score, selectedCard, 0);
    }

    /// <summary>
    /// Obtiene un tipo de afinidad aleatorio
    /// </summary>
    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])System.Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    /// <summary>
    /// Siguiente enemigo (para Boss Rush)
    /// </summary>
    public void NextEnemy()
    {
        StartRandomCombat();
    }

    /// <summary>
    /// Obtener cartas del tipo actual (para UI)
    /// </summary>
    public int GetCurrentCards()
    {
        if (currentEnemy == null) return 0;

        AffinityType type = combatMode == CombatMode.Passive 
            ? currentEnemy.enemyData.affinityType 
            : playerCombatData.selectedAttackType;

        return PlayerCombatData.cards[type];
    }

    /// <summary>
    /// Calcular puntuación por combate según el tier del enemigo y condiciones de victoria
    /// </summary>

    public int CalculateScorePerCombat(float multiplier)
    {int Newscore = 0;

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
            Newscore += 1; // Bonificación por explotar debilidad
        }

        return Newscore;
    }

    /// <summary>
    /// Obtener cartas de un tipo específico (para UI)
    /// </summary>
    public int GetCardsOfType(AffinityType type)
    {
        return PlayerCombatData.cards[type];
    }

        public bool HasActiveEnemy()
    {
        return currentEnemy != null;
    }


    // GETTERS para la UI
    public EnemyInstance GetCurrentEnemy() => currentEnemy;
    public bool IsCombatEnded() => combatEnded;
    public CombatMode GetCombatMode() => combatMode;
    public int GetPlayerLife() => playerCombatData?.playerLife ?? 0;
    public int GetPlayerMaxLife() => playerCombatData?.playerMaxLife ?? 100;
    public int GetCurrentTurnNumber() => currentTurnNumber; // NUEVO
}