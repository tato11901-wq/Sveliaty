using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor centralizado de todos los datos y estado del jugador
/// Maneja: vida, cartas, score, estadisticas
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Vida del Jugador")]
    [SerializeField] private int maxLife = 100;
    private int currentLife;

    [Header("Inventario de Cartas")]
    private Dictionary<AffinityType, int> cards = new Dictionary<AffinityType, int>()
    {
        { AffinityType.Fuerza, 0 },
        { AffinityType.Agilidad, 0 },
        { AffinityType.Destreza, 0 }
    };

    [Header("Puntuacion")]
    private int score = 0;

    [Header("Estadisticas")]
    private int enemiesDefeated = 0;
    private int combatsWon = 0;
    private int combatsLost = 0;

    // Eventos
    public event Action<int, int> OnHealthChanged; // (currentLife, maxLife)
    public event Action<AffinityType, int> OnCardsChanged; // (type, newAmount)
    public event Action<int> OnScoreChanged; // (newScore)
    public event Action OnPlayerDeath;

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

    /// <summary>
    /// Inicializa el jugador para una nueva run
    /// </summary>
    public void InitializeForNewRun(int initialCards = 5)
    {
        // Resetear vida
        currentLife = maxLife;

        // Resetear cartas
        cards[AffinityType.Fuerza] = 0;
        cards[AffinityType.Agilidad] = 0;
        cards[AffinityType.Destreza] = 0;

        // Dar cartas iniciales aleatorias
        for (int i = 0; i < initialCards; i++)
        {
            AffinityType randomType = GetRandomAffinityType();
            AddCards(randomType, 1);
            Debug.Log("Carta inicial " + (i + 1) + ": " + randomType);
        }

        // Resetear puntuacion
        score = 0;

        // Resetear estadisticas
        enemiesDefeated = 0;
        combatsWon = 0;
        combatsLost = 0;

        // Notificar cambios
        OnHealthChanged?.Invoke(currentLife, maxLife);
        OnScoreChanged?.Invoke(score);

        Debug.Log("PlayerManager inicializado - Vida: " + currentLife + "/" + maxLife);
    }

    // ========== GESTION DE VIDA ==========

    /// <summary>
    /// Modifica la vida del jugador
    /// </summary>
    public void ModifyHealth(int amount)
    {
        int previousLife = currentLife;
        currentLife += amount;
        currentLife = Mathf.Clamp(currentLife, 0, maxLife);

        Debug.Log("Vida modificada: " + amount + " (de " + previousLife + " a " + currentLife + ")");

        OnHealthChanged?.Invoke(currentLife, maxLife);

        if (currentLife <= 0)
        {
            Debug.Log("Jugador muerto");
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// Establece la vida del jugador directamente
    /// </summary>
    public void SetHealth(int value)
    {
        currentLife = Mathf.Clamp(value, 0, maxLife);
        OnHealthChanged?.Invoke(currentLife, maxLife);

        if (currentLife <= 0)
        {
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// Cura al jugador completamente
    /// </summary>
    public void HealToFull()
    {
        ModifyHealth(maxLife);
    }

    // ========== GESTION DE CARTAS ==========

    /// <summary>
    /// Añade cartas de un tipo
    /// </summary>
    public void AddCards(AffinityType type, int amount)
    {
        if (amount <= 0) return;

        cards[type] += amount;
        Debug.Log("+" + amount + " carta(s) de " + type + ". Total: " + cards[type]);
        OnCardsChanged?.Invoke(type, cards[type]);
    }

    /// <summary>
    /// Quita cartas de un tipo
    /// </summary>
    public bool RemoveCards(AffinityType type, int amount)
    {
        if (amount <= 0) return true;
        if (cards[type] < amount) return false;

        cards[type] -= amount;
        Debug.Log("-" + amount + " carta(s) de " + type + ". Total: " + cards[type]);
        OnCardsChanged?.Invoke(type, cards[type]);
        return true;
    }

    /// <summary>
    /// Obtiene la cantidad de cartas de un tipo
    /// </summary>
    public int GetCards(AffinityType type)
    {
        return cards[type];
    }

    /// <summary>
    /// Obtiene todas las cartas
    /// </summary>
    public Dictionary<AffinityType, int> GetAllCards()
    {
        return new Dictionary<AffinityType, int>(cards);
    }

    /// <summary>
    /// Verifica si tiene suficientes cartas
    /// </summary>
    public bool HasCards(AffinityType type, int amount)
    {
        return cards[type] >= amount;
    }

    /// <summary>
    /// Quita una carta aleatoria (para maldiciones)
    /// </summary>
    public bool RemoveRandomCard()
    {
        // Obtener tipos con cartas disponibles
        List<AffinityType> availableTypes = new List<AffinityType>();
        foreach (AffinityType type in Enum.GetValues(typeof(AffinityType)))
        {
            if (cards[type] > 0)
                availableTypes.Add(type);
        }

        if (availableTypes.Count == 0)
        {
            Debug.Log("No hay cartas para quitar");
            return false;
        }

        AffinityType randomType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
        RemoveCards(randomType, 1);
        return true;
    }

    // ========== GESTION DE PUNTUACION ==========

    /// <summary>
    /// Añade puntos al score
    /// </summary>
    public void AddScore(int points)
    {
        if (points <= 0) return;

        score += points;
        Debug.Log("+" + points + " puntos. Score total: " + score);
        OnScoreChanged?.Invoke(score);
    }

    /// <summary>
    /// Establece el score directamente
    /// </summary>
    public void SetScore(int value)
    {
        score = Mathf.Max(0, value);
        OnScoreChanged?.Invoke(score);
    }

    // ========== ESTADISTICAS ==========

    /// <summary>
    /// Registra un enemigo derrotado
    /// </summary>
    public void RegisterEnemyDefeated()
    {
        enemiesDefeated++;
        Debug.Log("Enemigos derrotados: " + enemiesDefeated);
    }

    /// <summary>
    /// Registra un combate ganado
    /// </summary>
    public void RegisterCombatWon()
    {
        combatsWon++;
    }

    /// <summary>
    /// Registra un combate perdido
    /// </summary>
    public void RegisterCombatLost()
    {
        combatsLost++;
    }

    // ========== GETTERS ==========

    public int GetCurrentLife() => currentLife;
    public int GetMaxLife() => maxLife;
    public int GetScore() => score;
    public int GetEnemiesDefeated() => enemiesDefeated;
    public int GetCombatsWon() => combatsWon;
    public int GetCombatsLost() => combatsLost;

    public bool IsAlive() => currentLife > 0;

    // ========== HELPERS ==========

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    /// <summary>
    /// Debug: Muestra el estado actual del jugador
    /// </summary>
    public void DebugPrintState()
    {
        Debug.Log("=== ESTADO DEL JUGADOR ===");
        Debug.Log("Vida: " + currentLife + "/" + maxLife);
        Debug.Log("Score: " + score);
        Debug.Log("Cartas - Fuerza: " + cards[AffinityType.Fuerza]);
        Debug.Log("Cartas - Agilidad: " + cards[AffinityType.Agilidad]);
        Debug.Log("Cartas - Destreza: " + cards[AffinityType.Destreza]);
        Debug.Log("Enemigos derrotados: " + enemiesDefeated);
        Debug.Log("========================");
    }
}