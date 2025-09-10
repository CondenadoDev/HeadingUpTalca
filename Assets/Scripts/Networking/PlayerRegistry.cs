using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Helpers.Linq;
using Fusion.Sockets;
using System;

public class PlayerRegistry : NetworkBehaviour, INetworkRunnerCallbacks
{
	public const byte CAPACITY = 8; // Reducido de 10 a 8
	public static PlayerRegistry Instance { get; private set; }
	public static int CountAll => Instance.Object.IsValid ? Instance.ObjectByRef.Count : 0;
	public static int CountPlayers => Instance.Object.IsValid ? CountWhere(p => !p.IsSpectator) : 0;
	public static int CountSpectators => Instance.Object.IsValid ? CountWhere(p => p.IsSpectator) : 0;
	public static event System.Action<NetworkRunner, PlayerRef> OnPlayerJoined;
	public static event System.Action<NetworkRunner, PlayerRef> OnPlayerLeft;

	// OPTIMIZATION: Cache collections para evitar LINQ repetido
	private static List<PlayerObject> cachedEveryone = new List<PlayerObject>(8); // Pre-size para 8 jugadores
	private static List<PlayerObject> cachedPlayers = new List<PlayerObject>(8);
	private static bool cacheValid = false;
	private static int lastFrameUpdated = -1;

	public static IEnumerable<PlayerObject> Everyone 
	{
		get
		{
			UpdateCacheIfNeeded();
			return cachedEveryone;
		}
	}
	
	public static IEnumerable<PlayerObject> Players 
	{
		get
		{
			UpdateCacheIfNeeded();
			return cachedPlayers;
		}
	}

	private static void UpdateCacheIfNeeded()
	{
		if (Time.frameCount == lastFrameUpdated && cacheValid) return;
		
		cachedEveryone.Clear();
		cachedPlayers.Clear();
		
		if (Instance?.Object?.IsValid == true)
		{
			foreach (var kvp in Instance.ObjectByRef)
			{
				if (kvp.Value != null)
				{
					cachedEveryone.Add(kvp.Value);
					if (!kvp.Value.IsSpectator)
						cachedPlayers.Add(kvp.Value);
				}
			}
		}
		
		cacheValid = true;
		lastFrameUpdated = Time.frameCount;
	}

	private static void InvalidateCache()
	{
		cacheValid = false;
	}

	[Networked, Capacity(CAPACITY)]
	NetworkDictionary<PlayerRef, PlayerObject> ObjectByRef { get; }

	void Awake()
    {
		Instance = this;
    }

	public override void Spawned()
	{
		Instance = this;
		Runner.AddCallbacks(this);
		DontDestroyOnLoad(gameObject);
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		Instance = null;
		runner.RemoveCallbacks(this);
		OnPlayerJoined = OnPlayerLeft = null;
		InvalidateCache();
	}

	bool GetAvailable(out byte index)
	{
		if (ObjectByRef.Count == 0)
		{
			index = 0;
			return true;
		}
		else if (ObjectByRef.Count == CAPACITY)
		{
			index = default;
			Debug.LogWarning($"Maximum player capacity ({CAPACITY}) reached!");
			return false;
		}

		byte[] indices = ObjectByRef.OrderBy(kvp => kvp.Value.Index).Select(kvp => kvp.Value.Index).ToArray();

		for (int i = 0; i < indices.Length - 1; i++)
		{
			if (indices[i + 1] > indices[i] + 1)
			{
				index = (byte)(indices[i] + 1);
				return true;
			}
		}

		index = (byte)(indices[indices.Length - 1] + 1);
		return true;
	}

	public static void Server_Add(NetworkRunner runner, PlayerRef pRef, PlayerObject pObj)
	{
		Debug.Assert(runner.IsServer);

		// Verificación adicional de capacidad
		if (Instance.ObjectByRef.Count >= CAPACITY)
		{
			Debug.LogError($"Cannot add player {pRef}: Maximum capacity ({CAPACITY}) reached!");
			return;
		}

		if (Instance.GetAvailable(out byte index))
		{
			Instance.ObjectByRef.Add(pRef, pObj);
			DontDestroyOnLoad(pObj.gameObject);
			pObj.Server_Init(pRef, index);
			InvalidateCache();
			Debug.Log($"Player {pRef} added. Total players: {Instance.ObjectByRef.Count}/{CAPACITY}");
		}
		else
		{
			Debug.LogWarning($"Unable to register player {pRef}", pObj);
		}
	}
	
	public static void PlayerJoined(PlayerRef player)
	{
		InvalidateCache();
		OnPlayerJoined?.Invoke(Instance.Runner, player);
	}

	public static void Server_Remove(NetworkRunner runner, PlayerRef pRef)
	{
		Debug.Assert(runner.IsServer);
		Debug.Assert(pRef.IsRealPlayer);

		Debug.Log($"Removing player {pRef}. Players before removal: {Instance.ObjectByRef.Count}/{CAPACITY}");

		if (Instance.ObjectByRef.Remove(pRef) == false)
		{
			Debug.LogWarning("Could not remove player from registry");
		}
		else
		{
			Debug.Log($"Player {pRef} removed. Remaining players: {Instance.ObjectByRef.Count}/{CAPACITY}");
		}
		InvalidateCache();
	}

	public static bool HasPlayer(PlayerRef pRef)
	{
		return Instance.ObjectByRef.ContainsKey(pRef);
	}

	public static PlayerObject GetPlayer(PlayerRef pRef)
	{
		if (HasPlayer(pRef))
			return Instance.ObjectByRef.Get(pRef);
		return null;
	}

	public static bool IsHost(PlayerRef pRef)
	{
		return GetPlayer(pRef)?.Index == 0;
	}

	public static bool CanJoinAsPlayer()
	{
		return CountPlayers < CAPACITY;
	}

	#region Utility Methods

	public static IEnumerable<PlayerObject> Where(System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		return (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p));
	}

	public static PlayerObject First(System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		return (includeSpectators ? Everyone : Players).First(p => match.Invoke(p));
	}

	public static void ForEach(System.Action<PlayerObject> action, bool includeSpectators = false)
	{
		(includeSpectators ? Everyone : Players).ForEach(p => action.Invoke(p));
	}

	public static void ForEach(System.Action<PlayerObject, int> action, bool includeSpectators = false)
	{
		int i = 0;
		(includeSpectators ? Everyone : Players).ForEach(p => action.Invoke(p, i++));
	}

	public static void ForEachWhere(System.Predicate<PlayerObject> match, System.Action<PlayerObject> action, bool includeSpectators = false)
	{
		(includeSpectators ? Everyone : Players).Where(p => match.Invoke(p)).ForEach(p => action.Invoke(p));
	}

	public static int CountWhere(System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		return (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p)).Count();
	}

	public static bool Any(System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		if (Instance == null) return false;
		return (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p)).Count() > 0;
	}

	public static bool All(System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		return (includeSpectators ? Everyone : Players).Where(p => !match.Invoke(p)).Count() == 0;
	}

	public static IOrderedEnumerable<PlayerObject> OrderAsc<T>(
		System.Func<PlayerObject, T> selector,
		System.Predicate<PlayerObject> match = null,
		bool includeSpectators = false) where T : System.IComparable<T>
	{
		if (match != null) return (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p)).OrderBy(selector);
		return (includeSpectators ? Everyone : Players).OrderBy(selector);
	}

	public static IOrderedEnumerable<PlayerObject> OrderDesc<T>(
		System.Func<PlayerObject, T> selector, 
		System.Predicate<PlayerObject> match = null, 
		bool includeSpectators = false) where T : System.IComparable<T>
	{
		if (match != null) return (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p)).OrderByDescending(selector);
		return (includeSpectators ? Everyone : Players).OrderByDescending(selector);
	}

	public static PlayerObject Next(PlayerObject current, bool includeSpectators = false)
	{
		IEnumerable<PlayerObject> collection = (includeSpectators ? Everyone : Players);
		int index = collection.FirstIndex(current);
		if (index == -1) return current;
		int length = collection.Count();
		return collection.ElementAt((int)Mathf.Repeat(index + 1, length));
	}

	public static PlayerObject NextWhere(PlayerObject current, System.Predicate<PlayerObject> match, bool includeSpectators = false)
	{
		IEnumerable<PlayerObject> collection = (includeSpectators ? Everyone : Players).Where(p => match.Invoke(p));
		int index = collection.FirstIndex(current);
		if (index == -1) return current;
		int length = collection.Count();
		return collection.ElementAt((int)Mathf.Repeat(index + 1, length));
	}

    public static PlayerObject PreviousWhere(PlayerObject current, Func<PlayerObject, bool> predicate)
    {
        var allPlayers = (IList<PlayerObject>)(Everyone?.ToList() ?? new List<PlayerObject>());
        int index = allPlayers.IndexOf(current);
        if (index == -1) return null;

        for (int i = index - 1; i >= 0; i--)
        {
            if (predicate(allPlayers[i]))
                return allPlayers[i];
        }

        for (int i = allPlayers.Count - 1; i > index; i--)
        {
            if (predicate(allPlayers[i]))
                return allPlayers[i];
        }

        return null;
    }

    #endregion

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer) Server_Remove(runner, player);
		InvalidateCache();
		OnPlayerLeft?.Invoke(Runner, player);
	}

	#region INetworkRunnerCallbacks
	void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
	void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
	void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
	void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
	void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
	void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
	void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
	void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}