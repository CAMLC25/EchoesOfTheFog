using Unity.Cinemachine;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private CinemachineThirdPersonFollow follow;
        [SerializeField] private float targetHeight = 1.5f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.10f;
        [SerializeField, Min(1)] private float stickDegreesPerSecond = 150;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-35, 65);
        [SerializeField] private float normalFov = 58, aimFov = 44;
        [SerializeField] private float normalDistance = 3.6f, aimDistance = 2.3f;
        [SerializeField, Min(0.1f)] private float aimResponse = 10;
        private PlayerInputReader input;
        private float pitch = 12;

        public float Yaw { get; private set; }
        public float Pitch => pitch;
        public bool IsAiming => input != null && input.AimHeld;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            Yaw = transform.eulerAngles.y;
            if (cameraTarget == null || virtualCamera == null || follow == null)
            {
                Debug.LogError("PlayerCameraRig needs its camera target, CinemachineCamera and ThirdPersonFollow.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Vector2 look = input.Look;
            // Pointer delta is already a per-frame displacement; only sticks use deltaTime.
            float sensitivity = input.LookUsesPointer ? mouseSensitivity : stickDegreesPerSecond * Time.deltaTime;
            SetViewAngles(Yaw + look.x * sensitivity, pitch - look.y * sensitivity);
            float t = 1 - Mathf.Exp(-aimResponse * Time.deltaTime);
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, IsAiming ? aimFov : normalFov, t);
            follow.CameraDistance = Mathf.Lerp(follow.CameraDistance, IsAiming ? aimDistance : normalDistance, t);
        }

        public void SetViewAngles(float yaw, float verticalPitch)
        {
            Yaw = Mathf.Repeat(yaw, 360);
            pitch = Mathf.Clamp(verticalPitch, pitchLimits.x, pitchLimits.y);
        }

        private void LateUpdate()
        {
            // Read the interpolated Rigidbody transform; keep orbit yaw independent of actor rotation.
            cameraTarget.SetPositionAndRotation(transform.position + Vector3.up * targetHeight, Quaternion.Euler(pitch, Yaw, 0));
        }
    }
}
