using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Text;

public class UIScript : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private PlayerController _playerController;

    [SerializeField]
    private TMP_Text _velocityText;

    [SerializeField]
    private TMP_Text _desiredVelocityText;

    [SerializeField]
    private TMP_Text _accelerationText;

    [SerializeField]
    private TMP_Text _verticalVelocityText;


    private void Update()
    {
        FillHelpTexts();
    }

    private void FillHelpTexts()
    {
        _velocityText.text = "Velocity: " + _playerController.GetVelocity();

        Vector3 combinedDesiredVelocity = _playerController.GetDesiredForwardVelocity() + _playerController.GetDesiredRightVelocity();
        _desiredVelocityText.text = "DesiredVelocity: " + combinedDesiredVelocity;

        _accelerationText.text = "Acceleration: " + _playerController.GetCurrentAcceleration();
    }

}
