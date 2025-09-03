using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;
using System.Threading.Tasks;
using System.Linq;
using Helpers.Linq;
using UnityEngine.Experimental.GlobalIllumination;

public class SessionScreenUI : MonoBehaviour
{
    public UIScreen screen;
    public Transform playerItemHolder;
    public Transform spectatorItemHolder;
    public GameObject spectatorHeader;
    public GameObject spectatorPanel;
    public TMP_Text sessionNameBanner;
    public TMP_Text sessionNameText;
    public TMP_Dropdown courseLengthSetting;
    public TMP_Dropdown holeTimeSetting;
    public TMP_Dropdown maxShotsSetting;
    public Toggle collisionCheck;
    public Toggle privateCheck;
    public TMP_Dropdown weightDropdown;
    public Putter playerPutter;

    public Button startGameButton;
    public Button readyButton;

    //RGB Sliders
    //public Slider slider_R, slider_G, slider_B;
    public GameObject golfBallRend;

    readonly Dictionary<PlayerRef, PlayerSessionItemUI> playerItems = new Dictionary<PlayerRef, PlayerSessionItemUI>();

    bool isUpdatingSession = false;

    private void Start()
    {
        weightDropdown.onValueChanged.AddListener(OnWeightChanged);
    }
    private void DefaultSkin()
    {
        PlayerSkin selectedSkin;

        if (PlayerObject.Local.Skin != null) {

            // Find the skin from ResourcesManager
            Debug.Log("Last Skin:" + SaveDataManager.Instance.LastCalledItem);
            selectedSkin = ResourcesManager.Instance.GetSkin(SaveDataManager.Instance.LastCalledItem);
            PlayerObject.Local.Rpc_SetSkin(selectedSkin.Name);
        }
        else
        {
            // Technically, this line can never be called since this Skin is never null
            Debug.Log("Last Skin:" + PlayerObject.Local.Skin);
            selectedSkin = ResourcesManager.Instance.GetSkin(PlayerObject.Local.Skin);
        }

        // Apply the mesh and materials to the example ball
        var renderGolf = InterfaceManager.Instance.sessionScreen.golfBallRend;
        renderGolf.GetComponent<MeshFilter>().mesh = selectedSkin.Mesh;
        renderGolf.GetComponent<MeshRenderer>().material = selectedSkin.Material;
        if (PlayerObject.Local != null) renderGolf.GetComponent<MeshRenderer>().material.color = PlayerObject.Local.Color;
        renderGolf.transform.localRotation = Quaternion.Euler(selectedSkin.Direction);

        if (selectedSkin.DisplayPosition != Vector3.zero) renderGolf.transform.localPosition = selectedSkin.DisplayPosition;
        else renderGolf.transform.localPosition = selectedSkin.DisplayPosition;

        if (selectedSkin.Name == "Golf") renderGolf.transform.localScale = selectedSkin.Scale * 0.7f;
        else if (selectedSkin.DisplayScale != Vector3.zero) renderGolf.transform.localScale = selectedSkin.DisplayScale * 0.5f;
        else renderGolf.transform.localScale = selectedSkin.Scale * 0.5f;
    }

    // UI hook
    public void AddSubscriptions()
    {
        PlayerRegistry.OnPlayerJoined += PlayerJoined;
        PlayerRegistry.OnPlayerLeft += PlayerLeft;
    }

    private void OnEnable()
    {
        PlayerRegistry.OnPlayerJoined -= PlayerJoined;
        PlayerRegistry.OnPlayerLeft -= PlayerLeft;
        PlayerRegistry.OnPlayerJoined += PlayerJoined;
        PlayerRegistry.OnPlayerLeft += PlayerLeft;
        PlayerObject.Local.Rpc_SetCheckMark(false); // Initialize the checkMark in false at begin.

        UpdateSessionConfig();
        StartCoroutine(SetSlidersDefault());
    }

    private void OnDisable()
    {
        playerItems.Clear();

        PlayerRegistry.OnPlayerJoined -= PlayerJoined;
        PlayerRegistry.OnPlayerLeft -= PlayerLeft;
    }

    private void Update()
    {
        if (GameManager.Instance?.Runner?.SessionInfo == true)
        {
            if (privateCheck.isOn != !GameManager.Instance.Runner.SessionInfo.IsVisible)
                UpdateSessionConfig();
        }
    }

    public void UpdateSessionConfig()
    {
        if (!isUpdatingSession && gameObject.activeInHierarchy)
        {
            isUpdatingSession = true;
            StartCoroutine(UpdateSessionConfigRoutine());
        }
    }

    public void OnReadyButtonClicked()
    {
        //PlayerRef localPlayer = GameManager.Instance.Runner.LocalPlayer;
        PlayerRef localPlayer = PlayerObject.Local.Ref;
    }
    IEnumerator UpdateSessionConfigRoutine()
    {
        if (!(GameManager.Instance?.Runner?.SessionInfo == true))
        {
            privateCheck.interactable = collisionCheck.interactable =
                maxShotsSetting.interactable = holeTimeSetting.interactable =
                courseLengthSetting.interactable = false;

            yield return new WaitUntil(() => GameManager.Instance?.Runner?.SessionInfo == true);
        }

        if (GameManager.Instance.Runner.IsServer)
            PlayerRegistry.ForEach(p =>
            {
                if (!playerItems.ContainsKey(p.Ref))
                {
                    CreatePlayerItem(p.Ref);
                    Debug.Log("" + playerItems[p.Ref].name);
                }
                PlayerSessionItemUI playerSessionItemUI = playerItems[p.Ref];
                if (playerSessionItemUI != null)
                {
                    //readyButton.onClick.AddListener(() => playerSessionItemUI.OnReadyClicked());
                    Debug.Log("OnReadyClicked");
                }
            }, true);

        sessionNameBanner.text = sessionNameText.text = GameManager.Instance.Runner.SessionInfo.Name;

        courseLengthSetting.value = GameManager.Instance.CourseLengthIndex;
        holeTimeSetting.value = GameManager.Instance.MaxTimeIndex;
        maxShotsSetting.value = GameManager.Instance.MaxStrokesIndex;
        privateCheck.isOn = !GameManager.Instance.Runner.SessionInfo.IsVisible;
        collisionCheck.isOn = GameManager.Instance.DoCollisions;

        startGameButton.gameObject.SetActive(GameManager.Instance.Runner.IsServer);
        readyButton.gameObject.SetActive(!GameManager.Instance.Runner.IsServer);

        privateCheck.interactable = collisionCheck.interactable =
            maxShotsSetting.interactable = holeTimeSetting.interactable =
            courseLengthSetting.interactable = GameManager.Instance.Runner.IsServer;

        isUpdatingSession = false;
    }

    IEnumerator SetSlidersDefault()
    {
        yield return new WaitUntil(() => PlayerObject.Local && PlayerObject.Local.Color != default);
        PlayerObject p = PlayerObject.Local;
        DefaultSkin();
        //SetRGB(p.Color.r, p.Color.g, p.Color.b);
    }
    void auixc(PlayerRef playerRef)
    {
        // Verificar si el jugador está en el diccionario
        if (playerItems.TryGetValue(playerRef, out PlayerSessionItemUI sessionItem))
        {
            // Acceder a checkMark dentro de PlayerSessionItemUI
            sessionItem.readyCheckMark.SetActive(true); // Si es un GameObject
        }
        else
        {
            Debug.LogError("No se encontró el jugador en playerItems.");
        }
    }

    public void PlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        CreatePlayerItem(player);
        /*if (playerItems.TryGetValue(player, out PlayerSessionItemUI playerSessionItemUI))
        {
            // Asegúrate de que el botón en el UI de sesión esté vinculado a la acción correcta
            readyButton.onClick.AddListener(() => playerSessionItemUI.OnReadyClicked());
        }*/
    }

    private void CreatePlayerItem(PlayerRef pRef)
    {
        if (!playerItems.ContainsKey(pRef))
        {
            if (GameManager.Instance.Runner.CanSpawn)
            {
                PlayerSessionItemUI item = GameManager.Instance.Runner.Spawn(
                    prefab: ResourcesManager.Instance.playerSessionItemUI,
                    inputAuthority: pRef);
                playerItems.Add(pRef, item);
            }
        }
        else
        {
            Debug.LogWarning($"{pRef} already in dictionary");
        }
    }

    public void PlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (playerItems.TryGetValue(player, out PlayerSessionItemUI item))
        {
            if (item)
            {
                Debug.Log($"Removing {nameof(PlayerSessionItemUI)} for {player}");
                runner.Despawn(item.Object);
            }
            else
            {
                Debug.Log($"{nameof(PlayerSessionItemUI)} for {player} was null.");
            }
            playerItems.Remove(player);
        }
        else
        {
            Debug.LogWarning($"{player} not found");
        }
    }
    /*
    public void SetRGB32(byte r, byte g, byte b)
    {
        slider_R.value = r;
        slider_G.value = g;
        slider_B.value = b;
        EditRGB();
    }

    public void SetRGB(float r, float g, float b)
    {
        slider_R.value = r * 255;
        slider_G.value = g * 255;
        slider_B.value = b * 255;
        EditRGB();
    }

    public void ClearRGB()
    {
        golfBallRend.material.color = Color.white;
    }

    public void EditRGB()
    {
        golfBallRend.material.color = new Color32((byte)slider_R.value, (byte)slider_G.value, (byte)slider_B.value, 255);
    }

    public void ApplyColorChange()
    {
        PlayerObject.Local.Rpc_SetColor(new Color32((byte)slider_R.value, (byte)slider_G.value, (byte)slider_B.value, 255));
    }*/

    private void SetupWeightDropdown()
    {
        weightDropdown.ClearOptions();
        weightDropdown.AddOptions(new List<string> { "Light", "Medium", "Heavy" });
        weightDropdown.value = 1;
        weightDropdown.onValueChanged.AddListener(OnWeightChanged);
    }

    private void OnWeightChanged(int selectedIndex)
    {
        playerPutter.SetWeight((WeightType)selectedIndex);
        AdjustPlayerSize((WeightType)selectedIndex);
    }

    private void AdjustPlayerSize(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Light:
                playerPutter.transform.localScale = Vector3.one * 0.5f;
                break;

            case WeightType.Medium:
                playerPutter.transform.localScale = Vector3.one;
                break;

            case WeightType.Heavy:
                playerPutter.transform.localScale = Vector3.one * 1.5f;
                break;
        }
    }

    public void StartGame()
    {
        readyButton.onClick.RemoveAllListeners();
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        if (PlayerRegistry.CountPlayers > 0)
        {
            GameManager.State.Server_SetState(GameState.EGameState.Loading);
        }
    }

    public void EditSession()
    {
        UIScreen.Focus(InterfaceManager.Instance.sessionSetupScreen);
    }

    public void ToggleSpectate()
    {
        PlayerObject.Local.Rpc_ToggleSpectate();
    }

    public void Leave()
    {
        StartCoroutine(LeaveRoutine());
    }

    IEnumerator LeaveRoutine()
    {
        Task task = Matchmaker.Instance.Runner.Shutdown();
        while (!task.IsCompleted)
        {
            yield return null;
        }
        UIScreen.BackToInitial();
    }
}