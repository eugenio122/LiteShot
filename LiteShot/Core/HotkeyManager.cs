using System;
using System.Runtime.InteropServices;

namespace LiteShot.Core
{
    /// <summary>
    /// Contém as chamadas nativas (P/Invoke) para a API do Windows (user32.dll).
    /// Permite que o LiteShot escute atalhos de teclado mesmo rodando em segundo plano.
    /// </summary>
    public partial class HotkeyManager
    {
        // Constantes do Windows API para mensagens e teclas modificadoras
        public const int WM_HOTKEY = 0x0312;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public const uint VK_PRINTSCREEN = 0x2C; // Tecla PrintScreen

        // O .NET 10 usa LibraryImport para gerar chamadas nativas otimizadas
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}