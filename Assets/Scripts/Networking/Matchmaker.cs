using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Fusion;
using Fusion.Sockets;
using System;

public class Matchmaker : MonoBehaviour, INetworkRunnerCallbacks
{
	public static Matchmaker Instance { get; private set; }

	public UIScreen sessionScreen;
	[SerializeField, ScenePath] string gameScene;
	public NetworkRunner runnerPrefab;
	public NetworkObject managerPrefab;
	public SessionItemUI sessionItemPrefab;
	public Transform sessionItemHolder;
	public UnityEvent onTryJoinLobby;
	public UnityEvent onLobbyConnected;
	public UnityEvent onCloseLobby_Before;
	public UnityEvent onCloseLobby_After;

	readonly List<SessionItemUI> sessionItems = new List<SessionItemUI>();
	public NetworkRunner Runner { get; private set; }

	bool _private = false;
	string _roomCode = null;
	
	// PLAYER LIMIT: Constante para máximo de jugadores
	public const int MAX_PLAYERS = 8;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;

		Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion = PlayerPrefs.GetString("regionPref", "");
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	public void ClearBrowser()
	{
		sessionItems.ForEach(item => Destroy(item.gameObject));
		sessionItems.Clear();
	}

	public void TryHostSession(System.Action successCallback = null)
	{
		StartCoroutine(HostSessionRoutine(successCallback));
	}

	IEnumerator HostSessionRoutine(System.Action successCallback)
	{
		if (!Runner)
		{
			Runner = Instantiate(runnerPrefab);
			Runner.GetComponent<NetworkEvents>().PlayerJoined.AddListener((runner, player) =>
			{
				if (runner.IsServer && runner.LocalPlayer == player)
				{
					runner.Spawn(managerPrefab);
				}
			});
			Runner.AddCallbacks(this);
		}
		
		string code = string.IsNullOrWhiteSpace(_roomCode) ? RoomCode.Create(6) : _roomCode;

		Task<StartGameResult> task = Runner.StartGame(new StartGameArgs()
		{
			GameMode = GameMode.Host,
			SessionName = code,
			SceneManager = Runner.GetComponent<INetworkSceneManager>(),
			PlayerCount = MAX_PLAYERS, // Límite de jugadores establecido
			SessionProperties = new Dictionary<string, SessionProperty>()
			{
				{ "MaxPlayers", MAX_PLAYERS }
			}
		});
		while (!task.IsCompleted)
		{
			yield return null;
		}
		StartGameResult result = task.Result;

		if (result.Ok)
		{
			// El MaxPlayers se establece automáticamente por Fusion basado en PlayerCount
			// Solo configuramos la visibilidad
			Runner.SessionInfo.IsVisible = !_private;
			
			Debug.Log($"Session created with max {MAX_PLAYERS} players. Private: {_private}. Session is {(Runner.SessionInfo.IsVisible ? "public" : "private")}");
			
			if (successCallback != null)
				successCallback.Invoke();
			else
				Runner.LoadScene(gameScene);
		}
		else
		{
			DisconnectUI.OnShutdown(result.ShutdownReason);
		}
	}

	public void SetPrivate(bool value)
	{
		_private = value;
	}

	public void SetRoomCode(string code)
	{
		_roomCode = code;
	}

	public void TryJoinSessionUI()
	{
		TryJoinSession(_roomCode);
	}

	public void TryJoinSession(string sessionCode, System.Action successCallback = null)
	{
		StartCoroutine(JoinSessionRoutine(sessionCode, successCallback));
	}

	IEnumerator JoinSessionRoutine(string sessionCode, System.Action successCallback)
	{
		if (Runner) Runner.Shutdown();
		Runner = Instantiate(runnerPrefab);

		Task<StartGameResult> task = Runner.StartGame(new StartGameArgs()
			{
				GameMode = GameMode.Client,
				SessionName = sessionCode,
				SceneManager = Runner.GetComponent<INetworkSceneManager>(),
				EnableClientSessionCreation = false,
			});
		while (!task.IsCompleted)
		{
			yield return null;
		}
		StartGameResult result = task.Result;

		if (result.Ok)
		{
			Debug.Log($"Joined session. Current players: {Runner.ActivePlayers.Count()}/{MAX_PLAYERS}");
			
			if (successCallback != null)
				successCallback.Invoke();
		}
		else
		{
			DisconnectUI.OnShutdown(result.ShutdownReason);
		}
	}

	public void TryJoinLobby()
	{
		StartCoroutine(JoinLobbyRoutine());
	}

	IEnumerator JoinLobbyRoutine()
	{
		onTryJoinLobby?.Invoke();
		Runner = Instantiate(runnerPrefab);
		Runner.AddCallbacks(this);
		Task<StartGameResult> task = Runner.JoinSessionLobby(SessionLobby.ClientServer);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		StartGameResult result = task.Result;
		
		if (result.Ok)
		{
			Debug.Log("Connected to lobby.");
			onLobbyConnected?.Invoke();
		}
		else
		{
			DisconnectUI.OnShutdown(result.ShutdownReason);
		}
	}

	public void CloseLobby()
	{
		StartCoroutine(CloseLobbyRoutine());
	}

	IEnumerator CloseLobbyRoutine()
	{
		onCloseLobby_Before?.Invoke();
		Task task = Runner.Shutdown();
		while (!task.IsCompleted)
		{
			yield return null;
		}
		onCloseLobby_After?.Invoke();
		Runner = null;
	}

	SessionItemUI GetSessionItem(int i)
	{
		return sessionItems.ElementAtOrDefault(i) ?? TrackItem(Instantiate(sessionItemPrefab, sessionItemHolder));
	}

	SessionItemUI TrackItem(SessionItemUI item)
	{
		sessionItems.Add(item);
		return item;
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
		Debug.Log("SessionListUpdated:\n" + string.Join("\n", sessionList.Select(s => $"{s.Name} [{s.PlayerCount}/{s.MaxPlayers}] - {(s.IsOpen ? "Open" : "Closed")}")));

		int i = 0;
		for (; i < sessionList.Count; i++)
		{
			SessionInfo sessionInfo = sessionList[i];
			
			// Filtrar sesiones que estén llenas o que excedan nuestro límite
			bool canJoin = sessionInfo.IsOpen && 
						   sessionInfo.PlayerCount < MAX_PLAYERS && 
						   sessionInfo.PlayerCount < sessionInfo.MaxPlayers;
			
			GetSessionItem(i).Init(sessionInfo.Name, sessionInfo.PlayerCount, sessionInfo.MaxPlayers, canJoin);
		}

		for (; i < sessionItems.Count; i++)
		{
			sessionItems[i].Disable();
		}
	}

	// PLAYER LIMIT: Callback para verificar cuando un jugador se une
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		int currentPlayers = runner.ActivePlayers.Count();
		int maxAllowed = runner.SessionInfo.MaxPlayers;
		
		Debug.Log($"Player {player} joined. Current players: {currentPlayers}/{maxAllowed} (our limit: {MAX_PLAYERS})");
		
		// Usar el menor entre nuestro límite y el límite de la sesión
		int effectiveLimit = Mathf.Min(MAX_PLAYERS, maxAllowed);
		
		if (currentPlayers >= effectiveLimit)
		{
			Debug.Log($"Maximum player limit reached! ({effectiveLimit})");
			// Cerrar la sesión para nuevos jugadores
			if (runner.IsServer)
			{
				runner.SessionInfo.IsOpen = false;
				Debug.Log("Session closed to new players");
			}
		}
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		int currentPlayers = runner.ActivePlayers.Count();
		int maxAllowed = runner.SessionInfo.MaxPlayers;
		int effectiveLimit = Mathf.Min(MAX_PLAYERS, maxAllowed);
		
		Debug.Log($"Player {player} left. Current players: {currentPlayers}/{effectiveLimit}");
		
		// Reabrir la sesión si hay espacio disponible
		if (currentPlayers < effectiveLimit && runner.IsServer)
		{
			runner.SessionInfo.IsOpen = true;
			Debug.Log("Session reopened to new players");
		}
	}

	public bool CanAcceptMorePlayers()
	{
		if (Runner == null) return true;
		
		int currentPlayers = Runner.ActivePlayers.Count();
		int maxAllowed = Runner.SessionInfo.MaxPlayers;
		int effectiveLimit = Mathf.Min(MAX_PLAYERS, maxAllowed);
		
		return currentPlayers < effectiveLimit;
	}

	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		Runner = null;
		if (shutdownReason != ShutdownReason.Ok)
		{
			DisconnectUI.OnShutdown(shutdownReason);
		}
	}

	#region INetworkRunnerCallbacks
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnDisconnectedFromServer(NetworkRunner runner) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    #endregion
}