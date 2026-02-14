using UnityEngine;
using System;

/// <summary>
/// Gestor del modo Boss Rush
/// Responsabilidades:
/// - Iniciar y terminar runs
/// - Seleccionar enemigos aleatorios
/// - Gestionar el flujo entre combates
/// - Llevar estadisticas de la run
/// </summary>
public class BossRushManager : MonoBehaviour
{
    [Header("References")]
    public CombatManager combatManager;
    public PlayerManager playerManager;
    public EnemyDatabase enemyDatabase;

    [Header("Boss Rush Settings")]
    public CombatMode defaultMode = CombatMode.PlayerChooses;
    
    [Header("Run Statistics")]
    private int enemiesDefeatedThisRun = 0;
    private int totalTurnsUsed = 0;
    private bool runInProgress = false;
    
    // Eventos
    public event Action<CombatMode> OnRunStarted;
    public event Action<int, int> OnRunEnded; // (finalScore, enemiesDefeated)

    void Start()
    {
        // Suscribirse a eventos del CombatManager
        if (combatManager != null)
        {
            combatManager.OnCombatEnd += HandleCombatEnd;
            combatManager.GameOver += HandleGameOver;
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatEnd -= HandleCombatEnd;
            combatManager.GameOver -= HandleGameOver;
        }
    }

    /// <summary>
    /// Inicia una nueva Boss Rush run
    /// </summary>
    public void StartNewRun(CombatMode mode)
    {
        Debug.Log("BossRushManager: Iniciando nueva run en modo " + mode);
        
        runInProgress = true;
        defaultMode = mode;
        
        // Resetear estadisticas
        enemiesDefeatedThisRun = 0;
        totalTurnsUsed = 0;
        
        // Inicializar jugador
        if (playerManager != null)
        {
            playerManager.InitializeForNewRun(5);
        }
        else
        {
            Debug.LogError("PlayerManager no asignado en BossRushManager");
        }
        
        // Notificar inicio
        OnRunStarted?.Invoke(mode);
        
        // Iniciar primer combate
        StartNextCombat();
    }

    /// <summary>
    /// Inicia el siguiente combate con un enemigo aleatorio
    /// </summary>
    public void StartNextCombat()
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso");
            return;
        }

        if (enemyDatabase == null)
        {
            Debug.LogError("EnemyDatabase no asignado en BossRushManager");
            return;
        }

        // Obtener enemigo aleatorio
        var (randomEnemy, randomTier) = enemyDatabase.GetRandomEnemy();

        if (randomEnemy == null)
        {
            Debug.LogError("No se pudo obtener enemigo aleatorio");
            return;
        }

        // Iniciar combate en CombatManager
        if (combatManager != null)
        {
            combatManager.StartCombat(randomEnemy, randomTier, defaultMode);
        }
        else
        {
            Debug.LogError("CombatManager no asignado en BossRushManager");
        }
    }

    /// <summary>
    /// Maneja el evento de fin de combate
    /// </summary>
    void HandleCombatEnd(bool victory, int currentScore, AffinityType rewardCard, int lifeLost)
    {
        if (!runInProgress) return;

        if (victory)
        {
            enemiesDefeatedThisRun++;
            Debug.Log("Enemigos derrotados en esta run: " + enemiesDefeatedThisRun);
            
            // El flujo continuará desde la UI (puede haber maldiciones, etc.)
            // La UI llamará a ContinueToNextCombat() cuando esté lista
        }
        else
        {
            // Derrota pero el jugador sigue vivo
            // El flujo continuará desde la UI
        }
    }

    /// <summary>
    /// Continua al siguiente combate (llamado desde UI después de eventos post-combate)
    /// </summary>
    public void ContinueToNextCombat()
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso");
            return;
        }

        if (!playerManager.IsAlive())
        {
            Debug.LogWarning("El jugador esta muerto, no se puede continuar");
            return;
        }

        Debug.Log("BossRushManager: Continuando al siguiente combate");
        StartNextCombat();
    }

    /// <summary>
    /// Maneja el evento de Game Over
    /// </summary>
    void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log("BossRushManager: Game Over - Score: " + finalScore);
        EndRun(finalScore);
    }

    /// <summary>
    /// Termina la run actual
    /// </summary>
    public void EndRun(int finalScore)
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso para terminar");
            return;
        }

        Debug.Log("BossRushManager: Run terminada");
        Debug.Log("Enemigos derrotados: " + enemiesDefeatedThisRun);
        Debug.Log("Score final: " + finalScore);
        
        runInProgress = false;
        
        // Notificar fin de run
        OnRunEnded?.Invoke(finalScore, enemiesDefeatedThisRun);
    }

    /// <summary>
    /// Fuerza el fin de la run (para debugging o quit)
    /// </summary>
    public void ForceEndRun()
    {
        if (runInProgress)
        {
            int currentScore = playerManager != null ? playerManager.GetScore() : 0;
            EndRun(currentScore);
        }
    }

    // GETTERS
    public bool IsRunInProgress() => runInProgress;
    public int GetEnemiesDefeatedThisRun() => enemiesDefeatedThisRun;
    public CombatMode GetCurrentMode() => defaultMode;
}