using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CurseChoiceUI : MonoBehaviour
{
    [Header("References")]
    public BossRushManager bossRushManager; // NUEVO: Para continuar al siguiente combate
    public CombatManager combatManager;
    public CurseManager curseManager;
    
    [Header("UI Elements - Fase 1: 3 Cartas")]
    public GameObject threeCardsPanel;
    public Button[] cardButtons; // 3 botones (todos muestran reverso)
    
    [Header("UI Elements - Fase 2: Descripcion")]
    public GameObject descriptionPanel;
    public Image curseIcon; // Icono de la maldicion seleccionada
    public TextMeshProUGUI curseNameText;
    public TextMeshProUGUI curseDescriptionText;
    public TextMeshProUGUI curseTypeText; // "Positiva" / "Negativa" / "Gambling"
    public Button continueButton;
    
    private List<CurseData> currentOptions;
    private CurseData selectedCurse;
    
    void Start()
    {
        if (curseManager == null)
        {
            Debug.LogError("CurseChoiceUI: CurseManager no asignado en Inspector");
            return;
        }
        
        curseManager.OnCurseChoiceEvent += ShowChoiceEvent;
        Debug.Log("CurseChoiceUI: Suscrito al evento OnCurseChoiceEvent");
        
        // Configurar boton de continuar
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
        
        // Ocultar ambos paneles al inicio
        if (threeCardsPanel != null)
        {
            threeCardsPanel.SetActive(false);
        }
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (curseManager != null)
        {
            curseManager.OnCurseChoiceEvent -= ShowChoiceEvent;
        }
    }
    
    /// <summary>
    /// FASE 1: Mostrar las 3 cartas volteadas (todas iguales)
    /// </summary>
    void ShowChoiceEvent(List<CurseData> options)
    {
        Debug.Log("CurseChoiceUI: ShowChoiceEvent llamado con " + options.Count + " opciones");
        
        if (threeCardsPanel == null)
        {
            Debug.LogError("CurseChoiceUI: threeCardsPanel no asignado");
            return;
        }
        
        currentOptions = options;
        selectedCurse = null;
        
        // Mostrar panel de 3 cartas
        threeCardsPanel.SetActive(true);
        
        // Ocultar panel de descripcion
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
        
        Debug.Log("Panel de 3 cartas activado");
        
        // Configurar los 3 botones
        for (int i = 0; i < 3; i++)
        {
            if (cardButtons != null && cardButtons.Length > i && cardButtons[i] != null)
            {
                cardButtons[i].interactable = true;
                
                int index = i; // Closure
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }
        }
    }
    
    /// <summary>
    /// Cuando el jugador selecciona una de las 3 cartas
    /// </summary>
    void OnCardSelected(int index)
    {
        if (currentOptions == null || index >= currentOptions.Count)
        {
            Debug.LogError("Indice invalido: " + index);
            return;
        }
        
        selectedCurse = currentOptions[index];
        Debug.Log("Carta " + index + " seleccionada: " + selectedCurse.curseName);
        
        // Deshabilitar todos los botones
        if (cardButtons != null)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].interactable = false;
                }
            }
        }
        
        // Dar la maldicion al jugador (aplicar efectos instantaneos si aplica)
        curseManager.ObtainCurse(selectedCurse);
        
        // Esperar un momento antes de cambiar a la fase de descripcion
        StartCoroutine(TransitionToDescriptionPhase());
    }
    
    /// <summary>
    /// Transicion suave entre fase de seleccion y fase de descripcion
    /// </summary>
    IEnumerator TransitionToDescriptionPhase()
    {
        // Esperar 1 segundo para que el jugador procese su seleccion
        yield return new WaitForSeconds(1f);
        
        // Ocultar las 3 cartas
        if (threeCardsPanel != null)
        {
            threeCardsPanel.SetActive(false);
        }
        
        // FASE 2: Mostrar panel de descripcion
        ShowDescriptionPhase();
    }
    
    /// <summary>
    /// FASE 2: Mostrar la descripcion de la maldicion seleccionada
    /// </summary>
    void ShowDescriptionPhase()
    {
        if (descriptionPanel == null)
        {
            Debug.LogError("CurseChoiceUI: descriptionPanel no asignado");
            // Fallback: ir directo al siguiente enemigo
            ContinueToNextEnemy();
            return;
        }
        
        if (selectedCurse == null)
        {
            Debug.LogError("No hay maldicion seleccionada");
            ContinueToNextEnemy();
            return;
        }
        
        descriptionPanel.SetActive(true);
        
        // Actualizar textos
        if (curseNameText != null)
        {
            curseNameText.text = selectedCurse.curseName;
        }
        
        if (curseDescriptionText != null)
        {
            curseDescriptionText.text = selectedCurse.description;
        }
        
        if (curseTypeText != null)
        {
            string typeLabel = selectedCurse.type switch
            {
                CurseType.Positive => "<color=green>POSITIVA</color>",
                CurseType.Negative => "<color=red>NEGATIVA</color>",
                CurseType.Gambling => "<color=yellow>AZAR</color>",
                _ => "DESCONOCIDA"
            };
            curseTypeText.text = typeLabel;
        }
        
        if (curseIcon != null && selectedCurse.icon != null)
        {
            curseIcon.sprite = selectedCurse.icon;
            curseIcon.gameObject.SetActive(true);
        }
        
        Debug.Log("Panel de descripcion mostrado: " + selectedCurse.curseName);
    }
    
    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de descripcion
    /// </summary>
    void OnContinueButtonPressed()
    {
        Debug.Log("Usuario presiono continuar -> Siguiente enemigo");
        
        // Ocultar panel de descripcion
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
        
        // Continuar al siguiente enemigo
        ContinueToNextEnemy();
    }
    
    /// <summary>
    /// Finalizar la secuencia e iniciar el siguiente combate
    /// </summary>
    void ContinueToNextEnemy()
    {
        if (bossRushManager != null)
        {
            combatManager.ResetPostCombatFlag();
            bossRushManager.ContinueToNextCombat();
        }
        else
        {
            Debug.LogError("BossRushManager no asignado en CurseChoiceUI");
        }
    }
}