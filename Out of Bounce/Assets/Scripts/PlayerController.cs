using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{

    [Header("Dependencies")]

    [Header("Configurations")]
    [SerializeField, Range(0f, 100f)]
    float _maxSpeed = 5f;

    [SerializeField, Range(0f, 100f)]
    float _maxAcceleration = 5f, _maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 100f)]
    float _jumpHeight = 5f;

    [SerializeField, Range(0f, 100f)]
    float _bounceHeight = 2f;

    [SerializeField, Range(0f, 5f)]
    int _maxAirJumps = 0;

    [SerializeField, Range(0f, 90f)]
    float _maxGroundAngle = 60f;


    // Properties
    bool OnGround => _groundContactCount > 0;

    // Inputs
    private PlayerInput _playerInput;
    private InputAction _jumpAction;
    private InputAction _movementAction;

    private Vector2 _playerInputMovementVector;

    // Components
    private Rigidbody _rb;

    // Physics Forces
    private Vector3 _gravity = Physics.gravity;
    
    // Velocity
    private Vector3 _velocity;
    private Vector3 _desiredForwardVelocity, _desiredRightVelocity = Vector3.zero;

    // Flags
    private bool _desiresJump = false;

    // Collision
    int stepsSinceLastGrounded, stepsSinceLastJumped;
    int _groundContactCount;
    float _minGroundDotProduct;
    Vector3 _contactNormal;
    Vector3 _upAxis = Vector3.up;

    // Jump
    int _jumpPhase;

    

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
    }

    private void Update()
    {
        HandlePlayerMovement();

        if (_jumpAction.WasPressedThisFrame())
        {
            _desiresJump = true;
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
        _velocity = _rb.velocity;

        UpdateState();
        AdjustVelocity();

        if (_desiresJump)
        {
            _desiresJump = false;
            Jump(_jumpHeight);
        }
        else if (_groundContactCount > 0)
        {
            Bounce();
        }

        // Add Gravity
        _velocity += _gravity * Time.fixedDeltaTime;

        // Set Velocity
        _rb.velocity = _velocity;

        // ClearState
        ClearState();
        
    }

    private void Jump(float jumpHeight)
    {
        Vector3 jumpDirection;

        if (OnGround)
        {
            jumpDirection = _contactNormal;
        }
        else if (_maxAirJumps > 0 && _jumpPhase <= _maxAirJumps)
        {
            if (_jumpPhase == 0)
            {
                _jumpPhase = 1;
            }
            jumpDirection = _contactNormal;
        }
        else
        {
            return;
        }


        stepsSinceLastJumped = 0;
        _jumpPhase += 1;
        float jumpSpeed = Mathf.Sqrt(2f * _gravity.magnitude * jumpHeight);

        float alignedSpeed = Vector3.Dot(_velocity, jumpDirection);

        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        
        _velocity += jumpDirection * jumpSpeed;
    }

    private void Bounce()
    {
        Vector3 jumpDirection = _contactNormal;

        float bounceSpeed = Mathf.Sqrt(2f * _gravity.magnitude * _bounceHeight);

        float alignedSpeed = Vector3.Dot(_velocity, jumpDirection);

        if (alignedSpeed > 0f)
        {
            bounceSpeed = Mathf.Max(bounceSpeed - alignedSpeed, 0f);
        }

        _velocity += jumpDirection * bounceSpeed;
    }

    private void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJumped += 1;

        if (OnGround)
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJumped > 1)
            {
                _jumpPhase = 0;
            }
        }
        else
        {
            _contactNormal = _upAxis;
        }
    }


    private void ClearState()
    {
        _contactNormal = Vector3.zero;
        _groundContactCount = 0;
    }

    private void AdjustVelocity()
    {
        float acceleration = OnGround ? _maxAcceleration : _maxAirAcceleration;

        float maxSpeedChange = acceleration * Time.deltaTime;

        float velocityDotForward = Vector3.Dot(_velocity, transform.forward);
        float desiredVelocityDotForward = Vector3.Dot(transform.forward, _desiredForwardVelocity);

        float velocityDotRight = Vector3.Dot(_velocity, transform.right);
        float desiredVelocityDotRight = Vector3.Dot(transform.right, _desiredRightVelocity);

        float newForward = Mathf.MoveTowards(velocityDotForward, desiredVelocityDotForward, maxSpeedChange);
        float newRight = Mathf.MoveTowards(velocityDotRight, desiredVelocityDotRight, maxSpeedChange);

        Vector3 movement = transform.forward * (newForward - velocityDotForward) + transform.right * (newRight - velocityDotRight);

        _velocity += movement;
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
                _contactNormal += normal;
            }            
        }

        if (collision.contactCount > 1)
        {
            _contactNormal.Normalize();
        }
        
    }

    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }
}
