using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class PlayerSessionItemUI : NetworkBehaviour
{
	public TMP_Text usernameText;
	public Image avatar, border;
	public GameObject leaderObj;
	public GameObject readyCheckMark;

	bool isHierarchyChanging = false;
	public bool isReady = false;
	PlayerObject _player = null;
	PlayerObject Player
	{
		get
		{
			if (_player == null) _player = PlayerRegistry.GetPlayer(Object.InputAuthority);
			return _player;
		}
	}

	private void Start()
    {
		readyCheckMark.SetActive(false);
    }
	private void Update(){
	}

	/*public void OnReadyClicked()
    {
        isReady = !isReady;
        readyCheckMark.SetActive(isReady);
    }*/
	public void OnReadyClicked()
    {
        if (Object.HasInputAuthority)  // Verificar si el cliente tiene autoridad sobre este objeto
        {
            isReady = !isReady;  // Cambiar el estado "Listo"
            readyCheckMark.SetActive(isReady);  // Actualizar el icono
        }
    }

    // RPC para actualizar el estado de "Listo" a todos los jugadores
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateReadyStatus(bool readyStatus)
    {
        isReady = readyStatus;
        readyCheckMark.SetActive(isReady);  // Actualizar el icono en todos los clientes
    }
	
	private void OnBeforeTransformParentChanged()
	{
		isHierarchyChanging = true;
	}

	private void OnTransformParentChanged()
	{
		isHierarchyChanging = false;
	}

	private void OnDisable()
	{
		if (!isHierarchyChanging && Runner?.IsRunning == true) Runner.Despawn(Object);
	}

	public void Init()
    {
		Player.OnStatChanged += UpdateStats;
		Player.OnSpectatorChanged += SetPosition;
		UpdateStats();
	}

	public override void Spawned()
	{
		Init();

		SetPosition();
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		if (Player)
		{
			Player.OnStatChanged -= UpdateStats;
			Player.OnSpectatorChanged -= SetPosition;
		}
	}

	void UpdateStats()
	{
		SetUsername(Player.Nickname);
		SetColour(Player.Color);
		SetCheckMark();
	}

	void SetPosition()
	{
		if (Player)
		{
			transform.SetParent(
				Player.IsSpectator
				? InterfaceManager.Instance.sessionScreen.spectatorItemHolder
				: InterfaceManager.Instance.sessionScreen.playerItemHolder,
				false);
		}
		else
		{
			transform.SetParent(InterfaceManager.Instance.sessionScreen.playerItemHolder, false);
		}

		bool anySpectators = InterfaceManager.Instance.sessionScreen.spectatorItemHolder.childCount > 0;
		InterfaceManager.Instance.sessionScreen.spectatorHeader.SetActive(anySpectators);
		InterfaceManager.Instance.sessionScreen.spectatorPanel.gameObject.SetActive(anySpectators);
	}

	public void SetUsername(string name)
    {
        usernameText.text = name;
    }

    public void SetColour(Color col)
    {
        usernameText.color = avatar.color = border.color = col;
    }

	public void SetCheckMark()
	{
		bool val = readyCheckMark.gameObject.activeSelf;
		readyCheckMark.gameObject.SetActive(!val);
	}
    public void SetLeader(bool set)
    {
        leaderObj.SetActive(set);
    }
}
