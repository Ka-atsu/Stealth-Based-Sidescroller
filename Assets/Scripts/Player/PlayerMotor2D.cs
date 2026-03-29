using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    #region Inspector

    [Header("Movement")]
    public float acceleration = 12f;
    public float deceleration = 18f;
    [Range(0f, 1f)] public float airControlPercent = 0.6f;
    public float turnAccelerationMultiplier = 1.75f;
    public float releaseDecelerationMultiplier = 1.35f;
    public float reverseDecelerationMultiplier = 1.5f;
    public float inputDeadZone = 0.08f;
    public float lowSpeedSnap = 0.08f;

    [Header("Sprint")]
    public float runSpeed = 14f;
    public float walkSpeed = 9f;

    [Header("Crouch")]
    public float crouchSpeed = 4f;

    [Header("Footsteps")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;

    [Header("Juice Events")]
    public float landingMinSpeed = 8f;
    public float skidMinSpeed = 6f;
    public float skidCooldown = 0.15f;

    [Header("Visual Juice")]
    [SerializeField] Transform visuals;
    [SerializeField] float maxLeanAngle = 10f;
    [SerializeField] float leanSpeed = 16f;
    [SerializeField] float stepSquash = 0.02f;
    [SerializeField] float landSquash = 0.10f;
    [SerializeField] float skidSquash = 0.06f;
    [SerializeField] float squashRecoverSpeed = 12f;

    [Header("Motion Stretch")]
    [SerializeField] float runStretch = 0.025f;
    [SerializeField] float riseStretch = 0.02f;
    [SerializeField] float fallStretch = 0.045f;
    [SerializeField] float stretchBlendSpeed = 14f;
    [SerializeField] float riseStretchVelocity = 9f;
    [SerializeField] float fallStretchVelocity = 14f;

    #endregion

    #region Public Hooks

    public Action<float> OnStep;
    public Action<float> OnLand;
    public Action<float> OnSkid;

    #endregion

    #region Private Refs

    Rigidbody2D rb;
    PlayerNoiseEmitter2D noise;

    #endregion

    #region State

    float stepTimer;
    float skidTimer;
    float lastVerticalVelocity;
    bool wasGrounded;

    Vector3 visualsBaseScaleAbs;
    float squashAmount;
    bool visualsIsRoot;

    float cachedMoveInputX;
    bool cachedIsGrounded;

    #endregion

    #region Unity

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noise = GetComponent<PlayerNoiseEmitter2D>();

        ResolveVisualsReference();
        CacheVisualBaseScale();
    }

    void Update()
    {
        UpdateVisualJuice(Time.deltaTime);
    }

    #endregion

    #region Main Motor

    public void TickFixed(
        float dt,
        Vector2 moveInput,
        bool isGrounded,
        Vector2 groundNormal,
        bool runHeld,
        bool isCrouching,
        bool movementLocked
    )
    {
        cachedIsGrounded = isGrounded;

        float inputX = Mathf.Abs(moveInput.x) >= inputDeadZone ? moveInput.x : 0f;
        cachedMoveInputX = inputX;

        if (movementLocked)
        {
            ResetMotionTimers(dt, isGrounded);
            return;
        }

        float currentSpeed =
            isCrouching ? crouchSpeed :
            runHeld ? runSpeed :
            walkSpeed;

        float targetSpeed = inputX * currentSpeed;

        // Preserve air momentum if no air input
        if (!isGrounded && Mathf.Abs(inputX) < 0.01f)
        {
            DetectLanding(dt, isGrounded);
            DetectSkid(inputX, isGrounded);
            HandleFootsteps(dt, isGrounded, runHeld, isCrouching);
            CacheFrameState(isGrounded);
            return;
        }

        float smoothRate = GetSmoothRate(inputX, isGrounded);

        if (isGrounded)
        {
            Vector2 slopeTangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;

            if (slopeTangent.x < 0f)
                slopeTangent *= -1f;

            float currentAlongSlope = Vector2.Dot(rb.linearVelocity, slopeTangent);
            float newAlongSlope = Mathf.Lerp(currentAlongSlope, targetSpeed, smoothRate * dt);

            if (Mathf.Abs(inputX) < 0.01f && Mathf.Abs(newAlongSlope) <= lowSpeedSnap)
                newAlongSlope = 0f;

            newAlongSlope = Mathf.Clamp(newAlongSlope, -currentSpeed, currentSpeed);

            Vector2 newVelocity = slopeTangent * newAlongSlope;

            // helps keep the player attached to the slope
            if (rb.linearVelocity.y <= 0f)
                newVelocity += -groundNormal * 1.5f;
            else
                newVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = newVelocity;
        }
        else
        {
            float newX = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, smoothRate * dt);

            if (Mathf.Abs(inputX) < 0.01f && Mathf.Abs(newX) <= lowSpeedSnap)
                newX = 0f;

            newX = Mathf.Clamp(newX, -currentSpeed, currentSpeed);

            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        DetectLanding(dt, isGrounded);
        DetectSkid(inputX, isGrounded);
        HandleFootsteps(dt, isGrounded, runHeld, isCrouching);
        CacheFrameState(isGrounded);
    }

    float GetSmoothRate(float inputX, bool isGrounded)
    {
        bool hasInput = Mathf.Abs(inputX) > 0.01f;
        float currentX = rb.linearVelocity.x;

        float smoothRate = hasInput ? acceleration : deceleration * releaseDecelerationMultiplier;

        if (!isGrounded)
            smoothRate *= hasInput ? airControlPercent : 1f;

        bool reversing =
            hasInput &&
            Mathf.Abs(currentX) > 0.1f &&
            Mathf.Sign(inputX) != Mathf.Sign(currentX);

        if (reversing)
            smoothRate *= turnAccelerationMultiplier * reverseDecelerationMultiplier;

        return smoothRate;
    }

    #endregion

    #region Footsteps

    void HandleFootsteps(float dt, bool isGrounded, bool runHeld, bool isCrouching)
    {
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);

        if (!isGrounded || horizontalSpeed <= 0.2f || isCrouching)
        {
            stepTimer = 0f;
            return;
        }

        float speedPercent = runSpeed > 0f
            ? Mathf.InverseLerp(0f, runSpeed, horizontalSpeed)
            : 0f;

        float currentInterval = Mathf.Lerp(walkStepInterval, runStepInterval, speedPercent);

        stepTimer += dt;
        if (stepTimer < currentInterval) return;

        float stepStrength = Mathf.Lerp(0.7f, 1.2f, speedPercent);

        ApplyStepJuice(stepStrength);

        if (noise != null)
            noise.Emit(runHeld ? 4f : 2f, runHeld ? NoiseType.Run : NoiseType.Walk);

        OnStep?.Invoke(stepStrength);

        stepTimer = 0f;
    }

    #endregion

    #region Juice Detection

    void DetectLanding(float dt, bool isGrounded)
    {
        skidTimer = Mathf.Max(0f, skidTimer - dt);

        if (!wasGrounded && isGrounded && Mathf.Abs(lastVerticalVelocity) >= landingMinSpeed)
        {
            float landStrength = Mathf.InverseLerp(
                landingMinSpeed,
                landingMinSpeed * 2f,
                Mathf.Abs(lastVerticalVelocity)
            );

            ApplyLandJuice(landStrength);
            OnLand?.Invoke(landStrength);
        }
    }

    void DetectSkid(float inputX, bool isGrounded)
    {
        bool reversing =
            isGrounded &&
            Mathf.Abs(inputX) > 0.1f &&
            Mathf.Abs(rb.linearVelocity.x) >= skidMinSpeed &&
            Mathf.Sign(inputX) != Mathf.Sign(rb.linearVelocity.x);

        if (reversing && skidTimer <= 0f)
        {
            float skidStrength = Mathf.InverseLerp(
                skidMinSpeed,
                runSpeed,
                Mathf.Abs(rb.linearVelocity.x)
            );

            ApplySkidJuice(skidStrength);
            OnSkid?.Invoke(skidStrength);

            skidTimer = skidCooldown;
        }
    }

    #endregion

    #region Visual Juice

    void UpdateVisualJuice(float dt)
    {
        if (visuals == null) return;

        UpdateLean(dt);

        if (!visualsIsRoot)
            UpdateScaleStyle(dt);
    }

    void UpdateLean(float dt)
    {
        float velocityLeanPercent = runSpeed > 0f
            ? Mathf.Clamp(rb.linearVelocity.x / runSpeed, -1f, 1f)
            : 0f;

        float inputLeanPercent = Mathf.Clamp(cachedMoveInputX, -1f, 1f);

        float blendedLeanPercent = Mathf.Lerp(velocityLeanPercent, inputLeanPercent, 0.3f);

        if (!cachedIsGrounded)
            blendedLeanPercent *= 0.85f;

        float targetAngle = -blendedLeanPercent * maxLeanAngle;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        visuals.localRotation = Quaternion.Lerp(visuals.localRotation, targetRotation, leanSpeed * dt);
    }

    void UpdateScaleStyle(float dt)
    {
        squashAmount = Mathf.Lerp(squashAmount, 0f, squashRecoverSpeed * dt);

        float xSign = Mathf.Sign(visuals.localScale.x);
        if (xSign == 0f) xSign = 1f;

        float ySign = Mathf.Sign(visuals.localScale.y);
        if (ySign == 0f) ySign = 1f;

        float zSign = Mathf.Sign(visuals.localScale.z);
        if (zSign == 0f) zSign = 1f;

        float speedPercent = runSpeed > 0f
            ? Mathf.InverseLerp(0f, runSpeed, Mathf.Abs(rb.linearVelocity.x))
            : 0f;

        float runStretchAmount = speedPercent * runStretch;

        float riseSpeed = Mathf.Max(0f, rb.linearVelocity.y);
        float fallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);

        float riseAmount = Mathf.InverseLerp(0f, riseStretchVelocity, riseSpeed) * riseStretch;
        float fallAmount = Mathf.InverseLerp(0f, fallStretchVelocity, fallSpeed) * fallStretch;

        float widthMul =
            1f
            + squashAmount
            - runStretchAmount
            - (riseAmount * 0.35f)
            - (fallAmount * 0.45f);

        float heightMul =
            1f
            - squashAmount
            + runStretchAmount
            + riseAmount
            + fallAmount;

        Vector3 targetScale = new Vector3(
            visualsBaseScaleAbs.x * widthMul * xSign,
            visualsBaseScaleAbs.y * heightMul * ySign,
            visualsBaseScaleAbs.z * zSign
        );

        visuals.localScale = Vector3.Lerp(visuals.localScale, targetScale, stretchBlendSpeed * dt);
    }

    void ApplyStepJuice(float strength)
    {
        if (visualsIsRoot) return;
        squashAmount = Mathf.Max(squashAmount, stepSquash * strength);
    }

    void ApplyLandJuice(float strength)
    {
        if (visualsIsRoot) return;
        float amount = landSquash * Mathf.Lerp(0.75f, 1.5f, strength);
        squashAmount = Mathf.Max(squashAmount, amount);
    }

    void ApplySkidJuice(float strength)
    {
        if (visualsIsRoot) return;
        float amount = skidSquash * Mathf.Lerp(0.75f, 1.25f, strength);
        squashAmount = Mathf.Max(squashAmount, amount);
    }

    #endregion

    #region Helpers

    void ResolveVisualsReference()
    {
        if (visuals != null)
        {
            visualsIsRoot = visuals == transform;
            return;
        }

        Transform found = transform.Find("Visuals");
        visuals = found != null ? found : transform;
        visualsIsRoot = visuals == transform;

        if (visualsIsRoot)
            Debug.LogWarning($"{name}: No 'Visuals' child assigned/found. Using root transform instead. Lean will work, scale juice is disabled for safety.");
    }

    void CacheVisualBaseScale()
    {
        visualsBaseScaleAbs = new Vector3(
            Mathf.Abs(visuals.localScale.x),
            Mathf.Abs(visuals.localScale.y),
            Mathf.Abs(visuals.localScale.z)
        );
    }

    void CacheFrameState(bool isGrounded)
    {
        lastVerticalVelocity = rb.linearVelocity.y;
        wasGrounded = isGrounded;
    }

    void ResetMotionTimers(float dt, bool isGrounded)
    {
        stepTimer = 0f;
        skidTimer = Mathf.Max(0f, skidTimer - dt);
        CacheFrameState(isGrounded);
    }

    #endregion
}