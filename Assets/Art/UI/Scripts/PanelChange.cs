using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelChange : MonoBehaviour
{
    public GameObject personajesPanel, skinsPanel, accesoriosPanel; // Paneles de selección

    public void ShowCategory(string category)
    {
        personajesPanel.SetActive(category == "Personajes");
        skinsPanel.SetActive(category == "Skins");
        accesoriosPanel.SetActive(category == "Accesorios");
    }
}
