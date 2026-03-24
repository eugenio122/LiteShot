using System;
using System.Runtime.InteropServices;
using System.Windows.Forms; // Necessário para a enumeração 'Keys'

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

        // --- ATALHOS TEMPORÁRIOS DO OVERLAY (PASSE VIP) ---
        public const int HOTKEY_ID_CTRL_A = 101;
        public const int HOTKEY_ID_CTRL_Z = 102;
        public const int HOTKEY_ID_CTRL_Y = 103;
        public const int HOTKEY_ID_ESC = 104;
        public const int HOTKEY_ID_CTRL_C = 105;
        public const int HOTKEY_ID_CTRL_S = 106;

        // O .NET 10 usa LibraryImport para gerar chamadas nativas otimizadas
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Regista os atalhos locais de forma global enquanto o overlay estiver aberto.
        /// Impede que eventos vazem para programas em segundo plano (como DBeaver ou Teams).
        /// </summary>
        public static void RegisterOverlayHotkeys(IntPtr windowHandle)
        {
            RegisterHotKey(windowHandle, HOTKEY_ID_CTRL_A, MOD_CONTROL, (uint)Keys.A);
            RegisterHotKey(windowHandle, HOTKEY_ID_CTRL_Z, MOD_CONTROL, (uint)Keys.Z);
            RegisterHotKey(windowHandle, HOTKEY_ID_CTRL_Y, MOD_CONTROL, (uint)Keys.Y);
            RegisterHotKey(windowHandle, HOTKEY_ID_ESC, MOD_NONE, (uint)Keys.Escape);
            RegisterHotKey(windowHandle, HOTKEY_ID_CTRL_C, MOD_CONTROL, (uint)Keys.C);
            RegisterHotKey(windowHandle, HOTKEY_ID_CTRL_S, MOD_CONTROL, (uint)Keys.S);
        }

        /// <summary>
        /// Liberta os atalhos do overlay para o sistema operativo.
        /// </summary>
        public static void UnregisterOverlayHotkeys(IntPtr windowHandle)
        {
            UnregisterHotKey(windowHandle, HOTKEY_ID_CTRL_A);
            UnregisterHotKey(windowHandle, HOTKEY_ID_CTRL_Z);
            UnregisterHotKey(windowHandle, HOTKEY_ID_CTRL_Y);
            UnregisterHotKey(windowHandle, HOTKEY_ID_ESC);
            UnregisterHotKey(windowHandle, HOTKEY_ID_CTRL_C);
            UnregisterHotKey(windowHandle, HOTKEY_ID_CTRL_S);
        }
    }
}