using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatUIManager : MonoBehaviour
{
    [Header("Referencias")]
    public CombatManager combatManager;
    public AbilityManager abilityManager;

    [Header("Enemy Display")]
    public Image enemySprite;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyTierText;

    [Header("Stats Display")]
    public TextMeshProUGUI vidaText;
    public TextMeshProUGUI escaladoText;
    public TextMeshProUGUI intentosText;
    public TextMeshProUGUI dadosText;
    public TextMeshProUGUI vidaActualText;

    [Header("Buttons - Modo Passive")]
    public GameObject passiveModePanel;
    public Button attackButton;
    public TextMeshProUGUI cartasText;

    [Header("Buttons - Modo PlayerChooses/RPG - NUEVO SISTEMA")]
    public GameObject playerChoosePanel;
    
    // Botones principales (selección de tipo)
    public Button fuerzaMainButton;
    public Button agilidadMainButton;
    public Button destrezaMainButton;
    
    // Textos de los botones principales
    public TextMeshProUGUI fuerzaMainText;
    public TextMeshProUGUI agilidadMainText;
    public TextMeshProUGUI destrezaMainText;
    
    // Paneles de habilidades (se muestran/ocultan)
    public GameObject fuerzaAbilitiesPanel;
    public GameObject agilidadAbilitiesPanel;
    public GameObject destrezaAbilitiesPanel;
    
    // Botones de habilidades (3 por tipo)
    [Header("Fuerza Abilities")]
    public Button fuerzaAbility1Button;
    public Button fuerzaAbility2Button;
    public Button fuerzaAbility3Button;
    public TextMeshProUGUI fuerzaAbility1Text;
    public TextMeshProUGUI fuerzaAbility2Text;
    public TextMeshProUGUI fuerzaAbility3Text;
    
    [Header("Agilidad Abilities")]
    public Button agilidadAbility1Button;
    public Button agilidadAbility2Button;
    public Button agilidadAbility3Button;
    public TextMeshProUGUI agilidadAbility1Text;
    public TextMeshProUGUI agilidadAbility2Text;
    public TextMeshProUGUI agilidadAbility3Text;
    
    [Header("Destreza Abilities")]
    public Button destrezaAbility1Button;
    public Button destrezaAbility2Button;
    public Button destrezaAbility3Button;
    public TextMeshProUGUI destrezaAbility1Text;
    public TextMeshProUGUI destrezaAbility2Text;
    public TextMeshProUGUI destrezaAbility3Text;

    [Header("Colors")]
    public Color colorDesconocido = Color.gray;
    public Color colorDebilidad = Color.green;
    public Color colorResistencia = Color.red;
    public Color colorInmunidad = Color.black;
    public Color colorNeutral = Color.white;
    public Color lockedAbilityColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("General Buttons")]

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

    // Estado actual del panel expandido
    private AffinityType? currentExpandedType = null;

void Start()
{
    // Configurar botón de modo pasivo
    attackButton.onClick.AddListener(() => combatManager.PlayerAttempt());

    // === NUEVO SISTEMA: Botones principales ===
    fuerzaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Fuerza));
    agilidadMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Agilidad));
    destrezaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Destreza));

    // === NUEVO: Botones de habilidades ===
    // Fuerza
    fuerzaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 0));
    fuerzaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 1));
    fuerzaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 2));
    
    // Agilidad
    agilidadAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 0));
    agilidadAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 1));
    agilidadAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 2));
    
    // Destreza
    destrezaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 0));
    destrezaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 1));
    destrezaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 2));

    // Botones de victoria/derrota
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

    // Ocultar paneles de habilidades al inicio
    HideAllAbilityPanels();
}

    /// <summary>
    /// NUEVO: Alterna la visibilidad del panel de habilidades del tipo seleccionado
    /// </summary>
    void ToggleAbilityPanel(AffinityType type)
    {
        // Si ya está expandido este tipo, colapsar
        if (currentExpandedType == type)
        {
            HideAllAbilityPanels();
            currentExpandedType = null;
            return;
        }

        // Ocultar todos los paneles
        HideAllAbilityPanels();

        // Mostrar el panel del tipo seleccionado
        GameObject panelToShow = type switch
        {
            AffinityType.Fuerza => fuerzaAbilitiesPanel,
            AffinityType.Agilidad => agilidadAbilitiesPanel,
            AffinityType.Destreza => destrezaAbilitiesPanel,
            _ => null
        };

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            currentExpandedType = type;
            
            // Actualizar los botones de habilidades
            UpdateAbilityButtons(type);
            
            // Seleccionar el tipo de ataque en el CombatManager
            combatManager.SelectAttackType(type);
        }
    }

    /// <summary>
    /// NUEVO: Oculta todos los paneles de habilidades
    /// </summary>
    void HideAllAbilityPanels()
    {
        fuerzaAbilitiesPanel.SetActive(false);
        agilidadAbilitiesPanel.SetActive(false);
        destrezaAbilitiesPanel.SetActive(false);
    }

    /// <summary>
    /// NUEVO: Actualiza los botones de habilidades según las disponibles
    /// </summary>
    void UpdateAbilityButtons(AffinityType type)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);

        // Arrays de botones y textos según el tipo
        Button[] buttons = type switch
        {
            AffinityType.Fuerza => new[] { fuerzaAbility1Button, fuerzaAbility2Button, fuerzaAbility3Button },
            AffinityType.Agilidad => new[] { agilidadAbility1Button, agilidadAbility2Button, agilidadAbility3Button },
            AffinityType.Destreza => new[] { destrezaAbility1Button, destrezaAbility2Button, destrezaAbility3Button },
            _ => null
        };

        TextMeshProUGUI[] texts = type switch
        {
            AffinityType.Fuerza => new[] { fuerzaAbility1Text, fuerzaAbility2Text, fuerzaAbility3Text },
            AffinityType.Agilidad => new[] { agilidadAbility1Text, agilidadAbility2Text, agilidadAbility3Text },
            AffinityType.Destreza => new[] { destrezaAbility1Text, destrezaAbility2Text, destrezaAbility3Text },
            _ => null
        };

        if (buttons == null || texts == null) return;

        // Configurar cada botón
        for (int i = 0; i < 3; i++)
        {
            if (i < abilities.Count)
            {
                AbilityData ability = abilities[i];
                
                // Actualizar texto
                string costInfo = GetAbilityCostString(ability);
                texts[i].text = $"{ability.abilityName}\n{costInfo}";

                // Verificar si puede usar la habilidad
                bool canUse = abilityManager.CanUseAbility(
                    ability, 
                    combatManager.GetPlayerLife(), 
                    combatManager.GetCurrentEnemy().attemptsRemaining
                );

                buttons[i].interactable = canUse;
                
                // Color visual
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = canUse ? Color.white : lockedAbilityColor;
                }
            }
            else
            {
                // No hay habilidad en este slot
                texts[i].text = "???";
                buttons[i].interactable = false;
                
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = lockedAbilityColor;
                }
            }
        }
    }

    /// <summary>
    /// NUEVO: Genera el string de costo de una habilidad
    /// </summary>
    string GetAbilityCostString(AbilityData ability)
    {
        List<string> costs = new List<string>();
        
        if (ability.cardCost > 0)
            costs.Add($"{ability.cardCost}");
        
        if (ability.healthCost > 0)
            costs.Add($"{ability.healthCost}");
        
        if (ability.turnCost > 1)
            costs.Add($"{ability.turnCost}");

        return costs.Count > 0 ? string.Join(" ", costs) : "Sin coste";
    }

    /// <summary>
    /// NUEVO: Usa una habilidad específica
    /// </summary>
    void UseAbility(AffinityType type, int abilityIndex)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);
        
        if (abilityIndex < abilities.Count)
        {
            AbilityData ability = abilities[abilityIndex];
            
            // Seleccionar el tipo de ataque
            combatManager.SelectAttackType(type);
            
            // Ejecutar el ataque con la habilidad
            combatManager.PlayerAttempt(ability);
            
            // Actualizar UI de afinidades
            UpdateAffinitiesUI();
            
            // Actualizar botones de habilidades
            UpdateAbilityButtons(type);
            
            // Colapsar panel después de usar habilidad (opcional)
            HideAllAbilityPanels();
            currentExpandedType = null;
        }
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

    // NUEVO: Ocultar paneles de habilidades
    HideAllAbilityPanels();

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
        enemyTierText.text = enemy.enemyTierData.GetEnemyTier();

        // Actualizar stats
        if (combatManager.GetCombatMode() == CombatMode.TraditionalRPG) 
        {
            vidaText.text = enemy.currentRPGHealth.ToString();
        }
        else 
        {
            vidaText.text = enemy.enemyTierData.healthThreshold.ToString();
        }


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

        // NUEVO: Resetear paneles de habilidades
        HideAllAbilityPanels();
        currentExpandedType = null;
    }

string GetEscaladoText(EnemyInstance enemy)
{
    if (combatManager.GetCombatMode() == CombatMode.PlayerChooses || 
        combatManager.GetCombatMode() == CombatMode.TraditionalRPG)
    {
        return " "; // No revelar debilidad en modos con elección
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
    
    // Actualizar vida del enemigo en Traditional RPG
    if (combatManager.GetCombatMode() == CombatMode.TraditionalRPG)
    {
        EnemyInstance currentEnemy = combatManager.GetCurrentEnemy();
        vidaText.text = Mathf.Max(0, currentEnemy.currentRPGHealth).ToString();
    }
    
    UpdateAffinitiesUI();

    // NUEVO: Actualizar botones de habilidades si hay panel expandido
    if (currentExpandedType.HasValue)
    {
        UpdateAbilityButtons(currentExpandedType.Value);
    }
}

    void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
{
    // Actualizar vida del jugador
    UpdatePlayerLifeUI();

    // NUEVO: Colapsar paneles de habilidades
    HideAllAbilityPanels();
    currentExpandedType = null;

    if (victory)
    {
        passiveEndPanel.SetActive(true);
        
        // Mensaje unificado para PlayerChooses y TraditionalRPG
        if (rewardCard == default && 
            (combatManager.GetCombatMode() == CombatMode.PlayerChooses || 
             combatManager.GetCombatMode() == CombatMode.TraditionalRPG))
        {
             passiveEndMessageText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\nNo explotaste la debilidad y no hubo suerte con el botín.";
        }
        else
        {
            passiveEndMessageText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\nObtienes 1 carta de:\n{rewardCard}";
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
        playerChoosesVictoryText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\n Elige tu recompensa:";
    }

    void SelectCard(AffinityType selectedType)
    {
        // Notificar al CombatManager la carta seleccionada
        combatManager.SelectRewardCard(selectedType);
        
        // Ocultar panel de selección
        playerChoosesVictoryPanel.SetActive(false);
        
        // Mostrar panel de victoria con la carta seleccionada
        passiveEndPanel.SetActive(true);
        passiveEndMessageText.text = $"¡WOW! ¡VICTORIA!\n\n Obtuviste 1 carta de:\n{selectedType}";
        
        // Actualizar display de cartas
        UpdateCardsDisplay();
    }

    void HandleAttemptsChanged(int remainingAttempts)
    {
        intentosText.text = remainingAttempts.ToString();
        UpdateCardsDisplay();
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
            cartasText.text = $"Tienes: {currentCards} muchas Cartas de {combatManager.GetCurrentEnemy().enemyData.affinityType}";
        }
        else
        {
            // NUEVO: Actualizar textos de botones principales con cantidad de cartas
            fuerzaMainText.text = $"ATACAR CON FUERZA\nCartas: {combatManager.GetCardsOfType(AffinityType.Fuerza)}";
            agilidadMainText.text = $"ATACAR CON AGILIDAD\nCartas: {combatManager.GetCardsOfType(AffinityType.Agilidad)}";
            destrezaMainText.text = $"ATACAR CON DESTREZA\nCartas: {combatManager.GetCardsOfType(AffinityType.Destreza)}";
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
    if (combatManager.GetCombatMode() == CombatMode.PlayerChooses || 
        combatManager.GetCombatMode() == CombatMode.TraditionalRPG)
    {
        EnemyData enemy = combatManager.GetCurrentEnemy().enemyData;
        
        // Actualizar colores de los botones principales
        UpdateButtonColor(fuerzaMainButton, AffinityType.Fuerza, enemy);
        UpdateButtonColor(agilidadMainButton, AffinityType.Agilidad, enemy);
        UpdateButtonColor(destrezaMainButton, AffinityType.Destreza, enemy);
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