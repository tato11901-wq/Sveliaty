using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador de la UI de Game Over
/// Muestra estadísticas finales y el enemigo que derrotó al jugador
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Referencias de Textos")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI fuerzaCardsText;
    public TextMeshProUGUI agilidadCardsText;
    public TextMeshProUGUI destrezaCardsText;
    public TextMeshProUGUI defeatedByText;

    [Header("Referencias de Imagen")]
    public Image defeatedBySprite;

    [Header("Botones")]
    public Button finishButton;

    void Start()
    {
        // Configurar botón
        if (finishButton != null)
        {
            finishButton.onClick.AddListener(OnFinishButtonClicked);
        }
    }

    /// <summary>
    /// Muestra la pantalla de Game Over con las estadísticas finales
    /// </summary>
    public void ShowGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log($"💀 Mostrando Game Over - Score: {finalScore}");

        // Actualizar textos de estadísticas
        if (scoreText != null)
        {
            scoreText.text = $"Puntuación Final: {finalScore}";
        }

        if (fuerzaCardsText != null)
        {
            fuerzaCardsText.text = $"Fuerza: {fuerzaCards}";
        }

        if (agilidadCardsText != null)
        {
            agilidadCardsText.text = $"Agilidad: {agilidadCards}";
        }

        if (destrezaCardsText != null)
        {
            destrezaCardsText.text = $"Destreza: {destrezaCards}";
        }

        // Mostrar enemigo que te derrotó
        if (defeatedBy != null)
        {
            if (defeatedByText != null)
            {
                defeatedByText.text = $"Fuiste derrotado por:\n{defeatedBy.enemyData.displayName}";
            }

            if (defeatedBySprite != null && defeatedBy.enemyTierData.sprite != null)
            {
                defeatedBySprite.sprite = defeatedBy.enemyTierData.sprite;
                defeatedBySprite.enabled = true;
            }
        }
        else
        {
            if (defeatedByText != null)
            {
                defeatedByText.text = "Fuiste derrotado";
            }

            if (defeatedBySprite != null)
            {
                defeatedBySprite.enabled = false;
            }
        }
    }

    /// <summary>
    /// Maneja el clic en el botón "Finalizar Partida"
    /// </summary>
    void OnFinishButtonClicked()
    {
        Debug.Log("Finalizando partida, volviendo al menú");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null");
        }
    }

    /// <summary>
    /// Opcional: Añadir efectos al activar el panel
    /// </summary>
    void OnEnable()
    {
        // Aquí puedes añadir animaciones, sonidos, etc.
    }
}