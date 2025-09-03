using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinEleccion : MonoBehaviour
{
    public List<Button> buttons = new List<Button>(); // Dynamic list of buttons
    public string[] names;
    public string selectedName;
    public GameObject coinIcon;

    public Sprite lockedSprite;
    public Sprite unlockedSprite;
    public Button buyButton;

    public GameObject ButtonPrefab; 
    public GameObject ParentUI; 

    public Color lockedIconColor = new Color(1, 1, 1, 0.5f);
    public Color unlockedIconColor = new Color(1, 1, 1, 1f);

    public TMP_Text moneyText;
    public TMP_Text insufficientMoneyText;

    private void Start()
    {
        GenerateButtons();
        buyButton.gameObject.SetActive(false); 
        UpdateMoneyUI();
    
        string lastSkin = SaveDataManager.Instance.LastCalledItem;
        if (!string.IsNullOrEmpty(lastSkin) && SaveDataManager.Instance.HasItem(lastSkin))
        {
            ApplySkin(lastSkin);
            selectedName = lastSkin;
            Debug.Log("Última skin aplicada: " + lastSkin);
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = SaveDataManager.Instance.Coins.ToString();
    }

    void GenerateButtons()
    {   
        
        // Clear old buttons if they exist
        foreach (Button btn in buttons)
        {
            if (btn.transform.parent != null) Destroy(btn.transform.parent.gameObject);
            else Destroy(btn.gameObject);
        }

        buttons.Clear();

        for (int i = 0; i < names.Length; i++)
        {   
            //SaveDataManager.Instance.OwnedItems.Find(names[i]);
            var skin = ResourcesManager.Instance.GetSkin(names[i]);
            if (skin == null)
            {
                Debug.LogWarning($"Skin not found: {names[i]}");
                continue;
            }

            var unlocked = SaveDataManager.Instance.HasItem(names[i]);
            int index = i;

            // Instantiate new button and set it inside ParentUI
            GameObject newButtonObj = Instantiate(ButtonPrefab, ParentUI.transform);

            // Ensure the button is found within the prefab hierarchy
            Button newButton = newButtonObj.GetComponentInChildren<Button>();
            if (newButton == null)
            {
                Debug.LogError($"Button component not found in prefab {ButtonPrefab.name}");
                continue;
            }

            buttons.Add(newButton); // Store button in the list

            // Assign click event
            newButton.onClick.AddListener(() => OnButtonClick(index));

            // Update its appearance based on unlock status
            UpdateButtonAppearance(newButtonObj, unlocked, skin.Icon);
        }
    }

    void OnButtonClick(int index)
    {
        selectedName = names[index];
        Debug.Log("Skin selected: " + selectedName);

        if (insufficientMoneyText != null) insufficientMoneyText.text = "";

        if (index >= names.Length)
        {
            Debug.LogWarning("Index out of range: " + index);
            return;
        }

        if (!SaveDataManager.Instance.HasItem(selectedName))
        {
            Debug.Log("Locked skin selected. Showing buy button.");
            ApplySkin(selectedName, true);

            // Activar el botón de compra y el icono
            buyButton.gameObject.SetActive(true);
            coinIcon.SetActive(true);

            // Obtener el componente de texto del botón
            TMP_Text buyButtonText = buyButton.GetComponentInChildren<TMP_Text>();
            if (buyButtonText != null)
            {
                //buyButtonText.text = "Buy " + skinPrices[index] + " Coins";
                var cost = ResourcesManager.Instance.GetSkin(selectedName).CoinCost;
                buyButtonText.text = cost.ToString();  

                // Verificar si el jugador tiene suficiente dinero
                if (SaveDataManager.Instance.Coins >= cost)
                {
                    buyButtonText.color = new Color32(42, 61, 108, 255); // Color #2A3D66 cuando tiene suficiente dinero
                }
                else
                {
                    buyButtonText.color = Color.red; // Rojo cuando no tiene suficiente dinero
                }
            }

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => ComprarSkin(index));

            // Al cambiar de skin, el mensaje de "dinero insuficiente" desaparece
            if (insufficientMoneyText != null) insufficientMoneyText.text = "";

            return;
        }

        Debug.Log("Skin selected: " + selectedName);
        ApplySkin(selectedName);

        // Ocultar el botón de compra y el icono si ya está desbloqueada
        buyButton.gameObject.SetActive(false);
        coinIcon.SetActive(false);
    }

    void ApplySkin(string skinName, bool showOnly = false)
    {
        PlayerSkin selectedSkin = ResourcesManager.Instance.GetSkin(skinName);

        if (selectedSkin == null)
        {
            Debug.LogWarning("Skin not found: " + skinName);
            return;
        }

        /*if (!PlayerObject.Local)
        {
            Debug.LogError("Local player not found.");
            return;
        }*/

        SaveDataManager.Instance.SetLastCalledItem(skinName);
        if (PlayerObject.Local != null && !showOnly) PlayerObject.Local.Rpc_SetSkin(skinName);

        var renderGolf = InterfaceManager.Instance.sessionScreen.golfBallRend;
        if (renderGolf == null)
        {
            Debug.LogError("PlayerObject not found! Skin cannot be applied.");
            return;
        }

        // Apply mesh and materials to the example ball
        renderGolf.GetComponent<MeshFilter>().mesh = selectedSkin.Mesh;
        renderGolf.GetComponent<MeshRenderer>().material = selectedSkin.Material;

        if (PlayerObject.Local != null && PlayerObject.Local.Color != null) renderGolf.GetComponent<MeshRenderer>().material.color = PlayerObject.Local.Color;
        renderGolf.transform.localRotation = Quaternion.Euler(selectedSkin.Direction);

        if (selectedSkin.DisplayPosition != Vector3.zero) renderGolf.transform.localPosition = selectedSkin.DisplayPosition;
        else renderGolf.transform.localPosition = selectedSkin.DisplayPosition;

        if (selectedSkin.DisplayScale != Vector3.zero) renderGolf.transform.localScale = selectedSkin.DisplayScale * 0.5f;
        else renderGolf.transform.localScale = selectedSkin.Scale * 0.5f;
    }

    public void ComprarSkin(int index)
    {
        if (SaveDataManager.Instance.TryRemoveCoins(ResourcesManager.Instance.GetSkin(selectedName).CoinCost)) // Verifica si puede pagar
        {
            Debug.Log("Skin bought: " + names[index]);
            UnlockSkin(index);
            UpdateMoneyUI(); // Refresca la UI después de la compra

            if (insufficientMoneyText != null) insufficientMoneyText.text = ""; // Borra el mensaje de error solo si compró

            // Ocultar botón de compra e icono tras compra exitosa
            buyButton.gameObject.SetActive(false);
            coinIcon.SetActive(false);
        }
        else
        {
            Debug.Log("Not enough credits!");

            if (insufficientMoneyText != null)
            {
                insufficientMoneyText.text = "Insufficient money"; // Muestra el mensaje
                insufficientMoneyText.color = Color.red; // Ponerlo en rojo
            }
        }
    }
    public void UnlockSkin(int index)
    {
        if (index >= names.Length) return;

        SaveDataManager.Instance.AddItem(names[index]);
        //unlockedSkins[index] = true; // Mark as unlocked
        UpdateButtonAppearance(buttons[index].gameObject, true, ResourcesManager.Instance.GetSkin(names[index]).Icon);

        buyButton.gameObject.SetActive(false); // Hide buy button after purchase
        OnButtonClick(index);
    }

    void UpdateButtonAppearance(GameObject buttonObj, bool isUnlocked, Sprite skinIcon)
    {
        // Update main button background
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = isUnlocked ? unlockedSprite : lockedSprite;
        }

        // Enable button interaction
        Button buttonComponent = buttonObj.GetComponentInChildren<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.interactable = true;
        }

        // Find and update the icon
        var iconImage = buttonObj.GetComponentInChildren<HoverOverSelectable>().Icon;
        if (iconImage != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = skinIcon; // Set the correct icon
                iconImage.color = isUnlocked ? unlockedIconColor : lockedIconColor;
            }
        }
        else
        {
            Debug.LogWarning("Icon object not found in button prefab!");
        }
    }
}