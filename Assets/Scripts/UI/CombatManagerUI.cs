using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatUIManager : MonoBehaviour
{
    [Header("Referencias")]
    public CombatManager combatManager;
    public AbilityManager abilityManager;
    public CurseManager curseManager;
    
    [Header("Canvas Groups para ocultar durante maldiciones")]
    public CanvasGroup combatUICanvasGroup; // Asignar el panel padre de toda la UI de combate

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

    [Header("Buttons - Modo PlayerChooses/RPG")]
    public GameObject playerChoosePanel;
    public Button fuerzaMainButton;
    public Button agilidadMainButton;
    public Button destrezaMainButton;
    public Button backButton; // Boton Volver
    public TextMeshProUGUI fuerzaMainText;
    public TextMeshProUGUI agilidadMainText;
    public TextMeshProUGUI destrezaMainText;
    public GameObject fuerzaAbilitiesPanel;
    public GameObject agilidadAbilitiesPanel;
    public GameObject destrezaAbilitiesPanel;
    
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

    [Header("Results Display")]
    public TextMeshProUGUI resultadoDadosText;
    public TextMeshProUGUI resultadoAtaqueText;

    [Header("Victory/Defeat Panels")]
    public GameObject passiveEndPanel;
    public TextMeshProUGUI passiveEndMessageText;
    public Button passiveNextEnemyButton;

    public GameObject playerChoosesVictoryPanel;
    public TextMeshProUGUI playerChoosesVictoryText;
    public Button selectFuerzaButton;
    public Button selectAgilidadButton;
    public Button selectDestrezaButton;

    public GameObject defeatPanel;
    public TextMeshProUGUI defeatMessageText;
    public Button defeatNextEnemyButton;

    // NUEVO: Panel de notificación de maldición
    [Header("Curse Notification Panel")]
    public GameObject curseNotificationPanel;
    public TextMeshProUGUI curseNotificationText;
    public Button curseNotificationButton;

    private AffinityType? currentExpandedType = null;

void Start()
{
    attackButton.onClick.AddListener(() => combatManager.PlayerAttempt());

    fuerzaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Fuerza));
    agilidadMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Agilidad));
    destrezaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Destreza));

    fuerzaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 0));
    fuerzaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 1));
    fuerzaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 2));
    
    agilidadAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 0));
    agilidadAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 1));
    agilidadAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 2));
    
    destrezaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 0));
    destrezaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 1));
    destrezaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 2));

    // MODIFICADO: Ya no va directo al siguiente enemigo
    passiveNextEnemyButton.onClick.AddListener(() => OnVictoryPanelContinue());
    defeatNextEnemyButton.onClick.AddListener(() => OnDefeatPanelContinue());

    selectFuerzaButton.onClick.AddListener(() => SelectCard(AffinityType.Fuerza));
    selectAgilidadButton.onClick.AddListener(() => SelectCard(AffinityType.Agilidad));
    selectDestrezaButton.onClick.AddListener(() => SelectCard(AffinityType.Destreza));

    // NUEVO: Botón de notificación de maldición
    if (curseNotificationButton != null)
    {
        curseNotificationButton.onClick.AddListener(() => OnCurseNotificationContinue());
    }

    // NUEVO: Botón Volver
    if (backButton != null)
    {
        backButton.onClick.AddListener(() => OnBackButtonPressed());
    }

    HideAllAbilityPanels();
    HideBackButton();
}

    void ToggleAbilityPanel(AffinityType type)
    {
        // Ocultar todos los paneles de habilidades
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
            UpdateAbilityButtons(type);
            combatManager.SelectAttackType(type);
            
            // NUEVO: Ocultar botones principales y mostrar botón Volver
            HideMainButtons();
            ShowBackButton();
        }
    }

    void HideAllAbilityPanels()
    {
        fuerzaAbilitiesPanel.SetActive(false);
        agilidadAbilitiesPanel.SetActive(false);
        destrezaAbilitiesPanel.SetActive(false);
    }

    void UpdateAbilityButtons(AffinityType type)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);

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

        for (int i = 0; i < 3; i++)
        {
            if (i < abilities.Count)
            {
                AbilityData ability = abilities[i];
                
                string costInfo = GetAbilityCostString(ability);
                texts[i].text = $"{ability.abilityName}\n{costInfo}";

                bool canUse = abilityManager.CanUseAbility(
                    ability, 
                    combatManager.GetPlayerLife(), 
                    combatManager.GetCurrentEnemy().attemptsRemaining
                );

                buttons[i].interactable = canUse;
                
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = canUse ? Color.white : lockedAbilityColor;
                }
            }
            else
            {
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

    string GetAbilityCostString(AbilityData ability)
    {
        List<string> costs = new List<string>();
        
        if (ability.cardCost > 0)
            costs.Add($"{ability.cardCost}");
        
        if (ability.healthCost > 0)
            costs.Add($"{ability.healthCost}");
        
        if (ability.turnCost > 1)
            costs.Add($"⏱{ability.turnCost}");

        return costs.Count > 0 ? string.Join(" ", costs) : "Gratis";
    }

    void UseAbility(AffinityType type, int abilityIndex)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);
        
        if (abilityIndex < abilities.Count)
        {
            AbilityData ability = abilities[abilityIndex];
            combatManager.SelectAttackType(type);
            combatManager.PlayerAttempt(ability);
            UpdateAffinitiesUI();
            UpdateAbilityButtons(type);
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

    passiveEndPanel.SetActive(false);
    playerChoosesVictoryPanel.SetActive(false);
    defeatPanel.SetActive(false);
    
    if (curseNotificationPanel != null)
    {
        curseNotificationPanel.SetActive(false);
    }

    HideAllAbilityPanels();

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
        // Restaurar UI de combate
        if (combatUICanvasGroup != null)
        {
            combatUICanvasGroup.alpha = 1f;
            combatUICanvasGroup.interactable = true;
            combatUICanvasGroup.blocksRaycasts = true;
        }
        
        enemySprite.sprite = enemy.enemyTierData.sprite;
        enemyNameText.text = enemy.enemyData.displayName;
        enemyTierText.text = enemy.enemyTierData.GetEnemyTier();

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
        UpdatePlayerLifeUI();

        resultadoDadosText.text = "-";
        resultadoAtaqueText.text = "-";

        UpdateCardsDisplay();
        UpdateAffinitiesUI();

        // NUEVO: Resetear estado de botones
        HideAllAbilityPanels();
        ShowMainButtons();
        HideBackButton();
        currentExpandedType = null;
    }

    string GetEscaladoText(EnemyInstance enemy)
    {
        if (combatManager.GetCombatMode() == CombatMode.PlayerChooses || 
            combatManager.GetCombatMode() == CombatMode.TraditionalRPG)
        {
            return " ";
        }
        else
        {
            return enemy.enemyData.affinityType.ToString();
        }
    }

    void HandleAttackResult(int roll, int bonus, int total, float multiplier)
    {
        resultadoDadosText.text = roll.ToString();
        
        string colorTag = multiplier > 1.1f ? "<color=green>" : (multiplier < 0.9f ? "<color=red>" : "<color=white>");
        string multText = multiplier != 1f ? $" ({colorTag}x{multiplier}</color>)" : "";
        
        resultadoAtaqueText.text = $"{total}{multText}";
        
        if (combatManager.GetCombatMode() == CombatMode.TraditionalRPG)
        {
            EnemyInstance currentEnemy = combatManager.GetCurrentEnemy();
            vidaText.text = Mathf.Max(0, currentEnemy.currentRPGHealth).ToString();
        }
        
        UpdateAffinitiesUI();

        if (currentExpandedType.HasValue)
        {
            UpdateAbilityButtons(currentExpandedType.Value);
        }
    }

    void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
    {
        UpdatePlayerLifeUI();
        HideAllAbilityPanels();
        currentExpandedType = null;

        if (victory)
        {
            passiveEndPanel.SetActive(true);
            
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
        playerChoosesVictoryPanel.SetActive(true);
        playerChoosesVictoryText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\n Elige tu recompensa:";
    }

    void SelectCard(AffinityType selectedType)
    {
        combatManager.SelectRewardCard(selectedType);
        playerChoosesVictoryPanel.SetActive(false);
        passiveEndPanel.SetActive(true);
        passiveEndMessageText.text = $"¡WOW! ¡VICTORIA!\n\n Obtuviste 1 carta de:\n{selectedType}";
        UpdateCardsDisplay();
    }

    void HandleAttemptsChanged(int remainingAttempts)
    {
        intentosText.text = remainingAttempts.ToString();
        UpdatePlayerLifeUI();
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
            fuerzaMainText.text = $"ATACAR CON FUERZA\nCartas: {combatManager.GetCardsOfType(AffinityType.Fuerza)}";
            agilidadMainText.text = $"ATACAR CON AGILIDAD\nCartas: {combatManager.GetCardsOfType(AffinityType.Agilidad)}";
            destrezaMainText.text = $"ATACAR CON DESTREZA\nCartas: {combatManager.GetCardsOfType(AffinityType.Destreza)}";
        }
    }

    void UpdateButtonColor(Button button, AffinityType type, EnemyData enemy)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) return;

        if (!AffinityDiscoveryTracker.IsDiscovered(enemy.id, type))
        {
            buttonImage.color = colorDesconocido;
            return;
        }

        AffinityMultiplier multiplier = AffinityMultiplier.Neutral;

        foreach (var relation in enemy.affinityRelations)
        {
            if (relation.type == type)
            {
                multiplier = relation.multiplier;
                break;
            }
        }

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
            UpdateButtonColor(fuerzaMainButton, AffinityType.Fuerza, enemy);
            UpdateButtonColor(agilidadMainButton, AffinityType.Agilidad, enemy);
            UpdateButtonColor(destrezaMainButton, AffinityType.Destreza, enemy);
        }
    }

    void UpdatePlayerLifeUI()
    {
        if (vidaActualText != null)
        {
            int currentLife = combatManager.GetPlayerLife();
            int maxLife = combatManager.GetPlayerMaxLife();
            vidaActualText.text = $"Vida: {currentLife}/{maxLife}";
        }
    }

    // ========== NUEVO: SISTEMA DE FLUJO SECUENCIAL ==========

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de victoria
    /// </summary>
    void OnVictoryPanelContinue()
    {
        passiveEndPanel.SetActive(false);
        
        // Verificar si hay evento de maldición
        if (combatManager.ShouldShowCurseEvent())
        {
            ShowCurseNotification();
        }
        else
        {
            // No hay maldición, ir directo al siguiente enemigo
            combatManager.ContinueToNextEnemy();
        }
    }

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de derrota
    /// </summary>
    void OnDefeatPanelContinue()
    {
        defeatPanel.SetActive(false);
        combatManager.ContinueToNextEnemy();
    }

    /// <summary>
    /// Muestra el panel de notificación "¡Has sido maldecido!"
    /// </summary>
    void ShowCurseNotification()
    {
        // Ocultar UI de combate
        if (combatUICanvasGroup != null)
        {
            combatUICanvasGroup.alpha = 0f;
            combatUICanvasGroup.interactable = false;
            combatUICanvasGroup.blocksRaycasts = false;
        }
        
        if (curseNotificationPanel != null)
        {
            curseNotificationPanel.SetActive(true);
            if (curseNotificationText != null)
            {
                curseNotificationText.text = "¡Has sido MALDECIDO!\n\nElige una carta...";
            }
        }
        else
        {
            // Fallback: ir directo al evento de selección
            OnCurseNotificationContinue();
        }
    }

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de notificación
    /// </summary>
    void OnCurseNotificationContinue()
    {
        if (curseNotificationPanel != null)
        {
            curseNotificationPanel.SetActive(false);
        }
        
        // Activar el evento de selección de 3 cartas
        combatManager.TriggerCurseEventFromUI();
        
        // La secuencia continuará cuando el jugador seleccione una carta en CurseChoiceUI
    }

    // ========== NUEVO: SISTEMA DE BOTON VOLVER ==========

    /// <summary>
    /// Cuando el jugador presiona el botón Volver
    /// </summary>
    void OnBackButtonPressed()
    {
        // Ocultar paneles de habilidades
        HideAllAbilityPanels();
        currentExpandedType = null;
        
        // Mostrar botones principales
        ShowMainButtons();
        
        // Ocultar botón Volver
        HideBackButton();
    }

    /// <summary>
    /// Oculta los botones principales de selección de tipo
    /// </summary>
    void HideMainButtons()
    {
        if (fuerzaMainButton != null)
            fuerzaMainButton.gameObject.SetActive(false);
        
        if (agilidadMainButton != null)
            agilidadMainButton.gameObject.SetActive(false);
        
        if (destrezaMainButton != null)
            destrezaMainButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra los botones principales de selección de tipo
    /// </summary>
    void ShowMainButtons()
    {
        if (fuerzaMainButton != null)
            fuerzaMainButton.gameObject.SetActive(true);
        
        if (agilidadMainButton != null)
            agilidadMainButton.gameObject.SetActive(true);
        
        if (destrezaMainButton != null)
            destrezaMainButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Muestra el botón Volver
    /// </summary>
    void ShowBackButton()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Oculta el botón Volver
    /// </summary>
    void HideBackButton()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }
}