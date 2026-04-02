using System.Runtime.InteropServices;

namespace Xabbo.Utility;

internal static class KeyboardState
{
    private const int VK_SHIFT = 0x10;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static bool IsShiftPressed()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        return IsKeyPressed(VK_SHIFT) ||
            IsKeyPressed(VK_LSHIFT) ||
            IsKeyPressed(VK_RSHIFT);
    }

    private static bool IsKeyPressed(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
