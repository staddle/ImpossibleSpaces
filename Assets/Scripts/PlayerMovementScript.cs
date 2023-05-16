using UnityEngine;
using UnityEngine.InputSystem;

// From: Samples.InputSystem.1.3.0.SimpleDemo.SimpleController_UsingPlayerInput.cs
// Use a separate PlayerInput component for setting up input.
public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float rotateSpeedVR;
    public Transform cameraTransform;

    private Vector2 m_Rotation;
    private Vector2 m_Look;
    private Vector2 m_LookVR;
    private Vector2 m_Move;

    public void OnMove(InputAction.CallbackContext context)
    {
        m_Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        m_Look = context.ReadValue<Vector2>();
    }   

    public void Update()
    {
        // Update orientation first, then move. Otherwise move orientation will lag
        // behind by one frame.
        Look(m_Look);
        #if ENABLE_VR
            LookVR(m_LookVR);
        #endif
        Move(m_Move);
        /*#if ENABLE_VR
            OVRInput.Update();
            Vector2 primary = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if(!primary.Equals(Vector2.zero))
            {
                m_Move = primary;
            }
            Vector2 secondary = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            if (!secondary.Equals(Vector2.zero))
            {   
                m_LookVR = secondary;
            }
        #endif*/
    }

    private void Move(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01)
            return;
        var scaledMoveSpeed = moveSpeed * Time.deltaTime;
        // For simplicity's sake, we just keep movement in a single plane here. Rotate
        // direction according to world Y rotation of player.
        var move = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * new Vector3(direction.x, 0, direction.y);
        transform.position += move * scaledMoveSpeed;
    }

    private void LookVR(Vector2 rotate)
    {
        if (rotate.sqrMagnitude < 0.01)
            return;
        var scaledRotateSpeed = rotateSpeedVR * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        transform.localEulerAngles = m_Rotation;
    }

    private void Look(Vector2 rotate)
    {
        if (rotate.sqrMagnitude < 0.01)
            return;
        var scaledRotateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
        cameraTransform.localEulerAngles = m_Rotation;
    }
}
