using UnityEngine;

/// <summary>
/// Controlador principal del flujo del juego
/// Gestiona: Menú Inicio -> Combate -> Game Over -> Menú Inicio
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de Paneles")]
    public GameObject startMenuPanel;
    public GameObject combatPanel;
    public GameObject gameOverPanel;

    [Header("Referencias de Managers")]
    public CombatManager combatManager;
    public StartMenuUI startMenuUI;
    public GameOverUI gameOverUI;

    [Header("Estado del Juego")]
    private CombatMode selectedMode;
    private bool gameInProgress = false;

    void Awake()
    {
        // Singleton pattern
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
        // Suscribirse al evento de Game Over del CombatManager
        if (combatManager != null)
        {
            combatManager.GameOver += HandleGameOver;
        }

        // Iniciar en el menú de inicio
        ShowStartMenu();
    }

    void OnDestroy()
    {
        // Desuscribirse del evento
        if (combatManager != null)
        {
            combatManager.GameOver -= HandleGameOver;
        }
    }

    /// <summary>
    /// Muestra el menú de inicio
    /// </summary>
    public void ShowStartMenu()
    {
        Debug.Log("Mostrando menú de inicio");
        
        startMenuPanel.SetActive(true);
        combatPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        gameInProgress = false;
        
    }

    /// <summary>
    /// Inicia una nueva run con el modo seleccionado
    /// </summary>
    public void StartNewRun(CombatMode mode)
    {
        Debug.Log($"Iniciando nueva run en modo: {mode}");
        
        selectedMode = mode;
        gameInProgress = true;

        // Ocultar menú de inicio
        startMenuPanel.SetActive(false);

        //Generar primer enemigo

        
        // Mostrar panel de combate
        combatPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        // Inicializar el combate con el modo seleccionado
        combatManager.StartNewRun(mode);
    }

    /// <summary>
    /// Maneja el evento de Game Over
    /// </summary>
    void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log($"GAME OVER - Score: {finalScore}");
        
        gameInProgress = false;

        // Ocultar panel de combate
        combatPanel.SetActive(false);
        
        // Mostrar panel de Game Over
        gameOverPanel.SetActive(true);

        // Pasar datos al GameOverUI
        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver(finalScore, fuerzaCards, agilidadCards, destrezaCards, defeatedBy);
        }
    }

    /// <summary>
    /// Reiniciar el juego
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Reiniciando juego");
        ShowStartMenu();
    }

    // GETTERS
    public bool IsGameInProgress() => gameInProgress;
    public CombatMode GetSelectedMode() => selectedMode;
}