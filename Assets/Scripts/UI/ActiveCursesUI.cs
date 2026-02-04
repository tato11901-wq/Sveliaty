using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ActiveCursesUI : MonoBehaviour
{
    [Header("References")]
    public CurseManager curseManager;
    
    [Header("UI Elements")]
    public Transform cursesContainer;
    public GameObject curseIconPrefab;
    
    void Update()
    {
        UpdateCurseDisplay();
    }
    
    void UpdateCurseDisplay()
    {
        // Limpiar iconos existentes
        foreach (Transform child in cursesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Mostrar curses activas
        foreach (var curse in curseManager.GetActiveCurses())
        {
            GameObject iconObj = Instantiate(curseIconPrefab, cursesContainer);
            Image icon = iconObj.GetComponent<Image>();
            icon.sprite = curse.data.icon;
            
            // Mostrar duración si aplica
            if (curse.remainingDuration > 0)
            {
                TextMeshProUGUI durationText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                if (durationText != null)
                {
                    durationText.text = curse.remainingDuration.ToString();
                }
            }
        }
    }
}