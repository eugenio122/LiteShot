using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    /// <summary>
    /// O painel central de configurações (UserControl).
    /// Contém toda a interface de opções. Foi arquitetado com o Padrão Wrapper para ser embutível,
    /// podendo rodar numa janela standalone (LiteShot.exe) ou numa aba do Host (LiteTools.dll).
    /// </summary>
    public class SettingsControl : UserControl
    {
        private MainContext? context;
        private TableLayoutPanel mainLayout;
        private CheckBox chkDarkMode;
        private CheckBox chkNotifications;
        private CheckBox chkCursor;
        private CheckBox chkNavbarVertical;
        private CheckBox chkKeepSelection;
        private CheckBox chkKeepNavbar;
        private ComboBox cmbFormat;
        private ComboBox cmbResolution;
        private ComboBox cmbLang;
        private TextBox txtHotkey;
        private Button btnReset;
        private Button btnSave;

        private uint newModifier;
        private uint newKey;

        /// <summary>
        /// Evento legado (não mais utilizado ativamente) para solicitar o fechamento da janela pai.
        /// Mantido para retrocompatibilidade de design.
        /// </summary>
        public event Action? RequestClose;

        /// <summary>Chamada nativa da API do Windows para converter códigos virtuais de teclas em ScanCodes físicos.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>Chamada nativa da API do Windows para obter o nome traduzido de uma tecla com base no ScanCode.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int cchSize);

        /// <summary>
        /// Inicializa o painel de opções, renderiza a interface e sincroniza os dados com o contexto ativo.
        /// Subscreve também aos eventos do Tema Global do LiteTools.
        /// </summary>
        /// <param name="ctx">O "Cérebro" ativo do aplicativo.</param>
        public SettingsControl(MainContext? ctx)
        {
            this.context = ctx;

            this.Size = new Size(430, 520);
            this.BackColor = SystemColors.Control;
            this.Padding = new Padding(15); // Margem de respiro para toda a janela

            InitializeResponsiveUI();
            LoadCurrentSettings();

            // Aplica o tema atual logo de início
            ApplyTheme(MainContext.IsDarkMode);

            // Se for plugin, fica a ouvir as mudanças de tema disparadas pela Nave-Mãe
            if (this.context != null)
            {
                this.context.OnThemeUpdated += ApplyTheme;
            }
        }


        /// <summary>
        /// Adapta dinamicamente as cores de fundo, textos, botões e controlos 
        /// de acordo com a ordem do Tema Global (Claro/Escuro) recebida pelo Host.
        /// </summary>
        /// <param name="isDark">True se o tema global for escuro; caso contrário, False.</param>
        private void ApplyTheme(bool isDark)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ApplyTheme(isDark)));
                return;
            }

            this.BackColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            this.ForeColor = isDark ? Color.White : Color.Black;

            ApplyThemeToControls(this.Controls, isDark);
        }

        /// <summary>Função auxiliar para aplicar cores em profundidade (em todos os filhos do Layout).</summary>
        private void ApplyThemeToControls(Control.ControlCollection controls, bool isDark)
        {
            foreach (Control c in controls)
            {
                if (c is CheckBox || c is Label)
                {
                    c.ForeColor = isDark ? Color.White : Color.Black;
                }
                else if (c is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.BackColor = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.ControlLight;
                    btn.ForeColor = isDark ? Color.White : Color.Black;
                    btn.FlatAppearance.BorderColor = isDark ? Color.Gray : Color.DarkGray;
                }
                else if (c is ComboBox cmb)
                {
                    cmb.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.White;
                    cmb.ForeColor = isDark ? Color.White : Color.Black;
                    cmb.FlatStyle = FlatStyle.Flat;
                }
                else if (c is TextBox txt)
                {
                    txt.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.White;
                    txt.ForeColor = isDark ? Color.White : Color.Black;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is TableLayoutPanel || c is FlowLayoutPanel || c is Panel)
                {
                    ApplyThemeToControls(c.Controls, isDark); // Pesquisa recursiva dentro dos contentores
                }
            }
        }

        /// <summary>
        /// Desenha a interface utilizando o TableLayoutPanel para garantir responsividade absoluta.
        /// Utiliza o "Truque da 3ª Coluna" para evitar que as comboboxes estiquem demasiado no ecrã inteiro.
        /// </summary>
        private void InitializeResponsiveUI()
        {
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top, // Top evita que os elementos se estiquem verticalmente na DLL
                ColumnCount = 3,      // O segredo anti-esticamento: 3 colunas!
                RowCount = 12,
                AutoSize = true
            };

            // Coluna 0: Labels (Texto) - Tamanho ajustável ao idioma
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            // Coluna 1: Inputs (ComboBoxes/Textboxes) - Ajustável ao conteúdo
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            // Coluna 2: Espaçador Invisível - Ocupa os 100% restantes e empurra a UI para a esquerda
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // --- CHECKBOXES (Ocupam as 3 colunas para segurança) ---
            chkDarkMode = new CheckBox { Text = LanguageManager.GetString("DarkMode"), AutoSize = true, Margin = new Padding(3, 5, 3, 5) };
            mainLayout.Controls.Add(chkDarkMode, 0, 0);
            mainLayout.SetColumnSpan(chkDarkMode, 3);

            chkNotifications = new CheckBox { Text = LanguageManager.GetString("ShowNotifications"), AutoSize = true, Margin = new Padding(3, 5, 3, 5) };
            mainLayout.Controls.Add(chkNotifications, 0, 1);
            mainLayout.SetColumnSpan(chkNotifications, 3);

            chkCursor = new CheckBox { Text = LanguageManager.GetString("CaptureCursor"), AutoSize = true, Margin = new Padding(3, 5, 3, 5) };
            mainLayout.Controls.Add(chkCursor, 0, 2);
            mainLayout.SetColumnSpan(chkCursor, 3);

            chkNavbarVertical = new CheckBox { Text = LanguageManager.GetString("NavbarVertical"), AutoSize = true, Margin = new Padding(3, 5, 3, 5) };
            mainLayout.Controls.Add(chkNavbarVertical, 0, 3);
            mainLayout.SetColumnSpan(chkNavbarVertical, 3);

            chkKeepSelection = new CheckBox { Text = LanguageManager.GetString("KeepSelection"), AutoSize = true, Margin = new Padding(3, 5, 3, 5) };
            mainLayout.Controls.Add(chkKeepSelection, 0, 4);
            mainLayout.SetColumnSpan(chkKeepSelection, 3);

            chkKeepNavbar = new CheckBox { Text = LanguageManager.GetString("KeepNavbarPosition"), AutoSize = true, Margin = new Padding(3, 5, 3, 15) };
            mainLayout.Controls.Add(chkKeepNavbar, 0, 5);
            mainLayout.SetColumnSpan(chkKeepNavbar, 3);

            // --- CAMPOS (Largura Fixa de 250px e Ancorados apenas à Esquerda) ---

            // Formato de Imagem
            Label lblFormat = new Label { Text = LanguageManager.GetString("ImgFormat"), Anchor = AnchorStyles.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            cmbFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left, Width = 250, Margin = new Padding(3, 3, 3, 10) };
            cmbFormat.Items.AddRange(new string[] { "PNG", "JPEG", "BMP" });
            mainLayout.Controls.Add(lblFormat, 0, 6);
            mainLayout.Controls.Add(cmbFormat, 1, 6);

            // Resolução
            Label lblRes = new Label { Text = LanguageManager.GetString("ResLabel"), Anchor = AnchorStyles.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            cmbResolution = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left, Width = 250, Margin = new Padding(3, 3, 3, 10) };
            cmbResolution.Items.AddRange(new string[] {
                LanguageManager.GetString("ResAuto"), LanguageManager.GetString("Res4K"),
                LanguageManager.GetString("ResQHD"), LanguageManager.GetString("ResFHD"),
                LanguageManager.GetString("Res1600"), LanguageManager.GetString("Res1366"),
                LanguageManager.GetString("Res720p"), LanguageManager.GetString("Res480p")
            });
            mainLayout.Controls.Add(lblRes, 0, 7);
            mainLayout.Controls.Add(cmbResolution, 1, 7);

            // Tecla de Atalho (Agrupada num FlowLayoutPanel para não quebrar a grelha)
            Label lblHotkey = new Label { Text = LanguageManager.GetString("HotkeyLabel"), Anchor = AnchorStyles.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };

            FlowLayoutPanel hotkeyLayout = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            txtHotkey = new TextBox { ReadOnly = true, TextAlign = HorizontalAlignment.Center, Width = 150, Margin = new Padding(3, 3, 5, 10) };
            txtHotkey.KeyDown += TxtHotkey_KeyDown;

            btnReset = new Button { Text = LanguageManager.GetString("BtnReset"), Width = 75, Height = 25, Margin = new Padding(0, 1, 0, 0) };
            btnReset.Click += (s, e) => { newModifier = HotkeyManager.MOD_NONE; newKey = HotkeyManager.VK_PRINTSCREEN; txtHotkey.Text = "PrintScreen"; };

            hotkeyLayout.Controls.Add(txtHotkey);
            hotkeyLayout.Controls.Add(btnReset);

            mainLayout.Controls.Add(lblHotkey, 0, 8);
            mainLayout.Controls.Add(hotkeyLayout, 1, 8);

            // Idioma
            Label lblLang = new Label { Text = LanguageManager.GetString("LangLabel"), Anchor = AnchorStyles.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left, Width = 250, Margin = new Padding(3, 3, 3, 10) };
            cmbLang.Items.AddRange(new string[] { "pt-BR", "en-US", "es-ES", "fr-FR", "de-DE", "it-IT" });
            mainLayout.Controls.Add(lblLang, 0, 9);
            mainLayout.Controls.Add(cmbLang, 1, 9);

            // Botão Salvar (Centrado entre as duas colunas principais)
            btnSave = new Button { Text = LanguageManager.GetString("BtnSave"), Width = 150, Height = 35, Anchor = AnchorStyles.None, Margin = new Padding(0, 20, 0, 0) };
            btnSave.Click += BtnSave_Click;
            mainLayout.Controls.Add(btnSave, 0, 10);
            mainLayout.SetColumnSpan(btnSave, 2);

            this.Controls.Add(mainLayout);
        }

        /// <summary>Puxa os dados ativos da memória e ajusta a interface de acordo com o Modo (Plugin ou Standalone).</summary>
        private void LoadCurrentSettings()
        {
            chkDarkMode.Checked = MainContext.IsDarkMode;
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
                cmbResolution.Items.Add($"{currentRes} {LanguageManager.GetString("ResCustom")}");
                cmbResolution.SelectedIndex = cmbResolution.Items.Count - 1;
            }
            else
            {
                cmbResolution.SelectedIndex = resIndex;
            }

            newModifier = MainContext.CurrentHotkeyModifier;
            newKey = MainContext.CurrentHotkey;
            txtHotkey.Text = ObterNomeAtalhoAtual();

            // Lógica Exclusiva: Quando rodar dentro da Nave-Mãe (LiteTools), bloqueamos Tema e Idioma
            if (context != null && context.IsPluginMode)
            {
                cmbLang.Enabled = false;

                chkDarkMode.Enabled = false;
                chkDarkMode.Visible = false; // Esconde para não causar confusão, e a UI flui magicamente graças ao TableLayoutPanel
            }
        }

        /// <summary>
        /// Converte o código da tecla do Windows num nome de tecla amigável para o utilizador.
        /// Resolve problemas de teclados internacionais usando as APIs nativas do Windows.
        /// </summary>
        /// <param name="vk">Virtual Key Code capturado pelo sistema.</param>
        /// <returns>O nome da tecla traduzido (ex: "PrintScreen", "/", "=").</returns>
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

        /// <summary>
        /// Constrói a string visual do atalho de teclado inteiro, unindo os Modificadores (Ctrl, Shift) e a Tecla.
        /// </summary>
        private string ObterNomeAtalhoAtual()
        {
            string name = "";
            if ((newModifier & HotkeyManager.MOD_CONTROL) != 0) name += "Ctrl + ";
            if ((newModifier & HotkeyManager.MOD_SHIFT) != 0) name += "Shift + ";
            if ((newModifier & HotkeyManager.MOD_ALT) != 0) name += "Alt + ";
            name += GetLocalizedKeyName(newKey);
            return name;
        }

        /// <summary>
        /// Interceta as teclas digitadas na caixa de atalhos e previne conflitos com 
        /// atalhos vitais reservados para o funcionamento interno do LiteShot (ex: Ctrl+C, Esc).
        /// </summary>
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

        /// <summary>Recolhe os dados, atualiza a memória local, guarda no JSON e aplica as regras.</summary>
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

            // Aplica o tema imediatamente se estiver rodando sozinho (.exe)
            if (context != null && !context.IsPluginMode)
            {
                MainContext.IsDarkMode = chkDarkMode.Checked;
                ApplyTheme(MainContext.IsDarkMode);
            }

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
            config.IsDarkMode = MainContext.IsDarkMode;

            SettingsManager.Save(config);

            if (context != null)
            {
                context.RegisterGlobalHotkey();
                context.AtualizarTextosInterface();
            }

            MainContext.ShowToast(LanguageManager.GetString("SettingsSaved"), null);
        }

        /// <summary>
        /// Remove com segurança os *event listeners* ao destruir o controlo 
        /// para evitar vazamentos de memória (Memory Leaks).
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && context != null)
            {
                context.OnThemeUpdated -= ApplyTheme;
            }
            base.Dispose(disposing);
        }
    }
}