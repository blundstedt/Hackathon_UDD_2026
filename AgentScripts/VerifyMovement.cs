using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Hackaton.Player;

var player = GameObject.Find("Player");
var controller = player.GetComponent<CharacterController>();
var input = player.GetComponent<PlayerInputReader>();
var pivot = player.transform.Find("CameraPivot");
var keyboard = InputSystem.AddDevice<Keyboard>();
var mouse = InputSystem.AddDevice<Mouse>();
Key[] keys = new Key[0];
Vector2 mouseDelta = Vector2.zero;
bool click = false;
System.Action queue = () => {
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    InputSystem.QueueStateEvent(mouse, new MouseState { delta = mouseDelta }.WithButton(MouseButton.Left, click));
};
InputSystem.onBeforeUpdate += queue;
var checks = new System.Collections.Generic.Dictionary<string, object>();
async System.Threading.Tasks.Task Frames(int count) {
    int frame = Time.frameCount;
    await Wait.Until(() => Time.frameCount >= frame + count, timeoutMs: 10000);
}
void Reset(Vector3 position) {
    controller.enabled = false;
    player.transform.SetPositionAndRotation(position, Quaternion.identity);
    controller.enabled = true;
    Physics.SyncTransforms();
}
try {
    await Frames(3);
    checks["framesAdvance"] = Time.frameCount;
    checks["focusedAndCaptured"] = input.HasControl;
    checks["grounded"] = controller.isGrounded;
    keys = new[] { Key.W };
    float startTime = Time.time;
    Vector3 start = player.transform.position;
    await Frames(12);
    float walk = (player.transform.position - start).magnitude / (Time.time - startTime);
    checks["walkSpeed"] = walk;
    keys = new[] { Key.W, Key.LeftShift };
    startTime = Time.time;
    start = player.transform.position;
    await Frames(12);
    checks["sprintSpeed"] = (player.transform.position - start).magnitude / (Time.time - startTime);
    keys = new[] { Key.S }; start = player.transform.position; await Frames(5);
    checks["S"] = player.transform.position.z < start.z;
    keys = new[] { Key.A }; start = player.transform.position; await Frames(5);
    checks["A"] = player.transform.position.x < start.x;
    keys = new[] { Key.D }; start = player.transform.position; await Frames(5);
    checks["D"] = player.transform.position.x > start.x;
    keys = new[] { Key.Space }; float groundY = player.transform.position.y;
    await Frames(2); checks["jump"] = player.transform.position.y > groundY + 0.01f;
    keys = new Key[0]; await Frames(2);
    keys = new[] { Key.Space }; await Frames(2);
    var speedField = typeof(PlayerMovement).GetField("verticalSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    float firstSpeed = (float)speedField.GetValue(player.GetComponent<PlayerMovement>());
    keys = new Key[0]; await Frames(1); keys = new[] { Key.Space }; await Frames(1);
    checks["noAirJump"] = (float)speedField.GetValue(player.GetComponent<PlayerMovement>()) <= firstSpeed;
    keys = new Key[0]; await Frames(80); checks["landed"] = controller.isGrounded;
    mouseDelta = new Vector2(15f, 10000f); await Frames(3);
    checks["yawResponds"] = player.transform.eulerAngles.y > 0.1f;
    checks["pitchClamped"] = Mathf.Abs(Mathf.DeltaAngle(0f, pivot.localEulerAngles.x)) <= 85.01f;
    mouseDelta = Vector2.zero;
    Reset(new Vector3(0f, 0.1f, 4f)); keys = new[] { Key.W }; await Frames(35);
    checks["wallCollision"] = player.transform.position.z < 5.5f;
    keys = new[] { Key.Escape }; await Frames(2);
    checks["escapeReleases"] = Cursor.lockState == CursorLockMode.None && Cursor.visible;
    keys = new Key[0]; click = true; await Frames(2);
    checks["clickCaptures"] = Cursor.lockState == CursorLockMode.Locked && !Cursor.visible;
    click = false; Reset(new Vector3(0f, 3f, -8f)); await Frames(80);
    checks["gravityLands"] = controller.isGrounded && player.transform.position.y < 0.1f;
} finally {
    InputSystem.onBeforeUpdate -= queue;
    InputSystem.RemoveDevice(keyboard);
    InputSystem.RemoveDevice(mouse);
}
return checks;
