using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class PlayerInputBehaviour : Fusion.Behaviour, INetworkRunnerCallbacks
{
	float accumulatedDelta = 0;
	
	// IMPROVED INPUT: Más datos para mejor sincronización
	private Vector3 lastCameraPosition;
	private Vector3 lastPlayerPosition;
	private bool wasGrounded = false;

	private void Update()
	{
		accumulatedDelta += Input.GetAxis("Mouse Y");
	}

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		if (PlayerObject.Local == null || PlayerObject.Local.Controller == null) return;
		if (UIScreen.activeScreen != InterfaceManager.Instance.hud) return;
		if (GameManager.State.Current != GameState.EGameState.Intro
			&& GameManager.State.Current != GameState.EGameState.Game) return;

		PlayerInput fwInput = new PlayerInput();

		if (fwInput.isDragging = Input.GetMouseButton(0))
		{
			fwInput.dragDelta = accumulatedDelta;
		}

		Vector3 forward = CameraController.Instance.transform.forward;
		fwInput.yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
		
		// IMPROVED INPUT: Añadir más datos para mejor sincronización
		fwInput.playerPosition = PlayerObject.Local.Controller.transform.position;
		fwInput.playerRotation = PlayerObject.Local.Controller.transform.rotation;
		fwInput.playerVelocity = PlayerObject.Local.Controller.rb.linearVelocity;
		fwInput.cameraPosition = CameraController.Instance.transform.position;
		fwInput.cameraRotation = CameraController.Instance.transform.rotation;
		
		// Detectar cambios significativos para forzar updates
		fwInput.hasSignificantMovement = 
			Vector3.Distance(fwInput.playerPosition, lastPlayerPosition) > 0.01f ||
			Vector3.Distance(fwInput.cameraPosition, lastCameraPosition) > 0.01f ||
			fwInput.playerVelocity.magnitude > 0.1f;
			
		// Input de teclado para movimiento adicional si es necesario
		fwInput.horizontalInput = Input.GetAxis("Horizontal");
		fwInput.verticalInput = Input.GetAxis("Vertical");
		
		// Datos de estado del jugador
		if (PlayerObject.Local.Controller.TryGetComponent<Putter>(out Putter putter))
		{
			fwInput.isGrounded = Physics.OverlapSphere(
				putter.transform.position, 
				putter.collider.radius * 1.05f,
				LayerMask.GetMask("Default"), 
				QueryTriggerInteraction.Ignore).Length > 0;
				
			fwInput.groundedStateChanged = fwInput.isGrounded != wasGrounded;
			wasGrounded = fwInput.isGrounded;
		}
		
		// Timestamp para interpolación
		fwInput.timestamp = runner.SimulationTime;
		
		input.Set(fwInput);

		// Cache para próximo frame
		lastCameraPosition = fwInput.cameraPosition;
		lastPlayerPosition = fwInput.playerPosition;
		accumulatedDelta = 0;
	}

	#region INetworkRunnerCallbacks
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnDisconnectedFromServer(NetworkRunner runner) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}

public struct PlayerInput : INetworkInput
{
	public bool isDragging;
	public float dragDelta;
	public Angle yaw;
	
	// IMPROVED INPUT: Datos adicionales para mejor sincronización
	public Vector3 playerPosition;
	public Quaternion playerRotation;
	public Vector3 playerVelocity;
	public Vector3 cameraPosition;
	public Quaternion cameraRotation;
	public bool hasSignificantMovement;
	public float horizontalInput;
	public float verticalInput;
	public bool isGrounded;
	public bool groundedStateChanged;
	public float timestamp;
}