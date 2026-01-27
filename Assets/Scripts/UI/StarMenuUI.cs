using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador de la UI del menú de inicio
/// Permite seleccionar el modo de juego
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("Referencias")]
    public Button passiveModeButton;
    public Button playerChoosesModeButton;
    public TextMeshProUGUI titleText;

    [Header("Textos Opcionales")]
    public TextMeshProUGUI passiveModeDescription;
    public TextMeshProUGUI playerChoosesModeDescription;

    void Start()
    {
        // Configurar botones
        if (passiveModeButton != null)
        {
            passiveModeButton.onClick.AddListener(() => StartGame(CombatMode.Passive));
        }

        if (playerChoosesModeButton != null)
        {
            playerChoosesModeButton.onClick.AddListener(() => StartGame(CombatMode.PlayerChooses));
        }

        // Configurar textos descriptivos (opcional)
        SetupDescriptions();
    }

    void SetupDescriptions()
    {
        if (passiveModeDescription != null)
        {
            passiveModeDescription.text = "Modo automático\nSuma cartas del tipo del enemigo\nIdeal para principiantes";
        }

        if (playerChoosesModeDescription != null)
        {
            playerChoosesModeDescription.text = "Modo avanzado\nElige tu tipo de ataque\nExplota las debilidades para mejores recompensas";
        }
    }

    /// <summary>
    /// Inicia el juego con el modo seleccionado
    /// </summary>
    void StartGame(CombatMode mode)
    {
        Debug.Log($"Modo seleccionado: {mode}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun(mode);
        }
        else
        {
            Debug.LogError("GameManager.Instance es null");
        }
    }

    /// <summary>
    /// Actualizar UI cuando el panel se activa
    /// </summary>
    void OnEnable()
    {
        // Puedes añadir animaciones o efectos aquí
        if (titleText != null)
        {
            titleText.text = "Sveliaty";
        }
    }
}