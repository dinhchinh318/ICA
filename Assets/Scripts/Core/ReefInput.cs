using UnityEngine;
using UnityEngine.InputSystem;
namespace LumaReef.Core
{
    public static class ReefInput
    {
        public static Vector2 ScreenPosition => Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
            ? Touchscreen.current.primaryTouch.position.ReadValue() : Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public static bool Held => (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        public static bool DebugPressed => Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
        public static bool BackPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
