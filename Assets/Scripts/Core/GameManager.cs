using UnityEngine;

/// <summary>
/// Controlador principal del flujo del juego
/// Gestiona: Menu Inicio -> Modo de Juego -> Game Over -> Menu Inicio
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de Paneles")]
    public GameObject startMenuPanel;
    public GameObject gameplayPanel; // Panel que contiene toda la UI de juego
    public GameObject gameOverPanel;

    [Header("Referencias de Managers")]
    public BossRushManager bossRushManager; // NUEVO: Ahora usa BossRushManager
    public CombatManager combatManager;
    public StartMenuUI startMenuUI;
    public GameOverUI gameOverUI;

    [Header("Estado del Juego")]
    private bool gameInProgress = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Suscribirse a eventos
        if (combatManager != null)
        {
            combatManager.GameOver += HandleGameOver;
        }

        if (bossRushManager != null)
        {
            bossRushManager.OnRunStarted += HandleRunStarted;
            bossRushManager.OnRunEnded += HandleRunEnded;
        }

        ShowStartMenu();
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.GameOver -= HandleGameOver;
        }

        if (bossRushManager != null)
        {
            bossRushManager.OnRunStarted -= HandleRunStarted;
            bossRushManager.OnRunEnded -= HandleRunEnded;
        }
    }

    public void ShowStartMenu()
    {
        Debug.Log("Mostrando menu de inicio");
        
        startMenuPanel.SetActive(true);
        gameplayPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        gameInProgress = false;
    }

    /// <summary>
    /// Inicia una nueva run (ahora delega a BossRushManager)
    /// </summary>
    public void StartNewRun(CombatMode mode)
    {
        Debug.Log("GameManager: Iniciando nueva run en modo " + mode);
        
        gameInProgress = true;

        startMenuPanel.SetActive(false);
        gameplayPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        // NUEVO: Delegar a BossRushManager
        if (bossRushManager != null)
        {
            bossRushManager.StartNewRun(mode);
        }
        else
        {
            Debug.LogError("BossRushManager no asignado");
        }
    }

    void HandleRunStarted(CombatMode mode)
    {
        Debug.Log("Run iniciada en modo " + mode);
    }

    void HandleRunEnded(int finalScore, int enemiesDefeated)
    {
        Debug.Log("Run terminada - Score: " + finalScore + ", Enemigos: " + enemiesDefeated);
    }

    void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log("GAME OVER - Score: " + finalScore);
        
        gameInProgress = false;

        gameplayPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver(finalScore, fuerzaCards, agilidadCards, destrezaCards, defeatedBy);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Reiniciando juego");
        ShowStartMenu();
    }

    // GETTERS
    public bool IsGameInProgress() => gameInProgress;
}