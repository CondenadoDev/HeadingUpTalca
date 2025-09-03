using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance { get; private set; }

    public Putter playerControllerPrefab;
    public PlayerScoreboardUI playerScoreUI;
    public ScoreItem scoreItem;
    public PlayerSessionItemUI playerSessionItemUI;
    public WorldNickname worldNicknamePrefab;
    public CrownHolder crownPrefab;
    public GameObject splashEffect;
    public Level[] levels;

    public List<PlayerSkin> skins;
    public List<GameModeInfo> gameModes; // List of all game modes

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Function to retrieve a skin
    public PlayerSkin GetSkin(string skinID)
    {
        var skin = skins.Find(x => x.Name == skinID);
        if (skin != null)
        {
            return skin;
        }
        return null;
    }
}