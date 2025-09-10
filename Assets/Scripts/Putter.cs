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
    private float swingAngle = 0f;
    private float swingDirection = 1f;
    public float swingSpeed = 70f;
    public float maxSwingAngle = 30f;

    // ABILITY
    public BaseAbility baseAbility;

    // DOUBLE CLICK
    private float clickTimer = 0f;
    private const float doubleClickThreshold = 0.3f;
    private bool firstClickRegistered = false;

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
    float PuttStrengthNormalized => PuttStrength / maxPuttStrength;

    [Networked]
    PlayerInput CurrInput { get; set; }
    PlayerInput prevInput = default;

    Vector3 prevVelocity = Vector3.zero;
    Angle yaw = default;

    // OPTIMIZATION: Cache para Ability updates - CONSERVADOR  
    private int abilityCooldownTick = 0;
    private const int ABILITY_COOLDOWN_INTERVAL = 8; // Menos frecuente

    // OPTIMIZATION: Cache para velocity magnitude
    private float velocityMagnitudeSquared = 0f;
    private bool isMovingCached = false;

    public Rigidbody rb;

    private void Start()
    {
        barrierGenerator = GetComponent<BarrierGenerator>();
        ApplyWeightSettings();
    }

    private void Update()
    {
        // SEPARAR UI del networking thread - CRÍTICO
        if (CameraController.HasControl(this))
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // UI updates ejecutándose a 60fps en lugar de network rate
        if (!CanPutt && couldPutt) HUD.ShowPuttCooldown();
        if (CanPutt && !couldPutt) HUD.HidePuttCooldown();
        if (PuttTimer.RemainingTime(Runner).HasValue) 
        {
            HUD.SetPuttCooldown(PuttTimer.RemainingTime(Runner).Value / 3f);
        }
    }

    private void LateUpdate()
    {
        if (CameraController.HasControl(this))
        {
            // Update arrow rotation - más suave
            guideArrow.rotation = Quaternion.AngleAxis((float)yaw + swingAngle, Vector3.up);
        }

        // INTERPOLACIÓN COMPLETAMENTE REMOVIDA - Dejar que Fusion maneje todo
        // Fusion ya tiene su propio sistema de interpolación que funciona perfectamente
    }

    public override void Spawned()
    {
        Debug.Log("PUTTER SPAWNED");
        PlayerObj = PlayerRegistry.GetPlayer(Object.InputAuthority);
        PlayerObj.Controller = this;

        ren.material.color = PlayerObj.Color;
        ApplySkin();
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
            // OPTIMIZATION: Cache velocity calculations una sola vez
            velocityMagnitudeSquared = rb.linearVelocity.sqrMagnitude;
            isMovingCached = velocityMagnitudeSquared > 0.0001f;

            // OPTIMIZATION: Ability cooldown menos frecuente
            abilityCooldownTick++;
            if (abilityCooldownTick >= ABILITY_COOLDOWN_INTERVAL)
            {
                if (baseAbility != null) baseAbility.TickCooldown();
                abilityCooldownTick = 0;
            }

            // Store velocity for movement tracking - solo si se está moviendo
            if (isMovingCached)
            {
                lastVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }

            // Movement logic - ORIGINAL sin cache de IsGrounded
            if (IsGrounded())
            {
                if (velocityMagnitudeSquared <= 0.00001f)
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
                else if (velocityMagnitudeSquared > 0.0025f) // 0.05f al cuadrado
                {
                    isRotatingOnSpot = false;
                    standUpTimer = 0f;
                    isStandingUp = false;
                    StopCoroutine(StandUpRoutine());
                }
            }

            // Double-click detection
            if (firstClickRegistered)
            {
                clickTimer += Time.fixedDeltaTime;
                if (clickTimer > doubleClickThreshold)
                {
                    firstClickRegistered = false;
                    clickTimer = 0f;
                }
            }

            // Input handling
            HandleInputLogic();

            // Store previous values - UI updates movido a Update()
            couldPutt = CanPutt;
            prevInput = CurrInput;
            prevVelocity = rb.linearVelocity;
            yaw = CurrInput.yaw;
        }

        // Rotation en el lugar
        if (isRotatingOnSpot)
        {
            Quaternion targetRotation = Quaternion.AngleAxis((float)yaw, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }

        // Physics damping
        if (IsGrounded() && velocityMagnitudeSquared > 0.00001f)
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

    private void HandleInputLogic()
    {
        // Began dragging
        if (CurrInput.isDragging && !prevInput.isDragging)
        {
            if (firstClickRegistered && clickTimer <= doubleClickThreshold)
            {
                Debug.Log("Double Click Detected! Attempting Ability...");
                if (baseAbility != null) baseAbility.ActivateAbility(this);

                firstClickRegistered = false;
                clickTimer = 0f;
            }
            else
            {
                Debug.Log("First Click Detected!");
                firstClickRegistered = true;
                clickTimer = 0f;
            }

            if (CameraController.HasControl(this)) HUD.ShowPuttCharge();

            swingAngle = 0f;
            swingDirection = (Random.value > 0.5f) ? 1f : -1f;
        }

        // Still dragging
        if (CurrInput.isDragging)
        {
            PuttStrength = Mathf.Clamp(PuttStrength - (CurrInput.dragDelta * puttGainFactor), 0, maxPuttStrength);
            
            // UI updates solo si tienes control
            if (CameraController.HasControl(this))
            {
                HUD.SetPuttCharge(PuttStrengthNormalized, CanPutt);
                guideArrow.localScale = new Vector3(1, 1, PuttStrengthNormalized * 1.5f);
                guideArrowRen.material.SetColor("_EmissionColor", HUD.Instance.PuttChargeColor.Evaluate(PuttStrengthNormalized) * Color.gray);
            }

            // Swing oscillation
            swingAngle += swingDirection * swingSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(swingAngle) >= maxSwingAngle)
            {
                swingDirection *= -1f;
                swingAngle = Mathf.Clamp(swingAngle, -maxSwingAngle, maxSwingAngle);
            }
        }

        // Stopped dragging
        if (!CurrInput.isDragging && prevInput.isDragging)
        {
            if (CanPutt && PuttStrength > 0)
            {
                Vector3 fwd = Quaternion.AngleAxis((float)CurrInput.yaw + swingAngle, Vector3.up) * Vector3.forward;
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(fwd.x, 0, fwd.z));
                transform.rotation = targetRotation;

                if (IsGrounded()) 
                    rb.AddForce(fwd * PuttStrength, ForceMode.VelocityChange);
                else 
                    rb.linearVelocity = fwd * PuttStrength;

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
    }

    private IEnumerator StandUpRoutine()
    {
        Quaternion initialRotation = transform.rotation;
        Vector3 flatVelocity = new Vector3(lastVelocity.x, 0, lastVelocity.z);
        float targetYRotation = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);

        float elapsedTime = 0f;

        while (elapsedTime < standingTime)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / standingTime);
            elapsedTime += Time.fixedDeltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
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

            if (playerSkin != null && playerSkin.ModelBones != null)
            {
                if (skinMeshRenderer != null) skinMeshRenderer.enabled = false;
                if (skinMeshFilter != null) skinMeshFilter.mesh = null;

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