using UnityEngine;
using System;
using UnityEngine.SocialPlatforms.Impl;

public class CombatManager : MonoBehaviour
{
    [Header("Boss Rush Setup")]
    public EnemyDatabase enemyDatabase;

    [Header("Combat Settings")]
    private CombatMode combatMode = CombatMode.Passive;
    [Range(0, 100)] public float randomCardChance = 30f;

    // Estado del combate
    private EnemyInstance currentEnemy;
    private PlayerCombatData playerCombatData;
    private bool combatEnded = false;
    private bool waitingForCardSelection = false;

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
    /// Intento de ataque del jugador
    /// </summary>
public void PlayerAttempt()
{
    int totalBase = 0;
    int totalFinal = 0;
    if (combatEnded) { NextEnemy(); return; }

    // Cálculos base
    int roll;

    if (combatMode == CombatMode.TraditionalRPG)
    {
        roll = RollDice(currentEnemy.currentRPGDiceCount);
    }
    else
    {
        roll = RollDice(currentEnemy.enemyTierData.diceCount);
    }
    AffinityType attackType = GetAttackType();
    int cardBonus = PlayerCombatData.cards[attackType];
    float multiplier = GetAffinityMultiplier(attackType);

    if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.TraditionalRPG)
    {
        AffinityDiscoveryTracker.RegisterDiscovery(currentEnemy.enemyData.id, attackType);
    }
    
    
    if(combatMode == CombatMode.TraditionalRPG)
        {
            // En Traditional RPG, el multiplicador se aplica a la suma total
        totalBase = roll + cardBonus;
        totalFinal = Mathf.RoundToInt(totalBase * multiplier);
        }
    
    if (combatMode == CombatMode.Passive)
    {
        totalBase = roll + cardBonus;
        totalFinal = totalBase; // Sin multiplicador en modo Pasivo
    }

    if (combatMode == CombatMode.PlayerChooses)
    {
        // En Player Chooses, el multiplicador se aplica SOLO al bonus de carta
        totalBase = roll;
        totalFinal = Mathf.RoundToInt(totalBase + (cardBonus * multiplier));
    }


    // Logs para debug
    Debug.Log($"Base: {totalBase} ({roll}+{cardBonus}) x Mult: {multiplier} = Total: {totalFinal}");

    // 2. Notificar UI incluyendo el multiplicador
    OnAttackResult?.Invoke(roll, cardBonus, totalFinal, multiplier);


    // Evaluar con el total final para modo Player Chooses y Modo Pasivo
    if (combatMode == CombatMode.PlayerChooses || combatMode == CombatMode.Passive)
    {
    if (totalFinal >= currentEnemy.enemyTierData.healthThreshold)
    {
        playerCombatData.score += CalculateScorePerCombat(multiplier); // Incrementar score de enemigos derrotados
        EndCombat(true, playerCombatData.score, multiplier);
    }
    else
    {
        currentEnemy.attemptsRemaining--;
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);

        if (currentEnemy.attemptsRemaining <= 0)
        {
            EndCombat(false, playerCombatData.score, multiplier);
        }
    }}
    else if (combatMode == CombatMode.TraditionalRPG)
    {
        // 1. Restar vida primero
        currentEnemy.currentRPGHealth -= totalFinal;
        Debug.Log($"Vida restante del enemigo: {currentEnemy.currentRPGHealth}");
        
        // 2. AHORA notificar a la UI (mover aquí específicamente para Traditional RPG)
        OnAttackResult?.Invoke(roll, cardBonus, totalFinal, multiplier);

        if (currentEnemy.currentRPGHealth <= 0)
        {
            playerCombatData.score += CalculateScorePerCombat(multiplier);
            EndCombat(true, playerCombatData.score, multiplier);
            return;
        }

        currentEnemy.RPGattemptsRemaining--;
        OnAttemptsChanged?.Invoke(currentEnemy.RPGattemptsRemaining);

        if (currentEnemy.RPGattemptsRemaining <= 0)
        {
            EndCombat(false, playerCombatData.score, multiplier);
        }
}

    int RollDice(int diceCount)
    {
 
    
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += UnityEngine.Random.Range(1, 13);
        }
        return total;
      
}
    

void EndCombat(bool victory, int finalScore, float lastMultiplier)
{
    combatEnded = true;
    AffinityType rewardCard = default;

    if (victory)
    {
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
                // Victoria normal: Probabilidad de carta aleatoria (50%)
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
}