using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoesOfTheFog.Player
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-150)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset instance;
        private InputAction move, look, sprint, aim, jump, reload, interact, releaseCursor, captureCursor;
        private bool controlsEnabled = true;
        private bool jumpQueued;
        private bool suppressFireUntilRelease;

        public bool CursorCaptured { get; private set; }
        public event System.Action PauseRequested;
        public bool HasPauseMenu => PauseRequested != null;
        public bool CanControl => isActiveAndEnabled && controlsEnabled && CursorCaptured;
        public Vector2 Move => CanControl ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public Vector2 Look => CanControl ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool LookUsesPointer => look?.activeControl?.device is Pointer;
        public bool SprintHeld => CanControl && sprint.IsPressed();
        public bool AimHeld => CanControl && aim.IsPressed();
        public bool FireHeld => CanControl && !suppressFireUntilRelease && captureCursor.IsPressed();
        public bool ReloadPressed => CanControl && reload != null && reload.WasPressedThisFrame();
        public bool InteractPressed => CanControl && interact != null && interact.WasPressedThisFrame();
        public bool UseMedkitPressed => CanControl && useMedkit != null && useMedkit.WasPressedThisFrame();
        private InputAction useMedkit;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerInputReader needs an InputActionAsset.", this);
                enabled = false;
                return;
            }
            // Each player owns its enabled map and never toggles the project's shared asset.
            instance = Instantiate(actions);
            var map = instance.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            sprint = map.FindAction("Sprint", true);
            aim = map.FindAction("Aim", true);
            jump = map.FindAction("Jump", true);
            reload = map.FindAction("Reload", false);
            interact = map.FindAction("Interact", false);
            useMedkit = map.FindAction("UseMedkit", false);
            if (useMedkit == null)
            {
                useMedkit = map.AddAction("UseMedkit", InputActionType.Button);
                useMedkit.AddBinding("<Keyboard>/e");
                useMedkit.AddBinding("<Gamepad>/dpad/up");
            }
            jump.performed += OnJump;
            releaseCursor = map.FindAction("ReleaseCursor", true);
            captureCursor = map.FindAction("Attack", true);
        }

        private void OnEnable()
        {
            if (instance != null) instance.FindActionMap("Player").Enable();
        }

        private void Start() => SetCursorCaptured(true);

        private void Update()
        {
            if (instance == null) return;
            if (!captureCursor.IsPressed()) suppressFireUntilRelease = false;
            if (releaseCursor.WasPressedThisFrame())
            {
                if (PauseRequested != null) PauseRequested.Invoke();
                else SetCursorCaptured(false);
            }
            else if (!CursorCaptured && controlsEnabled && captureCursor.WasPressedThisFrame()) SetCursorCaptured(true);
        }

        public void SetCursorCaptured(bool captured)
        {
            CursorCaptured = captured && controlsEnabled && instance != null;
            Cursor.lockState = CursorCaptured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !CursorCaptured;
            if (!CursorCaptured) jumpQueued = false;
            else suppressFireUntilRelease = captureCursor.IsPressed();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (CanControl) jumpQueued = true;
        }

        // Latch a press across Update/FixedUpdate, then consume it once even if airborne.
        public bool ConsumeJump()
        {
            bool requested = CanControl && jumpQueued;
            jumpQueued = false;
            return requested;
        }

        public void SetControlsEnabled(bool value)
        {
            controlsEnabled = value;
            if (!value) SetCursorCaptured(false);
        }

        public void SuppressFireUntilRelease() => suppressFireUntilRelease = captureCursor != null && captureCursor.IsPressed();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SetCursorCaptured(false);
        }

        private void OnDisable()
        {
            if (instance != null) instance.Disable();
            SetCursorCaptured(false);
        }

        private void OnDestroy()
        {
            if (jump != null) jump.performed -= OnJump;
            if (instance != null) Destroy(instance);
        }
    }
}
