using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Assets.Scripts;
using static Level;

public enum WeightType { Light, Medium, Heavy }

public class Putter : NetworkBehaviour, ICanControlCamera
{
    public WeightType weightType = WeightType.Medium;
    public Transform interpolationTarget;
    public Transform guideArrow;
    public MeshRenderer guideArrowRen;
    public MeshRenderer ren;
    public Rigidbody rb;
    new public SphereCollider collider;
    public float maxPuttStrength;
    public float puttGainFactor = 0.1f;
    public float speedLoss;

    // MOVEMENT
    public float timeAfterFullStop = 1f;
    public float standingSpeed = 1f;
    public float standingTime = 1.5f;
    private bool isStandingUp = false;
    private bool isRotatingOnSpot = false;
    private float standUpTimer = 0f;
    private Vector3 lastVelocity;

    // SKIN
    public PlayerSkin playerSkin;
    public GameObject BonesBase;
    public Transform skinModel;
    public MeshFilter skinMeshFilter;
    public MeshRenderer skinMeshRenderer;

    // FORCE OSCILLATION
    private float swingAngle = 0f; // Current Swing Angle
    private float swingDirection = 1f; // Direction for the random swing. 1 for right, -1 for left
    public float swingSpeed = 70f; // Speed Rotation. Degrees per second
    public float maxSwingAngle = 30f; // Max Oscillation Angle

    // ABILITY
    public BaseAbility baseAbility; // Reference to the Base Ability

    // DOUBLE ClICK
    private float clickTimer = 0f; // Click timer
    private const float doubleClickThreshold = 0.3f; // Time window for double-click
    private bool firstClickRegistered = false; // If the first clikc was registered.

    // SCRIPTS
    private BarrierGenerator barrierGenerator;
    public CrownCollision crownCollision;

    [Space]
    public float shakeImpulseThreshold = 0.5f;
    public float shakeCollisionAmount = 0.75f;
    public float shakeCollisionLambda = 10f;

    public PlayerObject PlayerObj { get; private set; }

    [Networked]
    public TickTimer PuttTimer { get; set; }
    public bool CanPutt => PuttTimer.ExpiredOrNotRunning(Runner);
    public bool couldPutt;

    [Networked]
    float PuttStrength { get; set; }
    float PuttStrengthNormalized => PuttStrength / maxPuttStrength; // The actual strength of the pushing force.



    [Networked]
    PlayerInput CurrInput { get; set; }
    PlayerInput prevInput = default;

    Vector3 prevVelocity = Vector3.zero;
    Angle yaw = default;

    bool isFirstUpdate = true;

    private void Start()
    {
        barrierGenerator = GetComponent<BarrierGenerator>();
        ApplyWeightSettings();
    }

    private void LateUpdate()
    {
        if (CameraController.HasControl(this))
        {
            // Update arrow rotation
            guideArrow.rotation = Quaternion.AngleAxis((float)yaw + swingAngle, Vector3.up);
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //	if (Runner.IsServer == false) Debug.Log("OnCollisionEnter client");

    //	if (CameraController.HasControl(this))
    //	{
    //		float dot = Vector3.Dot(rb.velocity.normalized, collision.impulse.normalized);
    //		if (dot > 0 && collision.impulse.magnitude > shakeImpulseThreshold)
    //		{
    //			CameraController.Instance.Shake.TriggerShake(collision.impulse.magnitude * dot * shakeCollisionAmount, shakeCollisionLambda);
    //		}
    //	}
    //}

    public override void Spawned()
    {
        Debug.Log("PUTTER SPAWNED");
        PlayerObj = PlayerRegistry.GetPlayer(Object.InputAuthority);
        PlayerObj.Controller = this;

        ren.material.color = PlayerObj.Color;
        ApplySkin(); // Apply the player's skin
        skinMeshRenderer.material.color = PlayerObj.Color;

        if (Object.HasInputAuthority)
        {
            CameraController.AssignControl(this);
        }
        else
        {
            Instantiate(ResourcesManager.Instance.worldNicknamePrefab, InterfaceManager.Instance.worldCanvas.transform).SetTarget(this);
        }
    }


    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (CameraController.HasControl(this))
        {
            CameraController.AssignControl(null);
        }

        if (!runner.IsShutdown)
        {
            if (PlayerObj.TimeTaken != PlayerObject.TIME_UNSET)
            {
                AudioManager.Play("ballInHoleSFX", AudioManager.MixerTarget.SFX, interpolationTarget.position);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput input))
        {
            CurrInput = input;
        }

        if (Runner.IsForward)
        {
            if (baseAbility != null) baseAbility.TickCooldown();

            // Assuming you have a Rigidbody for movement
            if (rb != null && rb.linearVelocity.magnitude > 0.01f)
            {
                // Store only the horizontal movement (ignoring Y-axis)
                lastVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }

            if (IsGrounded())
            {

                if (rb.linearVelocity.sqrMagnitude <= 0.00001f)
                {
                    if (!isStandingUp)
                    {
                        standUpTimer += Time.fixedDeltaTime;
                        if (standUpTimer >= timeAfterFullStop)
                        {
                            isStandingUp = true;
                            StartCoroutine(StandUpRoutine());
                        }
                    }
                    else if (!PlayerObj.IsSpectator && !isRotatingOnSpot)
                    {
                        isRotatingOnSpot = true;
                    }
                }
                else if (rb.linearVelocity.sqrMagnitude > 0.05f) // Prevents unwanted standing when moving
                {
                    isRotatingOnSpot = false;
                    standUpTimer = 0f;
                    isStandingUp = false;
                    StopCoroutine(StandUpRoutine()); // Cancels standing if ball starts moving
                }
            }


            // Double-click time detection
            if (firstClickRegistered)
            {
                clickTimer += Time.fixedDeltaTime;
                if (clickTimer > doubleClickThreshold)
                {
                    firstClickRegistered = false; // Reset if time exceeds threshold
                    clickTimer = 0f;
                }
            }

            // Began dragging
            if (CurrInput.isDragging && !prevInput.isDragging)
            {
                // Double-click detection for jump or other ability
                if (firstClickRegistered && clickTimer <= doubleClickThreshold)
                {
                    Debug.Log("Double Click Detected! Attempting Ability...");
                    if (baseAbility != null) baseAbility.ActivateAbility(this); // Call the Base Ability

                    // Reset click tracking after activation
                    firstClickRegistered = false;
                    clickTimer = 0f;
                }
                else
                {
                    Debug.Log("First Click Detected!");
                    firstClickRegistered = true;
                    clickTimer = 0f; // Start counting time after first click
                }

                // Arrow Display
                if (CameraController.HasControl(this)) HUD.ShowPuttCharge();
                Debug.Log("Starting Drag");

                // Reset arrow direction to center and pick a random swing direction
                swingAngle = 0f;
                swingDirection = (Random.value > 0.5f) ? 1f : -1f;
            }

            // Still dragging
            if (CurrInput.isDragging)
            {
                PuttStrength = Mathf.Clamp(PuttStrength - (CurrInput.dragDelta * puttGainFactor), 0, maxPuttStrength);
                if (CameraController.HasControl(this))
                {
                    HUD.SetPuttCharge(PuttStrengthNormalized, CanPutt);

                    guideArrow.localScale = new Vector3(1, 1, PuttStrengthNormalized * 1.5f);
                    guideArrowRen.material.SetColor("_EmissionColor", HUD.Instance.PuttChargeColor.Evaluate(PuttStrengthNormalized) * Color.gray);
                }

                // Update arrow swing
                swingAngle += swingDirection * swingSpeed * Time.fixedDeltaTime;

                // If the current angle reaches the maximum angle
                if (Mathf.Abs(swingAngle) >= maxSwingAngle)
                {
                    swingDirection *= -1f; // Reverse direction at limits
                    swingAngle = Mathf.Clamp(swingAngle, -maxSwingAngle, maxSwingAngle);
                }
            }

            // Stopped dragging
            if (!CurrInput.isDragging && prevInput.isDragging)
            {
                if (CanPutt && PuttStrength > 0)
                {
                    // Apply the direction of the oscillated arrow
                    Vector3 fwd = Quaternion.AngleAxis((float)CurrInput.yaw + swingAngle, Vector3.up) * Vector3.forward;

                    // Rotate towards the direction of the force
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3(fwd.x, 0, fwd.z));
                    transform.rotation = targetRotation;

                    // If is grounded, apply the force
                    if (IsGrounded()) rb.AddForce(fwd * PuttStrength, ForceMode.VelocityChange);
                    else rb.linearVelocity = fwd * PuttStrength;

                    PuttTimer = TickTimer.CreateFromSeconds(Runner, 3);
                    PlayerObj.Strokes++;

                    if (CameraController.HasControl(this))
                    {
                        HUD.SetStrokeCount(PlayerObj.Strokes);
                    }
                }

                PuttStrength = 0;
                if (CameraController.HasControl(this))
                {
                    HUD.HidePuttCharge();
                    guideArrow.localScale = new Vector3(1, 1, 0);
                }
            }

            if (CameraController.HasControl(this) && !isFirstUpdate)
            {
                if (!CanPutt && couldPutt) HUD.ShowPuttCooldown();
                if (CanPutt && !couldPutt) HUD.HidePuttCooldown();
                if (PuttTimer.RemainingTime(Runner).HasValue) HUD.SetPuttCooldown(PuttTimer.RemainingTime(Runner).Value / 3f);

                //Vector3 impulse = rb.velocity - prevVelocity;

                //float dot = Vector3.Dot(rb.velocity.normalized, prevVelocity.normalized);
                //Vector3 delta = (rb.velocity - prevVelocity);
                //if (dot > 0 && delta.magnitude > shakeImpulseThreshold)
                //{
                //	CameraController.Instance.Shake.TriggerShake(delta.magnitude * dot * shakeCollisionAmount, shakeCollisionLambda);
                //}
            }

            couldPutt = CanPutt;
            prevInput = CurrInput;
            prevVelocity = rb.linearVelocity;
            yaw = CurrInput.yaw;
            isFirstUpdate = false;
        }

        if (isRotatingOnSpot)
        {
            Quaternion targetRotation = Quaternion.AngleAxis((float)yaw, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f); // Smooth rotation
        }

        if (IsGrounded() && rb.linearVelocity.sqrMagnitude > 0.00001f)
        {
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * speedLoss);
            if (rb.linearVelocity.sqrMagnitude <= 0.00001f)
            {
                if (PlayerObj.Strokes >= GameManager.MaxStrokes)
                {
                    Debug.Log("Out of strokes");
                    GameManager.PlayerDNF(PlayerObj);
                }
            }
        }
    }
    private IEnumerator StandUpRoutine()
    {
        Quaternion initialRotation = transform.rotation;

        // Calculate the direction from the velocity (on the horizontal plane)
        Vector3 flatVelocity = new Vector3(lastVelocity.x, 0, lastVelocity.z); // Remove the Y component, we only care about the horizontal direction

        // Calculate the rotation angle based on the horizontal direction of the velocity
        float targetYRotation = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;

        // Combine this with the current Y rotation
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0); // Only rotating around the Y axis

        float elapsedTime = 0f;

        while (elapsedTime < standingTime)
        {
            // Smooth transition to the target rotation
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / standingTime);
            elapsedTime += Time.fixedDeltaTime;
            yield return null;
        }

        transform.rotation = targetRotation; // Ensure it fully reaches the target rotation
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_Respawn(bool effect)
    {
        if (effect) Instantiate(ResourcesManager.Instance.splashEffect, transform.position, ResourcesManager.Instance.splashEffect.transform.rotation);
        if (Object.HasInputAuthority) CameraController.Recenter();

        rb.linearVelocity = rb.angularVelocity = Vector3.zero;
        rb.MovePosition(Level.Current.GetSpawnPosition(PlayerObj.Index));
    }

    bool IsGrounded()
    {
        return Physics.OverlapSphere(transform.position, collider.radius * 1.05f,
            LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore).Length > 0;
    }

    public void SetWeight(WeightType newWeightType)
    {
        weightType = newWeightType;
        ApplyWeightSettings();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_UpdateLives(int newLives)
    {
        if (CameraController.HasControl(this))
        {
            PlayerObj.Lives = newLives;
            HUD.SetLives(PlayerObj.Lives);
        }
    }
    public void ApplySkin()
    {
        if (PlayerObj != null && PlayerObj.Skin != null)
        {
            var SelectedSkin = ResourcesManager.Instance.GetSkin(PlayerObj.Skin);
            playerSkin = SelectedSkin;

            // If there are model bones, use them instead
            if (playerSkin != null && playerSkin.ModelBones != null)
            {
                // Hide existing mesh
                if (skinMeshRenderer != null) skinMeshRenderer.enabled = false;
                if (skinMeshFilter != null) skinMeshFilter.mesh = null;

                // Instantiate ModelBones and attach to skinModel
                GameObject modelInstance = Instantiate(playerSkin.ModelBones, skinModel);
                BonesBase = modelInstance;

                skinModel.transform.localPosition = Vector3.zero;
                skinModel.transform.localRotation = Quaternion.identity;
                skinModel.transform.localScale = Vector3.one;

                BonesBase.transform.localPosition = playerSkin.Position;
                BonesBase.transform.localRotation = Quaternion.Euler(playerSkin.Direction);
                BonesBase.transform.localScale = playerSkin.Scale;

                BonesBase.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = playerSkin.Mesh;
                BonesBase.GetComponentInChildren<SkinnedMeshRenderer>().material = playerSkin.Material;
                BonesBase.GetComponentInChildren<SkinnedMeshRenderer>().material.color = PlayerObj.Color;
            }
            else if (playerSkin != null)
            {
                // Use normal skin mesh if no ModelBones are present
                if (BonesBase != null) Destroy(BonesBase);
                if (skinMeshFilter != null)
                {
                    skinMeshFilter = skinModel.GetComponent<MeshFilter>();
                    skinMeshFilter.mesh = playerSkin.Mesh;
                }
                if (skinMeshRenderer != null)
                {
                    skinMeshRenderer = skinModel.GetComponent<MeshRenderer>();
                    skinMeshRenderer.material = playerSkin.Material;
                    skinMeshRenderer.material.color = PlayerObj.Color;
                }

                skinModel.transform.localPosition = playerSkin.Position;
                skinModel.transform.localRotation = Quaternion.Euler(playerSkin.Direction);
                skinModel.transform.localScale = playerSkin.Scale;
            }
        }
    }

    // Add this attribute to make sure the method runs on all clients.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_ApplyGameModeRules()
    {
        barrierGenerator = GetComponent<BarrierGenerator>();
        crownCollision = GetComponent<CrownCollision>();

        var gameMode = GameManager.CurrentGameMode;
        if (gameMode == LevelGameMode.Snake)
        {
            if (crownCollision) Destroy(crownCollision); 
            barrierGenerator.enabled = true;
            Debug.Log("Snake mode active");
        }
        else if (gameMode == LevelGameMode.KingCrown)
        {
            if (!crownCollision) crownCollision = gameObject.AddComponent<CrownCollision>();
            crownCollision.player = this;
            crownCollision.enabled = true;
            barrierGenerator.enabled = false;
            Debug.Log("King's Crown active");
        }
        else
        {
            if (crownCollision) Destroy(crownCollision);
            barrierGenerator.enabled = false;
            Debug.Log("Normal game");
        }
    }
    private void ApplyWeightSettings()
    {
        switch (weightType)
        {
            case WeightType.Light:
                rb.mass = 1;
                speedLoss = 0.05f;
                maxPuttStrength = 15;
                break;

            case WeightType.Medium:
                rb.mass = 3;
                speedLoss = 0.1f;
                maxPuttStrength = 10;
                break;

            case WeightType.Heavy:
                rb.mass = 5;
                speedLoss = 0.2f;
                maxPuttStrength = 7;
                break;
        }

        Debug.Log($"Applied weight settings: {weightType}, Mass: {rb.mass}, SpeedLoss: {speedLoss}, MaxPuttStrength: {maxPuttStrength}");
    }

    public Vector3 Position => interpolationTarget.position;
    public void ControlCamera(ref float pitch, ref float yaw)
    {
        if (!Object.HasInputAuthority || prevInput.isDragging == false)
        {
            pitch -= Input.GetAxis("Mouse Y");
        }
        yaw += Input.GetAxis("Mouse X");
    }
}