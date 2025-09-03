using System.Collections.Generic;
using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    public static SaveDataManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persistent across scenes

            InitializeDefaults(); // Ensures default values exist on the first run
        }
        else Destroy(gameObject); // Ensures only one instance exists
    }

    // Save & Load Player Nickname
    private const string NicknameKey = "Nickname";
    public string PlayerNickname
    {
        get => PlayerPrefs.GetString(NicknameKey, "Jugador");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                PlayerPrefs.DeleteKey(NicknameKey);
            else
                PlayerPrefs.SetString(NicknameKey, value);
        }
    }

    public bool HasNickname => PlayerPrefs.HasKey(NicknameKey);

    // Save & Load Coins
    private const string CoinsKey = "Coins";
    public int Coins
    {
        get => PlayerPrefs.GetInt(CoinsKey, 100);
        private set => PlayerPrefs.SetInt(CoinsKey, value);
    }

    // Adds coins
    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            Coins += amount;
            Save();
            Debug.Log($"Added {amount} coins. New balance: {Coins}");
        }
    }

    // Tries to remove coins, returns true if successful, false otherwise
    public bool TryRemoveCoins(int amount)
    {
        if (amount > 0 && Coins >= amount)
        {
            Coins -= amount;
            Save();
            Debug.Log($"Removed {amount} coins. New balance: {Coins}");
            return true;
        }

        Debug.Log($"Not enough coins to remove {amount}. Current balance: {Coins}");
        return false;
    }


    // Save & Load Owned Items (List of Strings)
    private const string ItemsKey = "OwnedSkins";

    public List<string> OwnedItems
    {
        get
        {
            string json = PlayerPrefs.GetString(ItemsKey, "[]");
            List<string> items = JsonUtility.FromJson<ItemList>(json)?.items ?? new List<string>();

            // If the list is empty, give the default item "Golf"
            if (items.Count == 0)
            {
                items.Add("Golf");
                OwnedItems = items; // Save the updated list
            }

            return items;
        }
        set
        {
            string json = JsonUtility.ToJson(new ItemList(value));
            PlayerPrefs.SetString(ItemsKey, json);
        }
    }

    // Adds a new item to OwnedItems if it's not already owned
    public void AddItem(string newItem)
    {
        List<string> items = OwnedItems; // Get current list

        if (!items.Contains(newItem)) // Avoid duplicates
        {
            items.Add(newItem);
            OwnedItems = items; // Save updated list
            Save(); // Save PlayerPrefs
            Debug.Log($"Added item: {newItem}. New owned items: {string.Join(", ", OwnedItems)}");
        }
        else
        {
            Debug.Log($"Item {newItem} is already owned.");
        }
    }
    // Checks if the player owns a specific item
    public bool HasItem(string itemName)
    {
        return OwnedItems.Contains(itemName);
    }

    private const string LastItemKey = "LastCalledItem";

    public string LastCalledItem
    {
        get => PlayerPrefs.GetString(LastItemKey, "Golf"); // Default to "Golf"
        private set => PlayerPrefs.SetString(LastItemKey, value);
    }

    public void SetLastCalledItem(string itemName)
    {
        if (HasItem(itemName)) // Only allow owned items to be set
        {
            LastCalledItem = itemName;
            Save();
            Debug.Log($"Last called item set to: {LastCalledItem}");
        }
        else
        {
            Debug.LogWarning($"Cannot set {itemName} as last called item (not owned).");
        }
    }

    // Ensures default values exist if the game is launched for the first time
    private void InitializeDefaults()
    {
        if (!PlayerPrefs.HasKey(ItemsKey))
        {
            OwnedItems = new List<string> { "Golf" }; // Assign default item
        }

        if (!PlayerPrefs.HasKey(LastItemKey))
        {
            LastCalledItem = "Golf"; // Set default last called item
        }

        // Properly print the list in the console
        Debug.Log($"Owned Items: {string.Join(", ", OwnedItems)}");
        Debug.Log($"Current balance: {Coins}");
        Debug.Log($"Last Called Item: {LastCalledItem}");

        PlayerPrefs.Save();
    }

    // Call this to save PlayerPrefs data
    public void Save()
    {
        PlayerPrefs.Save();
    }

    // Helper class for JSON serialization
    [System.Serializable]
    private class ItemList
    {
        public List<string> items;
        public ItemList(List<string> list) => items = list;
    }
}