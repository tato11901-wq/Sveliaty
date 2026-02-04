using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurseChoiceUI : MonoBehaviour
{
    [Header("References")]
    public CurseManager curseManager;
    
    [Header("UI Elements")]
    public GameObject panel;
    public Button[] cardButtons; // 3 botones
    public Image[] cardFronts; // Las 3 cartas volteadas
    
    private List<CurseData> currentOptions;
    private bool[] revealed = new bool[3];
    
void OnEnable()
{
    if (curseManager == null)
    {
        Debug.LogError("❌ CurseChoiceUI: CurseManager no asignado");
        return;
    }
    
    curseManager.OnCurseChoiceEvent += ShowChoiceEvent;
    Debug.Log("✅ CurseChoiceUI suscrito correctamente");
}
    
    void OnDisable()
    {
        curseManager.OnCurseChoiceEvent -= ShowChoiceEvent;
    }
    
    void ShowChoiceEvent(List<CurseData> options)
    {

            Debug.Log($"🎴 ShowChoiceEvent llamado con {options.Count} opciones");
    
    if (panel == null)
    {
        Debug.LogError("❌ Panel es NULL en CurseChoiceUI");
        return;
    }
    
    currentOptions = options;
    panel.SetActive(true);
    Debug.Log($"✅ Panel activado: {panel.activeSelf}");

    
        currentOptions = options;
        panel.SetActive(true);
        
        // Resetear estado
        for (int i = 0; i < 3; i++)
        {
            revealed[i] = false;
            cardFronts[i].gameObject.SetActive(false); // Mostrar reverso
            cardButtons[i].interactable = true;
            
            int index = i; // Closure
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
        }
    }
    
    void OnCardSelected(int index)
    {
        // Revelar la carta seleccionada
        revealed[index] = true;
        cardFronts[index].sprite = currentOptions[index].icon;
        cardFronts[index].gameObject.SetActive(true);
        
        // Deshabilitar otros botones
        for (int i = 0; i < 3; i++)
        {
            cardButtons[i].interactable = false;
        }
        
        // Dar la maldición al jugador
        curseManager.ObtainCurse(currentOptions[index]);
        
        // Cerrar panel después de 2 segundos
        StartCoroutine(CloseAfterDelay(2f));
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }
}