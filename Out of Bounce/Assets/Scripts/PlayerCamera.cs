using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCamera : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private PlayerController _playerController;


    [Header("Configuration")]
    [SerializeField, Range(0f, 1f)]
    private float _mouseSensitivity;

    private PlayerInput _playerInput;

    private InputAction _lookAction;

    private Vector2 currentLook = Vector3.forward;

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _lookAction = _playerInput.actions["Look"];
    }

    private void Update()
    {
        HandleLookDirection();
    }

    private void HandleLookDirection()
    {
        // horizontal bewegt sich Spieler, Vertikal nur die Kamera

        Vector2 lookDelta = _lookAction.ReadValue<Vector2>();

        lookDelta.x *= _mouseSensitivity;
        lookDelta.y *= _mouseSensitivity;

        currentLook.x += lookDelta.x;
        currentLook.y = Mathf.Clamp(currentLook.y += lookDelta.y, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(-currentLook.y, Vector3.right);
        _playerController.transform.localRotation = Quaternion.Euler(0, currentLook.x, 0);

    }
}
