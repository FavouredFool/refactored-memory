using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{

    [Header("Dependencies")]

    [Header("Configurations")]
    [SerializeField, Range(0f, 100f)]
    float _maxSpeed = 5f;

    [SerializeField, Range(0f, 500f)]
    float _minAcceleration = 1f;

    [SerializeField, Range(0f, 500f)]
    float _maxAcceleration = 5f;

    [SerializeField, Range(0f, 50f)]
    float _minAccelerationFalloff = 0.2f;

    [SerializeField, Range(0f, 50f)]
    float _maxAccelerationFalloff = 2f;

    [SerializeField, Range(0f, 50f)]
    float _bounceAccelerationFalloff = 2f;
    
    [SerializeField, Range(0f, 100f)]
    float _jumpHeight = 5f;

    [SerializeField, Range(0f, 1000f)]
    float _jumpWindowInMS = 500f;

    [SerializeField, Range(0f, 100f)]
    float _bounceHeight = 2f;

    [SerializeField, Range(0f, 90f)]
    float _maxGroundAngle = 60f, _maxWallAngle = 90f;

    [SerializeField, Range(0f, 10f)]
    float _gravityScale = 3f;

    [Header("AnimationCurve")]
    [SerializeField]
    AnimationCurve _accelerationCurve;


    // Properties
    bool OnGround => _groundContactCount > 0;
    bool OnWall => _wallContactCount > 0;

    Vector3 Gravity => Physics.gravity;

    // Inputs
    private PlayerInput _playerInput;
    private InputAction _jumpAction;
    private InputAction _movementAction;

    private Vector2 _playerInputMovementVector;

    // Components
    private Rigidbody _rb;

    // Velocity
    private Vector3 _movementVelocity;
    private Vector3 _bounceVelocity;
    private Vector3 _desiredForwardVelocity, _desiredRightVelocity = Vector3.zero;
    private Vector3 _velocityLastStep;

    private float _bounceVelocityPercentage = 0f;

    // Acceleration
    private float _currentAcceleration;

    // Time
    private float _desiresJumpStartTimeInMS = float.NegativeInfinity;

    // Collision
    int _groundContactCount,_wallContactCount;
    float _minGroundDotProduct, _minWallDotProduct;
    Vector3 _groundContactNormal, _wallContactNormal;
    Vector3 _upAxis = Vector3.up;

    // Bounce
    private int _stepsSinceLastGroundBounce = 0;
    private int _stepsSinceLastWallBounce = 0;

    private void Awake()
    {
        // Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Set Rigidbody
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;

        // Set PlayerInputs
        _playerInput = GetComponent<PlayerInput>();
        _jumpAction = _playerInput.actions["Jump"];
        _movementAction = _playerInput.actions["Move"];


    }
    private void OnValidate()
    {
        _minGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        _minWallDotProduct = Mathf.Cos(_maxWallAngle * Mathf.Deg2Rad);
        _maxAcceleration = Mathf.Max(_minAcceleration, _maxAcceleration);
        _maxAccelerationFalloff = Mathf.Max(_minAccelerationFalloff, _maxAccelerationFalloff);
    }

    private void Update()
    {
        HandlePlayerMovement();

        if (_jumpAction.WasPressedThisFrame())
        {
            _desiresJumpStartTimeInMS = Time.time * 1000;
            
        }
        

    }

    private void HandlePlayerMovement()
    {
        _playerInputMovementVector = _movementAction.ReadValue<Vector2>();
        _playerInputMovementVector = Vector2.ClampMagnitude(_playerInputMovementVector, 1f);

        _desiredForwardVelocity = _playerInputMovementVector.y * transform.forward * _maxSpeed;
        _desiredRightVelocity = _playerInputMovementVector.x * transform.right * _maxSpeed;
    }

    private void FixedUpdate()
    {
        // Get Velocity
        _bounceVelocity = _rb.velocity;
        _movementVelocity = _rb.velocity;

        UpdateState();
        AdjustMovementVelocityAndAcceleration();
        AdjustBounceVelocityPercentage();

        if (OnGround)
        {
            // Jump or bounce
            if (Time.time * 1000 - _desiresJumpStartTimeInMS < _jumpWindowInMS)
            {
                Jump(_jumpHeight);
            }
            else
            {
                BounceOffGround();
            }
        }
        else if (OnWall)
        {
            BounceOffWall();
        }

        

        // Add Gravity
        _movementVelocity += Gravity * _gravityScale * Time.fixedDeltaTime;

        // Set Velocity
        _rb.velocity = _bounceVelocity * _bounceVelocityPercentage + _movementVelocity * (1 - _bounceVelocityPercentage);

        // ClearState
        ClearState();

        _velocityLastStep = _movementVelocity;

    }

    private void Jump(float jumpHeight)
    {
        if (_stepsSinceLastGroundBounce <= 1)
        {
            return;
        }

        // Reset Jump Timer
        _desiresJumpStartTimeInMS = float.NegativeInfinity;

        Vector3 jumpDirection = _groundContactNormal;

        float jumpSpeed = Mathf.Sqrt(2f * Gravity.magnitude * jumpHeight);
        float alignedSpeed = Vector3.Dot(_movementVelocity, jumpDirection);


        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        
        _movementVelocity += jumpDirection * jumpSpeed;

        _stepsSinceLastGroundBounce = 0;
    }

    private void BounceOffGround()
    {
        if (_stepsSinceLastGroundBounce <= 1)
        {
            return;
        }

        Vector3 jumpDirection = _groundContactNormal;
        float bounceSpeed = Mathf.Sqrt(2f * Gravity.magnitude * _bounceHeight);
        float alignedSpeed = Vector3.Dot(_movementVelocity, jumpDirection);
        
        if (alignedSpeed > 0f)
        {
            bounceSpeed = Mathf.Max(bounceSpeed - alignedSpeed, 0f);
        }

        _movementVelocity += jumpDirection * bounceSpeed;

        _stepsSinceLastGroundBounce = 0;
    }

    private void BounceOffWall()
    {
        if (_stepsSinceLastWallBounce <= 1)
        {
            return;
        }

        Vector3 wallNormal = _wallContactNormal;

        Vector3 reflectVelocity = Vector3.Reflect(_velocityLastStep, wallNormal);

        float bounceSpeed = Mathf.Sqrt(2f * Gravity.magnitude * reflectVelocity.magnitude);

        _bounceVelocity += reflectVelocity.normalized * bounceSpeed;

        _bounceVelocityPercentage = 1.0f;

        _stepsSinceLastWallBounce = 0;
    }

    private void UpdateState()
    {
        _stepsSinceLastGroundBounce += 1;
        _stepsSinceLastWallBounce += 1;

        if (!OnGround)
        {
            _groundContactNormal = _upAxis;
        }

        if (!OnWall)
        {
            _wallContactNormal = _upAxis;
        } 

    }


    private void ClearState()
    {
        _groundContactNormal = _wallContactNormal = Vector3.zero;
        _groundContactCount = _wallContactCount = 0;

    }
    
    private void AdjustBounceVelocityPercentage()
    {
        if (_bounceVelocityPercentage > 0f)
        {
            _bounceVelocityPercentage = Mathf.MoveTowards(_bounceVelocityPercentage, 0f, _bounceAccelerationFalloff);
        }
        else
        {
            _bounceVelocity = Vector3.zero;
        }
        
        
    }

    private void AdjustMovementVelocityAndAcceleration()
    {
        // Calculate velocity DotProducts
        float velocityDotForward = Vector3.Dot(_movementVelocity, transform.forward);
        float velocityDotRight = Vector3.Dot(_movementVelocity, transform.right);

        AdjustAcceleration(velocityDotForward, velocityDotRight);
        AdjustVelocity(velocityDotForward, velocityDotRight);
    }

    private void AdjustAcceleration(float velocityDotForward, float velocityDotRight)
    {
        float activeDot = Mathf.Abs(velocityDotForward) + Mathf.Abs(velocityDotRight);
        float deltaVelocityT = Mathf.Clamp01(activeDot / _maxSpeed);
        float desiredAcceleration = Mathf.Lerp(_minAcceleration, _maxAcceleration, _accelerationCurve.Evaluate(deltaVelocityT));

        float maxAccelerationChange;
        if (desiredAcceleration < _currentAcceleration)
        {
            maxAccelerationChange = Mathf.Lerp(_maxAccelerationFalloff, _minAccelerationFalloff, deltaVelocityT);
        }
        else
        {
            maxAccelerationChange = float.PositiveInfinity;
        }

        _currentAcceleration = Mathf.MoveTowards(_currentAcceleration, desiredAcceleration, maxAccelerationChange);
    }

    private void AdjustVelocity(float velocityDotForward, float velocityDotRight)
    {
        float desiredVelocityDotForward = Vector3.Dot(transform.forward, _desiredForwardVelocity);
        float desiredVelocityDotRight = Vector3.Dot(transform.right, _desiredRightVelocity);

        float maxSpeedChange = _currentAcceleration * Time.deltaTime;

        float calculatedForward = Mathf.MoveTowards(velocityDotForward, desiredVelocityDotForward, maxSpeedChange);
        float calculatedRight = Mathf.MoveTowards(velocityDotRight, desiredVelocityDotRight, maxSpeedChange);

        Vector3 movement = transform.forward * (calculatedForward - velocityDotForward) + transform.right * (calculatedRight - velocityDotRight);

        _movementVelocity += movement;
    }



    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(_upAxis, normal);

            if (upDot >= _minGroundDotProduct)
            {
                _groundContactCount += 1;
                _groundContactNormal += normal;
            }
            else
            {
                if (upDot > -0.01f)
                {
                    _wallContactCount += 1;
                    _wallContactNormal += normal;
                }
            }      
        }

        _groundContactNormal.Normalize();
        _wallContactNormal.Normalize();
    }

    public Vector3 GetVelocity()
    {
        return _movementVelocity;
    }

    public Vector3 GetDesiredForwardVelocity()
    {
        return _desiredForwardVelocity;
    }

    public Vector3 GetDesiredRightVelocity()
    {
        return _desiredRightVelocity;
    }

    public float GetCurrentAcceleration()
    {
        return _currentAcceleration;
    }
}
