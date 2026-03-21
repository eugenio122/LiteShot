using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    /// <summary>
    /// A tela de configurações principal (Opções).
    /// Permite alterar atalhos, idiomas, notificações e comportamento do cursor.
    /// </summary>
    public partial class SettingsForm : Form
    {
        private MainContext context;
        private CheckBox chkNotifications;
        private CheckBox chkCursor;
        private CheckBox chkNavbarVertical;
        private CheckBox chkKeepSelection;
        private CheckBox chkKeepNavbar; 
        private ComboBox cmbFormat;
        private ComboBox cmbLang;
        private TextBox txtHotkey;
        private Button btnSave;

        private uint newModifier;
        private uint newKey;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int cchSize);

        public SettingsForm(MainContext ctx)
        {
            this.context = ctx;
            this.Text = LanguageManager.GetString("SettingsTitle");
            this.Size = new Size(350, 490);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeUI();
            LoadCurrentSettings();
        }

        /// <summary>Monta a interface de usuário dinamicamente via código.</summary>
        private void InitializeUI()
        {
            int yPos = 20;

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
            cmbFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(140, yPos - 3), Width = 150 };
            cmbFormat.Items.AddRange(new string[] { "PNG", "JPEG", "BMP" });
            this.Controls.Add(cmbFormat);

            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("HotkeyLabel"), AutoSize = true, Location = new Point(20, yPos) });
            txtHotkey = new TextBox { Location = new Point(140, yPos - 3), ReadOnly = true, Width = 85, TextAlign = HorizontalAlignment.Center };
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            this.Controls.Add(txtHotkey);

            Button btnReset = new Button { Text = LanguageManager.GetString("BtnReset"), Location = new Point(230, yPos - 4), Width = 65, Height = 25 };
            btnReset.Click += (s, e) => {
                newModifier = HotkeyManager.MOD_NONE;
                newKey = HotkeyManager.VK_PRINTSCREEN;
                txtHotkey.Text = "PrintScreen";
            };
            this.Controls.Add(btnReset);

            yPos += 40;
            this.Controls.Add(new Label { Text = LanguageManager.GetString("LangLabel"), AutoSize = true, Location = new Point(20, yPos) });
            cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(140, yPos - 3), Width = 150 };
            cmbLang.Items.AddRange(new string[] { "pt-BR", "en-US", "es-ES", "fr-FR", "de-DE", "it-IT" });
            this.Controls.Add(cmbLang);

            yPos += 60;
            btnSave = new Button { Text = LanguageManager.GetString("BtnSave"), Location = new Point(110, yPos), Width = 120, Height = 30 };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);
        }

        /// <summary>Preenche os campos do formulário com os valores atuais em memória.</summary>
        private void LoadCurrentSettings()
        {
            chkNotifications.Checked = MainContext.ShowNotifications;
            chkCursor.Checked = MainContext.CaptureCursor;
            chkNavbarVertical.Checked = MainContext.NavbarVertical;
            chkKeepSelection.Checked = MainContext.KeepSelection;
            chkKeepNavbar.Checked = MainContext.KeepNavbarPosition; 
            cmbFormat.SelectedItem = MainContext.ImageFormat;
            cmbLang.SelectedItem = LanguageManager.CurrentLanguage;

            newModifier = MainContext.CurrentHotkeyModifier;
            newKey = MainContext.CurrentHotkey;
            txtHotkey.Text = ObterNomeAtalhoAtual();
        }

        /// <summary>Usa as APIs do Windows para converter um código de tecla (Ex: 191) no caractere real do teclado local (Ex: ; ou /).</summary>
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

        /// <summary>Captura a combinação de teclas digitada pelo usuário e atualiza a interface.</summary>
        private void TxtHotkey_KeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true; // Impede que o texto seja digitado

            // Ignora se for apenas uma tecla modificadora (Ctrl, Shift, Alt)
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
                return;

            // Validação de teclas reservadas
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

        /// <summary>Salva as novas opções no disco, aplica a hotkey e atualiza o idioma.</summary>
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
            SettingsManager.Save(config);

            context.RegisterGlobalHotkey();
            context.AtualizarTextosInterface();

            this.Close();
        }
    }
}