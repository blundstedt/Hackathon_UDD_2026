using UnityEngine;
using UnityEngine.InputSystem;

namespace Hackaton.Player
{
    // Only enable this component for the locally controlled player in future multiplayer.
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        private InputActionAsset ownedActions;
        private InputAction move, look, jump, sprint, attack;
        private bool skipLook;

        public bool HasControl => isActiveAndEnabled && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        public Vector2 Move => HasControl ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => HasControl && !skipLook ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool AttackPressed => HasControl && !skipLook && attack.WasPressedThisFrame();
        public bool InteractPressed => HasControl && Keyboard.current?.eKey.wasPressedThisFrame == true;
        public bool JumpPressed => HasControl && jump.WasPressedThisFrame();
        public bool SprintHeld => HasControl && sprint.IsPressed();

        private void Awake()
        {
            // Private copy: enabling one player never changes the shared project actions.
            ownedActions = Instantiate(inputActions);
            ownedActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            attack = ownedActions.FindAction("Player/Attack", true);
            move = ownedActions.FindAction("Player/Move", true);
            look = ownedActions.FindAction("Player/Look", true);
            jump = ownedActions.FindAction("Player/Jump", true);
            sprint = ownedActions.FindAction("Player/Sprint", true);
        }

        private void OnEnable()
        {
            ownedActions.FindActionMap("Player", true).Enable();
            CaptureCursor();
        }

        private void Update()
        {
            skipLook = false;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ReleaseCursor();
            else if (Application.isFocused && Cursor.lockState != CursorLockMode.Locked &&
                     Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CaptureCursor();
        }

        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            skipLook = true;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseCursor();
        }

        private void OnDisable()
        {
            ownedActions.Disable();
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            if (ownedActions != null) Destroy(ownedActions);
        }
    }
}
