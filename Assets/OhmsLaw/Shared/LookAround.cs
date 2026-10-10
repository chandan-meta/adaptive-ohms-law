using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OhmsLaw
{
    // v5: on the Main Camera (ModuleFlow.EnterScene adds it next to PanelFraming). A left-drag (or a touch drag) that starts
    // on the 3D view, with no UI under the pointer and no collider under it (rig parts, the key and the terminals keep their
    // taps), turns the shot up to ±MaxYaw and ±MaxPitch from where Cinemachine put it, and eases back when released.
    // The turn is applied to the camera for its render only (beginCameraRendering → endCameraRendering): outside the render
    // every script, raycast and the scene hand-off read the brain's own pose, so a drag never leaks into a virtual camera
    // or the next scene's entry shot. Off under reduced motion and while the pause menu, the title or Ask Wattson is open.
    // ponytail: render-time only, so the tap hand (TapHint, placed in Update) lags the view while a drag is held.
    public class LookAround : MonoBehaviour
    {
        public const float MaxYaw = 18f, MaxPitch = 8f;
        const float DegreesPerPixel = 0.06f;

        Camera cam;
        Vector2 look, target;
        Quaternion brainPose;
        bool dragging, applied;

        void Awake() => cam = GetComponent<Camera>();
        void OnEnable() { RenderPipelineManager.beginCameraRendering += Begin; RenderPipelineManager.endCameraRendering += End; }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= Begin; RenderPipelineManager.endCameraRendering -= End; look = target = Vector2.zero; }

        static bool Blocked => ReducedMotion.On || PauseMenu.IsOpen || TitleScreen.IsOpen || AskWattsonUI.IsOpen;

        void Update()
        {
            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool mouseDown = mouse != null && mouse.leftButton.isPressed, touchDown = touch != null && touch.primaryTouch.press.isPressed;
            bool pressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (touch != null && touch.primaryTouch.press.wasPressedThisFrame);
            Vector2 delta = mouseDown ? mouse.delta.ReadValue() : touchDown ? touch.primaryTouch.delta.ReadValue() : Vector2.zero;
            if (pressed && !Blocked)
            {
                Vector2 at = mouse != null && mouse.leftButton.wasPressedThisFrame ? mouse.position.ReadValue() : touch.primaryTouch.position.ReadValue();
                int pointer = mouse != null && mouse.leftButton.wasPressedThisFrame ? -1 : touch.primaryTouch.touchId.ReadValue();
                dragging = Free(at, pointer);
            }
            if (!(mouseDown || touchDown) || Blocked) dragging = false;
            // Grab and pull: dragging right turns the view left, dragging up tilts it down.
            target = dragging
                ? new Vector2(Mathf.Clamp(target.x - delta.x * DegreesPerPixel, -MaxYaw, MaxYaw), Mathf.Clamp(target.y + delta.y * DegreesPerPixel, -MaxPitch, MaxPitch))
                : Vector2.zero;
            look = Vector2.Lerp(look, target, 1f - Mathf.Exp(-(dragging ? 16f : 5f) * Time.unscaledDeltaTime));
        }

        // Nothing of the HUD and nothing tappable under the pointer.
        bool Free(Vector2 at, int pointer)
        {
            var events = EventSystem.current;
            if (events && events.IsPointerOverGameObject(pointer)) return false;
            return cam && !Physics.Raycast(cam.ScreenPointToRay(at), 100f);
        }

        void Begin(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || look.sqrMagnitude < 1e-4f) return;
            brainPose = cam.transform.rotation;
            cam.transform.rotation = brainPose * Quaternion.Euler(look.y, look.x, 0f);
            applied = true;
        }

        void End(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || !applied) return;
            cam.transform.rotation = brainPose;
            applied = false;
        }
    }
}
