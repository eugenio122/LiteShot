using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LiteShot.Core
{
    /// <summary>
    /// Contém as chamadas nativas (P/Invoke) para a API do Windows (user32.dll).
    /// Responsável por gerir atalhos de teclado (Hotkeys) tanto de forma global (segundo plano) 
    /// quanto de forma local (enquanto o overlay de recorte está aberto).
    /// </summary>
    public partial class HotkeyManager
    {
        /// <summary>Mensagem padrão do Windows que indica que uma Hotkey registada foi pressionada.</summary>
        public const int WM_HOTKEY = 0x0312;

        /// <summary>Modificador: Nenhuma tecla especial.</summary>
        public const uint MOD_NONE = 0x0000;
        /// <summary>Modificador: Tecla Alt.</summary>
        public const uint MOD_ALT = 0x0001;
        /// <summary>Modificador: Tecla Control.</summary>
        public const uint MOD_CONTROL = 0x0002;
        /// <summary>Modificador: Tecla Shift.</summary>
        public const uint MOD_SHIFT = 0x0004;
        /// <summary>Modificador: Tecla Windows.</summary>
        public const uint MOD_WIN = 0x0008;

        /// <summary>Código virtual da tecla PrintScreen.</summary>
        public const uint VK_PRINTSCREEN = 0x2C;

        // --- IDENTIFICADORES DE ATALHOS TEMPORÁRIOS DO OVERLAY ---
        public const int HOTKEY_ID_CTRL_A = 101;
        public const int HOTKEY_ID_CTRL_Z = 102;
        public const int HOTKEY_ID_CTRL_Y = 103;
        public const int HOTKEY_ID_ESC = 104;
        public const int HOTKEY_ID_CTRL_C = 105;
        public const int HOTKEY_ID_CTRL_S = 106;

        /// <summary>
        /// Chamada nativa ao Windows para registar um atalho global.
        /// O .NET 7/8/9/10 usa LibraryImport para gerar código nativo de alta performance (AOT).
        /// </summary>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        /// Chamada nativa ao Windows para libertar um atalho global previamente registado.
        /// </summary>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Regista os atalhos locais de edição (Copiar, Salvar, Desfazer) de forma global 
        /// APENAS enquanto o overlay estiver aberto. Impede que estes comandos vazem 
        /// para programas de fundo (como IDEs ou navegadores).
        /// </summary>
        /// <param name="windowHandle">O ponteiro (Handle) da janela de overlay.</param>
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
        /// Liberta os atalhos temporários de edição, devolvendo o controlo das teclas 
        /// (Ctrl+C, Ctrl+Z, etc.) para o sistema operativo e outras aplicações ativas.
        /// </summary>
        /// <param name="windowHandle">O ponteiro (Handle) da janela de overlay.</param>
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