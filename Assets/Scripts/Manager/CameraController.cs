using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Helpers.Math;
using System.Linq;

public class CameraController : MonoBehaviour
{
    [field: SerializeField] public ShakeBehaviour Shake { get; private set; }
    [SerializeField] ParticleSystem speedLines;
    ICanControlCamera con = null;
    public static ICanControlCamera Controller => Instance.con;

    [SerializeField] AnimationCurve speedLinesSpeedCurve = AnimationCurve.Constant(0, 1, 1);
    public float minDistance = 0.3f;
    public float defaultDistance = 1f;
    public float maxDistance = 3f;
    public float scrollRate = 0.1f;
    public float distanceLambda = 7f;
    public float lookHeightOffset = 0.2f;

    public float defaultPitch = 20;
    public float maxPitch = 60;

    [SerializeField, ReadOnly] float pitch = 0;
    [SerializeField, ReadOnly] float yaw = 0;

    float targetDistance;
    float distance;
    Vector3 cachedPosition;

    // OPTIMIZATION: Cache para evitar búsquedas repetidas de jugadores
    private static PlayerObject cachedNextPlayer;
    private static PlayerObject cachedPreviousPlayer;
    private static int lastPlayerSearchFrame = -1;
    
    // OPTIMIZATION: Reduce frequency of expensive operations
    private int updateCounter = 0;
    private const int CAMERA_UPDATE_INTERVAL = 2; // Every 2 frames instead of every frame
    
    // OPTIMIZATION: Cache para verificaciones costosas
    private bool lastRunnerState = false;
    private int lastPlayerCount = 0;

    public static CameraController Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
        targetDistance = distance = defaultDistance;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnValidate()
    {
        pitch = defaultPitch;
        transform.localEulerAngles = new Vector3(pitch, yaw, 0);
    }

    private void OnDestroy()
    {
        con = null;
        Instance = null;
        Cursor.lockState = CursorLockMode.None;
    }

    public static void AssignControl(ICanControlCamera controller)
    {
        Instance.con = controller;

        if (controller == null)
        {
            Instance.speedLines.Stop();
            HUD.Instance.SpectatingObj.SetActive(false);
        }
        else if (controller is NetworkBehaviour nbController)
        {
            HUD.Instance.SpectatingObj.SetActive(!nbController.Object.HasInputAuthority);
        }
    }

    public static bool HasControl(ICanControlCamera controller)
    {
        return Controller?.Equals(controller) == true;
    }

    public static Vector3 GetCameraForward()
    {
        return Instance ? Instance.transform.forward : Vector3.zero;
    }

    private void LateUpdate()
    {
        // OPTIMIZATION: Reduce frequency of expensive camera updates
        updateCounter++;
        if (updateCounter < CAMERA_UPDATE_INTERVAL) return;
        updateCounter = 0;

        // OPTIMIZATION: Cache expensive state checks
        bool currentRunnerState = GameManager.Instance?.Runner?.IsRunning == true;
        int currentPlayerCount = PlayerRegistry.CountPlayers;
        
        if (!currentRunnerState || currentPlayerCount == 0) 
        {
            lastRunnerState = currentRunnerState;
            lastPlayerCount = currentPlayerCount;
            return;
        }

        // Only update player search if conditions changed
        if (con == null && currentPlayerCount > 0 && 
            (GameManager.State.Current == GameState.EGameState.Intro || GameManager.State.Current == GameState.EGameState.Game))
        {
            // OPTIMIZATION: Only search if state actually changed
            if (!lastRunnerState || lastPlayerCount != currentPlayerCount)
            {
                var playerWithController = SafeFirstPlayer(p => p.Controller != null);
                if (playerWithController != null)
                    AssignControl(playerWithController.Controller);
            }
        }

        lastRunnerState = currentRunnerState;
        lastPlayerCount = currentPlayerCount;

        if (con == null) return;

        if (con is NetworkBehaviour nb)
        {
            if (nb.Object.HasInputAuthority == false)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // OPTIMIZATION: Cache player searches per frame
                    if (Time.frameCount != lastPlayerSearchFrame)
                    {
                        var currentPlayer = SafeFirstPlayer(p => p.Controller == nb);
                        if (currentPlayer != null)
                        {
                            cachedNextPlayer = PlayerRegistry.NextWhere(currentPlayer, p => p.Controller);
                        }
                        lastPlayerSearchFrame = Time.frameCount;
                    }
                    
                    if (cachedNextPlayer?.Controller != null)
                    {
                        AssignControl(cachedNextPlayer.Controller);
                        if (con == null) return;
                    }
                }
                
                if (Input.GetMouseButtonDown(1))
                {
                    print("anterior");
                    if (Time.frameCount != lastPlayerSearchFrame)
                    {
                        var currentPlayer = SafeFirstPlayer(p => p.Controller == nb);
                        if (currentPlayer != null)
                        {
                            cachedPreviousPlayer = PlayerRegistry.PreviousWhere(currentPlayer, p => p.Controller != null);
                        }
                        lastPlayerSearchFrame = Time.frameCount;
                    }
                    
                    if (cachedPreviousPlayer?.Controller != null)
                    {
                        AssignControl(cachedPreviousPlayer.Controller);
                        if (con == null) return;
                    }
                }
            }
        }

        distance = MathUtil.Damp(distance, targetDistance, distanceLambda, Time.deltaTime);

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            targetDistance -= scroll * scrollRate * targetDistance.Remap(minDistance, maxDistance, 0.25f, 4f);
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        con.ControlCamera(ref pitch, ref yaw);
        yaw = Mathf.Repeat(yaw + 180, 360) - 180;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Quaternion orientation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 fwd = orientation * Vector3.forward;
        Vector3 up = orientation * Vector3.up;
        transform.position = con.Position - fwd * distance;
        transform.LookAt(con.Position + Vector3.up * lookHeightOffset * distance.Remap(minDistance, maxDistance, 0.25f, 2));

        ParticleSystem.MainModule main = speedLines.main;
        main.startSpeed = speedLinesSpeedCurve.Evaluate(Vector3.Distance(con.Position, cachedPosition) / Time.deltaTime);

        if (main.startSpeed.constant > 0)
            speedLines.transform.rotation = Quaternion.LookRotation(con.Position - cachedPosition);

        cachedPosition = con.Position;
    }

    // OPTIMIZATION: Helper method for safe PlayerRegistry searches
    private static PlayerObject SafeFirstPlayer(System.Predicate<PlayerObject> match)
    {
        try
        {
            return PlayerRegistry.First(match);
        }
        catch
        {
            return null;
        }
    }

    public static void Recenter()
    {
        Instance.pitch = Instance.defaultPitch;
        Instance.yaw = 0;
    }
}