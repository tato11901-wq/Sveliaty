using UnityEngine;
using System;

public class CombatManager : MonoBehaviour
{
    [Header("Boss Rush Setup")]
    public EnemyDatabase enemyDatabase;

    [Header("Combat Settings")]
    public CombatMode combatMode = CombatMode.Passive;
    [Range(0, 100)] public float randomCardChance = 30f;

    // Estado del combate
    private EnemyInstance currentEnemy;
    private PlayerCombatData playerCombatData;
    private bool combatEnded = false;
    private bool waitingForCardSelection = false; // Nuevo estado

    // Eventos para la UI
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float> OnAttackResult; // (roll, bonus, total, multiplier)
    public event Action<bool, int, AffinityType, int> OnCombatEnd; // (victory, finalScore, cardReward, lifeLost)
    public event Action<int> OnAttemptsChanged; // (remainingAttempts)
    public event Action<int> OnWaitingForCardSelection; // (finalScore) - Nuevo evento

    void Start()
    {
        InitializePlayerCards();
        StartRandomCombat();
    }

    void InitializePlayerCards()
    {
        playerCombatData = new PlayerCombatData();
        
        // Ejemplo: cartas iniciales (esto vendría de tu sistema de guardado)
        PlayerCombatData.cards[AffinityType.Fuerza] = 0;
        PlayerCombatData.cards[AffinityType.Agilidad] = 0;
        PlayerCombatData.cards[AffinityType.Destreza] = 0;
    }

    /// <summary>
    /// Inicia un combate con un enemigo aleatorio
    /// </summary>
    public void StartRandomCombat()
    {
        if (enemyDatabase == null)
        {
            Debug.LogError("❌ No hay EnemyDatabase asignado");
            return;
        }

        var (randomEnemy, randomTier) = enemyDatabase.GetRandomEnemy();

        if (randomEnemy == null)
        {
            Debug.LogError("❌ No se pudo obtener enemigo aleatorio");
            return;
        }

        StartCombat(randomEnemy, randomTier);
    }

    /// <summary>
    /// Inicia un combate específico
    /// </summary>
    void StartCombat(EnemyData enemyData, EnemyTier tier)
    {
        EnemyTierData tierData = GetTierData(enemyData, tier);

        // Si no existe el tier solicitado, usar el primer tier disponible
        if (tierData == null)
        {
            Debug.LogWarning($"⚠️ Tier {tier} no encontrado para {enemyData.displayName}. Usando tier disponible.");
            
            if (enemyData.enemyTierData != null && enemyData.enemyTierData.Length > 0)
            {
                tierData = enemyData.enemyTierData[0];
                Debug.Log($"→ Usando {tierData.enemyTier} en su lugar");
            }
            else
            {
                Debug.LogError($"❌ {enemyData.displayName} no tiene ningún tier configurado");
                return;
            }
        }

        currentEnemy = new EnemyInstance(enemyData, tierData);
        combatEnded = false;

        Debug.Log($"\n🎮 BOSS RUSH: {currentEnemy.enemyData.displayName} ({tierData.enemyTier})");

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
    /// Cambiar entre modos de combate
    /// </summary>
    public void ToggleCombatMode()
    {
        combatMode = (combatMode == CombatMode.Passive) 
            ? CombatMode.PlayerChooses 
            : CombatMode.Passive;

        Debug.Log($"🔄 Modo cambiado a: {combatMode}");

        // Reiniciar combate con el nuevo modo
        StartRandomCombat();
    }

    /// <summary>
    /// Seleccionar tipo de ataque (solo en modo PlayerChooses)
    /// </summary>
    public void SelectAttackType(AffinityType type)
    {
        if (combatMode != CombatMode.PlayerChooses)
        {
            Debug.LogWarning("⚠️ Solo puedes seleccionar ataque en modo PlayerChooses");
            return;
        }

        playerCombatData.selectedAttackType = type;
        Debug.Log($"🎯 Ataque seleccionado: {type}");
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
    if (combatEnded) { NextEnemy(); return; }

    // Cálculos base
    int roll = RollDice(currentEnemy.enemyTierData.diceCount);
    AffinityType attackType = GetAttackType();
    int cardBonus = PlayerCombatData.cards[attackType];
    float multiplier = GetAffinityMultiplier(attackType);

    if (combatMode == CombatMode.PlayerChooses)
    {
        AffinityDiscoveryTracker.RegisterDiscovery(currentEnemy.enemyData.id, attackType);
    }
    
    
    // Aplicamos el multiplicador a la SUMA de dados y cartas para que siempre importe
    int totalBase = roll + cardBonus;
    int totalFinal = Mathf.RoundToInt(totalBase * multiplier);

    // Logs para debug
    Debug.Log($"Base: {totalBase} ({roll}+{cardBonus}) x Mult: {multiplier} = Total: {totalFinal}");

    // 2. Notificar UI incluyendo el multiplicador
    OnAttackResult?.Invoke(roll, cardBonus, totalFinal, multiplier);


    // Evaluar con el total final
    if (totalFinal >= currentEnemy.enemyTierData.healthThreshold)
    {
        playerCombatData.score += 1; // Incrementar score de enemigos derrotados
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
            rewardCard = GetRandomAffinityType();
            PlayerCombatData.cards[rewardCard]++;
            OnCombatEnd?.Invoke(victory, finalScore, rewardCard, 0);
        }
        else if (combatMode == CombatMode.PlayerChooses)
        {
            // LÓGICA NUEVA:
            // 1.5f es el valor que definiste para AffinityMultiplier.Weak
            if (lastMultiplier >= 1.5f) 
            {
                // Victoria por DEBILIDAD: Elige carta
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
                    // Llamamos a OnCombatEnd con "default" para indicar que no hubo carta
                    OnCombatEnd?.Invoke(true, finalScore, default, 0); 
                }
            }
        }
    }
    else
    {
        // Aplicar daño al jugador
        playerCombatData.playerLife -= currentEnemy.enemyTierData.failureDamage;

        OnCombatEnd?.Invoke(false, finalScore, rewardCard, currentEnemy.enemyTierData.failureDamage);
    }
}
    /// <summary>
    /// Método público para que la UI confirme la selección de carta
    /// </summary>
    public void SelectRewardCard(AffinityType selectedCard)
    {
        if (!waitingForCardSelection)
        {
            Debug.LogWarning("⚠️ No hay selección de carta pendiente");
            return;
        }

        // Dar la carta seleccionada
        PlayerCombatData.cards[selectedCard]++;
        
        Debug.Log($"🎁 ¡Obtienes 1 carta de {selectedCard}! Total: {PlayerCombatData.cards[selectedCard]}");
        
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
    /// Obtener cartas de un tipo específico (para UI)
    /// </summary>
    public int GetCardsOfType(AffinityType type)
    {
        return PlayerCombatData.cards[type];
    }

    // GETTERS para la UI
    public EnemyInstance GetCurrentEnemy() => currentEnemy;
    public bool IsCombatEnded() => combatEnded;
    public CombatMode GetCombatMode() => combatMode;
}