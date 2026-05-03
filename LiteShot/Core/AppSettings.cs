using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace LiteShot.Core
{
    /// <summary>
    /// Representa a estrutura de dados do arquivo 'liteshot_settings.json'.
    /// Contém todas as preferências persistentes do utilizador, atuando como a "memória" do LiteShot.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Define se exibe o balão de notificação após copiar ou salvar uma imagem.</summary>
        public bool ShowNotifications { get; set; } = true;

        /// <summary>Define se o cursor do rato deve ser desenhado na captura final.</summary>
        public bool CaptureCursor { get; set; } = false;

        /// <summary>Formato de imagem preferido para salvar o ficheiro (ex: PNG, JPEG).</summary>
        public string ImageFormat { get; set; } = "PNG";

        /// <summary>
        /// Limite de resolução da captura. Deixado em branco por padrão para que o 
        /// MainContext decida entre 'Auto' (.exe) ou detetar o limite do monitor (.dll).
        /// </summary>
        public string CaptureResolution { get; set; } = "";

        /// <summary>Modificador da tecla de atalho global (ex: Ctrl, Shift, Alt).</summary>
        public uint HotkeyModifier { get; set; } = HotkeyManager.MOD_NONE;

        /// <summary>Código virtual da tecla de atalho global (Padrão: PrintScreen).</summary>
        public uint Hotkey { get; set; } = HotkeyManager.VK_PRINTSCREEN;

        /// <summary>Última cor utilizada nas ferramentas de desenho (em formato Hexadecimal).</summary>
        public string LastColor { get; set; } = "#FF0000";

        /// <summary>Última cor utilizada especificamente na ferramenta Marcador/Highlighter.</summary>
        public string LastHighlightColor { get; set; } = "#FFFF00";

        /// <summary>Guarda os 16 slots de cores personalizadas definidas pelo utilizador no ColorDialog.</summary>
        public int[] CustomColors { get; set; } = new int[16];

        /// <summary>
        /// Modo escuro ativado ou desativado. Em modo Plugin, este valor será sempre
        /// sobrescrito pelo tema injetado pela Nave-Mãe (LiteTools).
        /// </summary>
        public bool IsDarkMode { get; set; } = true;

        /// <summary>
        /// Idioma atual do painel. Em modo Plugin, este valor será sempre 
        /// sobrescrito pelo idioma injetado pela Nave-Mãe (LiteTools).
        /// </summary>
        public string Language { get; set; } = "pt-BR";

        /// <summary>Define se a área de seleção (Bounding Box) já nasce a ocupar o ecrã inteiro.</summary>
        public bool FullScreenMode { get; set; } = false;

        /// <summary>Define se a barra de ferramentas (Navbar) flutuante deve ser disposta na vertical.</summary>
        public bool NavbarVertical { get; set; } = false;

        /// <summary>Define se o software deve lembrar o tamanho e posição da última área recortada.</summary>
        public bool KeepSelection { get; set; } = false;

        /// <summary>Define se o software deve lembrar a posição customizada da barra de ferramentas (Navbar).</summary>
        public bool KeepNavbarPosition { get; set; } = false;

        /// <summary>A coordenada exata da última seleção feita (se KeepSelection for true).</summary>
        public Rectangle LastSelection { get; set; } = Rectangle.Empty;

        /// <summary>A coordenada exata da última posição da barra de ferramentas (se KeepNavbarPosition for true).</summary>
        public Point LastNavbarPosition { get; set; } = Point.Empty;
    }

    /// <summary>
    /// Responsável por carregar (Load) e gravar (Save) o objeto AppSettings 
    /// no disco usando serialização JSON. Garante a persistência das configurações.
    /// </summary>
    public static class SettingsManager
    {
        // O ficheiro json será sempre guardado na mesma pasta do executável principal
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liteshot_settings.json");

        /// <summary>
        /// Lê o ficheiro JSON do diretório atual e converte num objeto AppSettings.
        /// Se o ficheiro não existir ou corromper, retorna as configurações padrão em segurança.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LiteShot] Erro ao carregar configurações: {ex.Message}");
            }

            return new AppSettings();
        }

        /// <summary>
        /// Serializa o objeto AppSettings para formato JSON (com indentação para fácil leitura) 
        /// e grava no disco.
        /// </summary>
        /// <param name="settings">O objeto contendo as configurações atualizadas.</param>
        public static void Save(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LiteShot] Erro ao guardar configurações: {ex.Message}");
            }
        }
    }
}