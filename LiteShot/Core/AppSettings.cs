using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace LiteShot.Core
{
    /// <summary>
    /// Representa a estrutura de dados do arquivo 'liteshot_settings.json'.
    /// Contém todas as preferências persistentes do usuário.
    /// </summary>
    public class AppSettings
    {
        public bool ShowNotifications { get; set; } = true;
        public bool CaptureCursor { get; set; } = false;
        public string ImageFormat { get; set; } = "PNG";
        public uint HotkeyModifier { get; set; } = HotkeyManager.MOD_NONE;
        public uint Hotkey { get; set; } = HotkeyManager.VK_PRINTSCREEN;

        public string LastColor { get; set; } = "#FF0000"; // Vermelho por padrão
        public int[] CustomColors { get; set; } = new int[16]; // 16 slots de cores personalizadas do Windows

        public string Language { get; set; } = "pt-BR";
    }

    /// <summary>
    /// Responsável por carregar (Load) e gravar (Save) o objeto AppSettings 
    /// no disco usando serialização JSON. Mantém o aplicativo portátil.
    /// </summary>
    public static class SettingsManager
    {
        // O ficheiro json será guardado na mesma pasta do executável
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liteshot_settings.json");

        /// <summary>Lê o JSON do diretório atual. Se falhar, retorna configurações padrão.</summary>
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
                // Regista o erro para os programadores na aba de "Saída" do Visual Studio
                // Garante que a aplicação portátil não crasha no PC do utilizador final
                Debug.WriteLine($"[LiteShot] Erro ao carregar configurações: {ex.Message}");
            }

            return new AppSettings();
        }

        /// <summary>Grava as configurações atuais no arquivo JSON.</summary>
        public static void Save(AppSettings settings)
        {
            try
            {
                // Escreve o JSON de forma indentada e bonita para ser fácil de ler
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Regista o erro (ex: sem permissões de escrita na pasta 'Program Files')
                Debug.WriteLine($"[LiteShot] Erro ao guardar configurações: {ex.Message}");
            }
        }
    }
}