using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIManager : MonoBehaviour
{
    [Header("Referencias")]
    public CombatManager combatManager;

    [Header("Enemy Display")]
    public Image enemySprite;
    public TextMeshProUGUI enemyNameText;

    [Header("Stats Display")]
    public TextMeshProUGUI vidaText;
    public TextMeshProUGUI escaladoText;
    public TextMeshProUGUI intentosText;
    public TextMeshProUGUI dadosText;
    public TextMeshProUGUI vidaActualText; // NUEVO: Muestra vida actual/máxima

    [Header("Buttons - Modo Passive")]
    public GameObject passiveModePanel;
    public Button attackButton;
    public TextMeshProUGUI cartasText;

    [Header("Buttons - Modo PlayerChooses")]
    public GameObject playerChoosePanel;
    public Button fuerzaButton;
    public Button agilidadButton;
    public Button destrezaButton;
    public TextMeshProUGUI fuerzaCartasText;
    public TextMeshProUGUI agilidadCartasText;
    public TextMeshProUGUI destrezaCartasText;
    public Color colorDesconocido = Color.gray;
    public Color colorDebilidad = Color.green;   // Weak
    public Color colorResistencia = Color.red;    // Strong
    public Color colorInmunidad = Color.black;    // Immune
    public Color colorNeutral = Color.white;     // Neutral

    [Header("General Buttons")]
    // Botón de cambio de modo ELIMINADO - ahora se selecciona al inicio

    [Header("Results Display")]
    public TextMeshProUGUI resultadoDadosText;
    public TextMeshProUGUI resultadoAtaqueText;

    [Header("Victory/Defeat Panel - PASSIVE MODE")]
    public GameObject passiveEndPanel;
    public TextMeshProUGUI passiveEndMessageText;
    public Button passiveNextEnemyButton;

    [Header("Victory Panel - PLAYERCHOOSES MODE")]
    public GameObject playerChoosesVictoryPanel;
    public TextMeshProUGUI playerChoosesVictoryText;
    public Button selectFuerzaButton;
    public Button selectAgilidadButton;
    public Button selectDestrezaButton;

    [Header("Defeat Panel - BOTH MODES")]
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatMessageText;
    public Button defeatNextEnemyButton;

void Start()
{
    // Configurar botones (solo una vez)
    attackButton.onClick.AddListener(() => combatManager.PlayerAttempt());

    fuerzaButton.onClick.AddListener(() =>
    {
        combatManager.SelectAttackType(AffinityType.Fuerza);
        combatManager.PlayerAttempt();
        UpdateAffinitiesUI();
    });

    agilidadButton.onClick.AddListener(() =>
    {
        combatManager.SelectAttackType(AffinityType.Agilidad);
        combatManager.PlayerAttempt();
        UpdateAffinitiesUI();
    });

    destrezaButton.onClick.AddListener(() =>
    {
        combatManager.SelectAttackType(AffinityType.Destreza);
        combatManager.PlayerAttempt();
        UpdateAffinitiesUI();
    });

    passiveNextEnemyButton.onClick.AddListener(() =>
    {
        passiveEndPanel.SetActive(false);
        combatManager.NextEnemy();
    });

    defeatNextEnemyButton.onClick.AddListener(() =>
    {
        defeatPanel.SetActive(false);
        combatManager.NextEnemy();
    });

    selectFuerzaButton.onClick.AddListener(() => SelectCard(AffinityType.Fuerza));
    selectAgilidadButton.onClick.AddListener(() => SelectCard(AffinityType.Agilidad));
    selectDestrezaButton.onClick.AddListener(() => SelectCard(AffinityType.Destreza));
}

private void OnEnable()
{
    if (combatManager == null) return;

    combatManager.OnCombatStart += HandleCombatStart;
    combatManager.OnAttackResult += HandleAttackResult;
    combatManager.OnCombatEnd += HandleCombatEnd;
    combatManager.OnAttemptsChanged += HandleAttemptsChanged;
    combatManager.OnWaitingForCardSelection += HandleWaitingForCardSelection;

    // Sincronización visual
    passiveEndPanel.SetActive(false);
    playerChoosesVictoryPanel.SetActive(false);
    defeatPanel.SetActive(false);

    // Sincronizar combate si ya existe
    if (combatManager.HasActiveEnemy())
    {
        HandleCombatStart(combatManager.GetCurrentEnemy());
    }
}

private void OnDisable()
{
    if (combatManager == null) return;

    combatManager.OnCombatStart -= HandleCombatStart;
    combatManager.OnAttackResult -= HandleAttackResult;
    combatManager.OnCombatEnd -= HandleCombatEnd;
    combatManager.OnAttemptsChanged -= HandleAttemptsChanged;
    combatManager.OnWaitingForCardSelection -= HandleWaitingForCardSelection;
}
    void OnDestroy()
    {
        // Desuscribirse
        if (combatManager != null)
        {
            combatManager.OnCombatStart -= HandleCombatStart;
            combatManager.OnAttackResult -= HandleAttackResult;
            combatManager.OnCombatEnd -= HandleCombatEnd;
            combatManager.OnAttemptsChanged -= HandleAttemptsChanged;
            combatManager.OnWaitingForCardSelection -= HandleWaitingForCardSelection;
        }
    }

    void HandleCombatStart(EnemyInstance enemy)
    {
        // Actualizar sprite y nombre
        enemySprite.sprite = enemy.enemyTierData.sprite;
        enemyNameText.text = enemy.enemyData.displayName;

        // Actualizar stats
        vidaText.text = enemy.enemyTierData.healthThreshold.ToString();
        escaladoText.text = GetEscaladoText(enemy);
        intentosText.text = enemy.attemptsRemaining.ToString();
        dadosText.text = enemy.enemyTierData.diceCount.ToString();

        UpdateModeUI();


        // Actualizar vida del jugador
        UpdatePlayerLifeUI();

        // Limpiar resultados
        resultadoDadosText.text = "-";
        resultadoAtaqueText.text = "-";

        // Actualizar cartas
        UpdateCardsDisplay();
        // Actualizar UI de afinidades
        UpdateAffinitiesUI();
    }

    string GetEscaladoText(EnemyInstance enemy)
    {
        if (combatManager.GetCombatMode() == CombatMode.PlayerChooses)
        {
            return "???"; // No revelar debilidad en modo PlayerChooses
        }
        else
        {
            return enemy.enemyData.affinityType.ToString();
        }
    }

void HandleAttackResult(int roll, int bonus, int total, float multiplier)
{
    resultadoDadosText.text = roll.ToString();
    
    // Mostramos el total con color según efectividad
    string colorTag = multiplier > 1.1f ? "<color=green>" : (multiplier < 0.9f ? "<color=red>" : "<color=white>");
    string multText = multiplier != 1f ? $" ({colorTag}x{multiplier}</color>)" : "";
    
    resultadoAtaqueText.text = $"{total}{multText}";
    
    // Opcional: Feedback visual rápido en el texto de cartas o un popup
    UpdateAffinitiesUI();
}

    void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
{
    // Actualizar vida del jugador
    UpdatePlayerLifeUI();

    if (victory)
    {
        passiveEndPanel.SetActive(true);
        
        // Si rewardCard es el default del Enum (usualmente el primer valor), 
        // podrías chequear si realmente se entregó una. 
        // Una forma limpia es ver si el manager notificó una carta válida.
        
        if (rewardCard == default && combatManager.GetCombatMode() == CombatMode.PlayerChooses)
        {
             passiveEndMessageText.text = $"🎉 ¡VICTORIA!\n\nPuntuación: {finalScore}\n\nNo explotaste la debilidad y no hubo suerte con el botín.";
        }
        else
        {
            passiveEndMessageText.text = $"🎉 ¡VICTORIA!\n\nPuntuación: {finalScore}\n\nObtienes 1 carta de:\n{rewardCard}";
        }
    }
    else
    {
        defeatPanel.SetActive(true);
        defeatMessageText.text = $"DERROTA\n\nPuntuación: {finalScore}";

        if (lifeLost > 0)
        {
            
            defeatMessageText.text += $"\n\nPerdiste {lifeLost} vida.";
        }
    }
    
    UpdateCardsDisplay();
}

    void HandleWaitingForCardSelection(int finalScore)
    {
        // Mostrar panel de selección de carta en modo PLAYERCHOOSES
        playerChoosesVictoryPanel.SetActive(true);
        playerChoosesVictoryText.text = $"🎉 ¡VICTORIA!\n\nPuntuación: {finalScore}\n\n🎁 Elige tu recompensa:";
    }

    void SelectCard(AffinityType selectedType)
    {
        // Notificar al CombatManager la carta seleccionada
        combatManager.SelectRewardCard(selectedType);
        
        // Ocultar panel de selección
        playerChoosesVictoryPanel.SetActive(false);
        
        // Mostrar panel de victoria con la carta seleccionada
        passiveEndPanel.SetActive(true);
        passiveEndMessageText.text = $"🎉 ¡VICTORIA!\n\n🎁 Obtuviste 1 carta de:\n{selectedType}";
        
        // Actualizar display de cartas
        UpdateCardsDisplay();
    }

    void HandleAttemptsChanged(int remainingAttempts)
    {
        intentosText.text = remainingAttempts.ToString();
    }

    void UpdateModeUI()
    {
        CombatMode mode = combatManager.GetCombatMode();

        if (mode == CombatMode.Passive)
        {
            passiveModePanel.SetActive(true);
            playerChoosePanel.SetActive(false);
        }
        else
        {
            passiveModePanel.SetActive(false);
            playerChoosePanel.SetActive(true);
        }

        UpdateCardsDisplay();
        UpdateAffinitiesUI();
        UpdatePlayerLifeUI();
    }

    void UpdateCardsDisplay()
    {
        if (combatManager.GetCombatMode() == CombatMode.Passive)
        {
            int currentCards = combatManager.GetCurrentCards();
            cartasText.text = $"Tienes: {currentCards} Cartas";
        }
        else
        {
            fuerzaCartasText.text = $"ATACAR CON FUERZA Tienes: {combatManager.GetCardsOfType(AffinityType.Fuerza)} Cartas";
            agilidadCartasText.text = $"ATACAR CON AGILIDAD Tienes: {combatManager.GetCardsOfType(AffinityType.Agilidad)} Cartas";
            destrezaCartasText.text = $"ATACAR CON DESTREZA Tienes: {combatManager.GetCardsOfType(AffinityType.Destreza)} Cartas";
        }
    }

    void UpdateButtonColor(Button button, AffinityType type, EnemyData enemy)
{
    Image buttonImage = button.GetComponent<Image>();
    if (buttonImage == null) return;

    // Si no ha sido descubierto, color gris/desconocido
    if (!AffinityDiscoveryTracker.IsDiscovered(enemy.id, type))
    {
        buttonImage.color = colorDesconocido;
        return;
    }

    // Si ya se descubrió, buscar qué multiplicador tiene este enemigo
    AffinityMultiplier multiplier = AffinityMultiplier.Neutral; // Por defecto

    foreach (var relation in enemy.affinityRelations)
    {
        if (relation.type == type)
        {
            multiplier = relation.multiplier;
            break;
        }
    }

    // Asignar color según el multiplicador
    buttonImage.color = multiplier switch
    {
        AffinityMultiplier.Weak => colorDebilidad,
        AffinityMultiplier.Strong => colorResistencia,
        AffinityMultiplier.Immune => colorInmunidad,
        _ => colorNeutral
    };

    
}
public void UpdateAffinitiesUI()
{
    if (combatManager.GetCombatMode() == CombatMode.PlayerChooses)
    {
        EnemyData enemy = combatManager.GetCurrentEnemy().enemyData;
        UpdateButtonColor(fuerzaButton, AffinityType.Fuerza, enemy);
        UpdateButtonColor(agilidadButton, AffinityType.Agilidad, enemy);
        UpdateButtonColor(destrezaButton, AffinityType.Destreza, enemy);
    }
}

    /// <summary>
    /// Actualiza el display de vida del jugador
    /// </summary>
    void UpdatePlayerLifeUI()
    {
        if (vidaActualText != null)
        {
            int currentLife = combatManager.GetPlayerLife();
            int maxLife = combatManager.GetPlayerMaxLife();
            vidaActualText.text = $"Vida: {currentLife}/{maxLife}";
        }
    }
}
