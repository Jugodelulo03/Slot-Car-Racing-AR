using UnityEngine;
using UnityEngine.InputSystem;

namespace SlotCarRacingAR.Runtime.Debug
{
    /// <summary>
    /// Overhead camera controller for PC/Editor testing.
    /// WASD to pan, QE to raise/lower, right-click drag to orbit, scroll to zoom.
    /// </summary>
    public sealed class EditorCameraController : MonoBehaviour
    {
        private float _moveSpeed = 2f;
        private float _fastMultiplier = 3f;
        private float _rotateSpeed = 0.15f;
        private float _scrollSpeed = 0.5f;

        private float _yaw;
        private float _pitch;

        private void OnEnable()
        {
            Transform t = transform;
            t.position = new Vector3(0f, 1.5f, 0f);
            t.rotation = Quaternion.Euler(75f, 0f, 0f);

            Vector3 euler = t.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
        }

        private void Update()
        {
            HandleRotation();
            HandleMovement();
            HandleZoom();
        }

        private void HandleRotation()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * _rotateSpeed;
            _pitch -= delta.y * _rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, 5f, 90f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            float speed = _moveSpeed;
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                speed *= _fastMultiplier;

            Vector3 move = Vector3.zero;
            Transform t = transform;

            if (kb.wKey.isPressed) move += t.forward;
            if (kb.sKey.isPressed) move -= t.forward;
            if (kb.aKey.isPressed) move -= t.right;
            if (kb.dKey.isPressed) move += t.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move -= Vector3.up;

            if (move.sqrMagnitude > 0.001f)
                t.position += move.normalized * (speed * Time.unscaledDeltaTime);
        }

        private void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            transform.position += transform.forward * (scroll * _scrollSpeed * Time.unscaledDeltaTime);
        }
    }
}
