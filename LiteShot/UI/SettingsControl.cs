using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    public class SettingsControl : UserControl
    {
        private MainContext? context;
        private CheckBox chkNotifications;
        private CheckBox chkCursor;
        private CheckBox chkNavbarVertical;
        private CheckBox chkKeepSelection;
        private CheckBox chkKeepNavbar;
        private ComboBox cmbFormat;
        private ComboBox cmbResolution;
        private ComboBox cmbLang;
        private TextBox txtHotkey;
        private Button btnSave;

        private uint newModifier;
        private uint newKey;

        public event Action? RequestClose;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int cchSize);

        public SettingsControl(MainContext? ctx)
        {
            this.context = ctx;

            this.Size = new Size(430, 480);
            this.BackColor = SystemColors.Control;

            InitializeUI();
            LoadCurrentSettings();
        }

        private void InitializeUI()
        {
            int yPos = 20;
            int controlesX = 210;

            chkNotifications = new CheckBox { Text = LanguageManager.GetString("ShowNotifications"), AutoSize = true, Location = new Point(20, yPos) };
            this.Controls.Add(chkNotifications);

            yPos += 30;
            chkCursor = new CheckBox { Text = LanguageManager.GetString("CaptureCursor"), AutoSize = true, Location = new Point(20, yPos) };
            this.Controls.Add(chkCursor);

            yPos += 30;
            chkNavbarVertical = new CheckBox { Text = LanguageManager.GetString("NavbarVertical"), AutoSize = true, Location = new Point(20, yPos) };
            this.Controls.Add(chkNavbarVertical);

            yPos += 30;
            chkKeepSelection = new CheckBox { Text = LanguageManager.GetString("KeepSelection"), AutoSize = true, Location = new Point(20, yPos) };
            this.Controls.Add(chkKeepSelection);

            yPos += 30;
            chkKeepNavbar = new CheckBox { Text = LanguageManager.GetString("KeepNavbarPosition"), AutoSize = true, Location = new Point(20, yPos) };
            this.Controls.Add(chkKeepNavbar);

            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("ImgFormat"), AutoSize = true, Location = new Point(20, yPos) });
            cmbFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(controlesX, yPos - 3), Width = 180 };
            cmbFormat.Items.AddRange(new string[] { "PNG", "JPEG", "BMP" });
            this.Controls.Add(cmbFormat);

            // AGORA USANDO O LANGUAGEMANAGER
            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("ResLabel"), AutoSize = true, Location = new Point(20, yPos) });
            cmbResolution = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(controlesX, yPos - 3), Width = 180 };
            cmbResolution.Items.AddRange(new string[] {
                LanguageManager.GetString("ResAuto"),
                LanguageManager.GetString("Res4K"),
                LanguageManager.GetString("ResQHD"),
                LanguageManager.GetString("ResFHD"),
                LanguageManager.GetString("Res1600"),
                LanguageManager.GetString("Res1366"),
                LanguageManager.GetString("Res720p"),
                LanguageManager.GetString("Res480p")
            });
            this.Controls.Add(cmbResolution);

            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("HotkeyLabel"), AutoSize = true, Location = new Point(20, yPos) });
            txtHotkey = new TextBox { Location = new Point(controlesX, yPos - 3), ReadOnly = true, Width = 100, TextAlign = HorizontalAlignment.Center };
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            this.Controls.Add(txtHotkey);

            Button btnReset = new Button { Text = LanguageManager.GetString("BtnReset"), Location = new Point(controlesX + 105, yPos - 4), Width = 75, Height = 25 };
            btnReset.Click += (s, e) => {
                newModifier = HotkeyManager.MOD_NONE;
                newKey = HotkeyManager.VK_PRINTSCREEN;
                txtHotkey.Text = "PrintScreen";
            };
            this.Controls.Add(btnReset);

            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("LangLabel"), AutoSize = true, Location = new Point(20, yPos) });
            cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(controlesX, yPos - 3), Width = 180 };
            cmbLang.Items.AddRange(new string[] { "pt-BR", "en-US", "es-ES", "fr-FR", "de-DE", "it-IT" });
            this.Controls.Add(cmbLang);

            yPos += 60;
            btnSave = new Button { Text = LanguageManager.GetString("BtnSave"), Location = new Point((this.Width - 120) / 2, yPos), Width = 120, Height = 30 };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);
        }

        private void LoadCurrentSettings()
        {
            chkNotifications.Checked = MainContext.ShowNotifications;
            chkCursor.Checked = MainContext.CaptureCursor;
            chkNavbarVertical.Checked = MainContext.NavbarVertical;
            chkKeepSelection.Checked = MainContext.KeepSelection;
            chkKeepNavbar.Checked = MainContext.KeepNavbarPosition;
            cmbFormat.SelectedItem = MainContext.ImageFormat;
            cmbLang.SelectedItem = LanguageManager.CurrentLanguage;

            string currentRes = MainContext.CaptureResolution;
            int resIndex = -1;
            for (int i = 0; i < cmbResolution.Items.Count; i++)
            {
                if (cmbResolution.Items[i].ToString().StartsWith(currentRes))
                {
                    resIndex = i;
                    break;
                }
            }

            if (resIndex == -1)
            {
                cmbResolution.Items.Add($"{currentRes} (Personalizado)");
                cmbResolution.SelectedIndex = cmbResolution.Items.Count - 1;
            }
            else
            {
                cmbResolution.SelectedIndex = resIndex;
            }

            newModifier = MainContext.CurrentHotkeyModifier;
            newKey = MainContext.CurrentHotkey;
            txtHotkey.Text = ObterNomeAtalhoAtual();

            // Se o LiteShot estiver rodando como Plugin (dentro do LiteTools), 
            // desativamos o combobox de idioma para que o utilizador só possa alterá-lo no Host.
            if (context != null && context.IsPluginMode)
            {
                cmbLang.Enabled = false;
            }
        }

        private string GetLocalizedKeyName(uint vk)
        {
            if (vk == HotkeyManager.VK_PRINTSCREEN) return "PrintScreen";
            switch (vk)
            {
                case 193: return "/";
                case 191: return ";";
                case 186: return "Ç";
                case 188: return ",";
                case 190: return ".";
                case 194: return ".";
                case 187: return "=";
                case 189: return "-";
                case 226: return "\\";
            }
            uint scanCode = MapVirtualKey(vk, 0);
            int lParam = (int)(scanCode << 16);
            if (vk >= 33 && vk <= 46) lParam |= 0x1000000;
            StringBuilder sb = new StringBuilder(256);
            if (GetKeyNameText(lParam, sb, 256) > 0) return sb.ToString();
            string name = ((Keys)vk).ToString();
            return (name == "None" || vk == 0) ? $"Key {vk}" : name;
        }

        private string ObterNomeAtalhoAtual()
        {
            string name = "";
            if ((newModifier & HotkeyManager.MOD_CONTROL) != 0) name += "Ctrl + ";
            if ((newModifier & HotkeyManager.MOD_SHIFT) != 0) name += "Shift + ";
            if ((newModifier & HotkeyManager.MOD_ALT) != 0) name += "Alt + ";
            name += GetLocalizedKeyName(newKey);
            return name;
        }

        private void TxtHotkey_KeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
                return;

            Keys[] reservedKeys = { Keys.A, Keys.C, Keys.S, Keys.Z, Keys.Y, Keys.Escape, Keys.Oemplus, Keys.Add, Keys.OemMinus, Keys.Subtract };

            if (Array.Exists(reservedKeys, key => key == e.KeyCode))
            {
                MessageBox.Show(
                    "Esta tecla já é usada como um atalho interno do LiteShot. Por favor, escolha outra.",
                    "Atalho Reservado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            uint modifiers = HotkeyManager.MOD_NONE;
            if (e.Control) modifiers |= HotkeyManager.MOD_CONTROL;
            if (e.Shift) modifiers |= HotkeyManager.MOD_SHIFT;
            if (e.Alt) modifiers |= HotkeyManager.MOD_ALT;
            uint vk = (uint)e.KeyValue;
            if (vk == 0) vk = (uint)e.KeyCode;
            txtHotkey.Text = (e.Control ? "Ctrl + " : "") + (e.Shift ? "Shift + " : "") + (e.Alt ? "Alt + " : "") + GetLocalizedKeyName(vk);
            newModifier = modifiers; newKey = vk;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            MainContext.ShowNotifications = chkNotifications.Checked;
            MainContext.CaptureCursor = chkCursor.Checked;
            MainContext.NavbarVertical = chkNavbarVertical.Checked;
            MainContext.KeepSelection = chkKeepSelection.Checked;
            MainContext.KeepNavbarPosition = chkKeepNavbar.Checked;
            MainContext.ImageFormat = cmbFormat.SelectedItem?.ToString() ?? "PNG";
            LanguageManager.CurrentLanguage = cmbLang.SelectedItem?.ToString() ?? "pt-BR";
            MainContext.CurrentHotkeyModifier = newModifier;
            MainContext.CurrentHotkey = newKey;

            MainContext.CaptureResolution = cmbResolution.SelectedItem?.ToString().Split(' ')[0] ?? "Auto";

            AppSettings config = SettingsManager.Load();
            config.ShowNotifications = MainContext.ShowNotifications;
            config.CaptureCursor = MainContext.CaptureCursor;
            config.NavbarVertical = MainContext.NavbarVertical;
            config.KeepSelection = MainContext.KeepSelection;
            config.KeepNavbarPosition = MainContext.KeepNavbarPosition;
            config.ImageFormat = MainContext.ImageFormat;
            config.Language = LanguageManager.CurrentLanguage;
            config.HotkeyModifier = MainContext.CurrentHotkeyModifier;
            config.Hotkey = MainContext.CurrentHotkey;
            config.CaptureResolution = MainContext.CaptureResolution;
            SettingsManager.Save(config);

            if (context != null)
            {
                context.RegisterGlobalHotkey();
                context.AtualizarTextosInterface();
            }

            // Exibe a notificação com a chave Certa para as configurações
            MainContext.ShowToast(LanguageManager.GetString("SettingsSaved"), null);
        }
    }
}