using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

[RequireComponent(typeof(PlayerInput))]
public class GameControlManager : MonoBehaviour
{

    PlayerInput _playerInput;
    InputAction _quitAction;
    
    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>(); 
        _quitAction = _playerInput.actions["Quit"];
        
    }

    
    void Update()
    {
        if (_quitAction.WasPressedThisFrame())
        {
            //EditorApplication.isPlaying = false;
            Application.Quit();
        }
    }
}
