using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteShot.Core;
using LiteTools.Interfaces;

namespace LiteShot.UI
{
    /// <summary>
    /// O "Cérebro" do aplicativo em execução. 
    /// Substitui o Form principal padrão do Windows Forms, permitindo que o app rode 
    /// direto na bandeja do sistema (System Tray) sem uma janela sempre aberta.
    /// </summary>
    public class MainContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private HiddenMessageWindow messageWindow;
        private SelectionForm? currentSelectionForm;

        public static bool FullScreenMode = false;
        public static bool ShowNotifications = true;
        public static bool CaptureCursor = false;
        public static string ImageFormat = "PNG";

        // Resolução de captura
        public static string CaptureResolution = "1920x1080";

        public static uint CurrentHotkeyModifier = HotkeyManager.MOD_NONE;
        public static uint CurrentHotkey = HotkeyManager.VK_PRINTSCREEN;

        public static string LastColor = "#FF0000";
        public static string LastHighlightColor = "#FFFF00";
        public static int[] CustomColors = new int[16];

        public static bool NavbarVertical = false;

        // Variáveis de Memória 
        public static bool KeepSelection = false;
        public static bool KeepNavbarPosition = false;
        public static Rectangle LastSelection = Rectangle.Empty;
        public static Point LastNavbarPosition = Point.Empty;

        // Canal de comunicação para o Dual-Mode 
        private IImagePublisher? _publisher;

        private bool _isPluginMode = false;

        // Propriedade inteligente: ao mudar de valor, ela atualiza o ícone na hora.
        public bool IsPluginMode
        {
            get => _isPluginMode;
            set
            {
                _isPluginMode = value;
                if (trayIcon != null)
                {
                    // Se ativou o modo plugin, o ícone fica invisível (!true = false)
                    trayIcon.Visible = !_isPluginMode;
                }
            }
        }

        /// <summary>Construtor Modo Standalone (.exe)</summary>
        public MainContext()
        {
            InitCore(false, null, null);
        }

        /// <summary>Construtor Modo Plugin (.dll) - Agora recebe o Idioma</summary>
        public MainContext(IImagePublisher publisher, string hostLanguage)
        {
            InitCore(true, publisher, hostLanguage);
        }

        /// <summary>Inicialização centralizada para aplicar regras de arquitetura.</summary>
        private void InitCore(bool isPlugin, IImagePublisher? publisher, string? hostLanguage)
        {
            _isPluginMode = isPlugin;
            _publisher = publisher;

            // 1. Lê o que está salvo no JSON local (pode conter um idioma velho)
            CarregarConfiguracoes();

            // 2. Sincronização Global de Idioma (Se for plugin, a Nave-Mãe manda)
            if (_isPluginMode && !string.IsNullOrEmpty(hostLanguage))
            {
                LanguageManager.CurrentLanguage = hostLanguage;
            }

            // 3. Aplica as Regras de Ouro da Resolução
            if (!_isPluginMode)
            {
                // .exe: Não importa a resolução salva, esmaga e seta Automático
                CaptureResolution = "Auto";
            }
            else
            {
                // .dll: Se estiver vazio (primeira vez), detecta o monitor
                if (string.IsNullOrEmpty(CaptureResolution))
                {
                    int nativeWidth = Screen.PrimaryScreen.Bounds.Width;
                    CaptureResolution = nativeWidth > 1920 ? "1920x1080" : "Auto";
                }
            }

            // Atualiza o JSON com a decisão tomada (Idioma e Resolução alinhados)
            AppSettings config = SettingsManager.Load();
            config.CaptureResolution = CaptureResolution;
            config.Language = LanguageManager.CurrentLanguage;
            SettingsManager.Save(config);

            trayIcon = new NotifyIcon()
            {
                Icon = CreateAppIcon(),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = !_isPluginMode
            };

            AtualizarTextosInterface();

            messageWindow = new HiddenMessageWindow(this);
            RegisterGlobalHotkey();
        }

        /// <summary>Reconstrói o menu de contexto da bandeja aplicando o idioma atual.</summary>
        public void AtualizarTextosInterface()
        {
            trayIcon.Text = LanguageManager.GetString("AppTooltip");

            trayIcon.ContextMenuStrip.Items.Clear();

            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("Capturar"), null, (s, e) => TriggerScreenshot());
            trayIcon.ContextMenuStrip.Items.Add("-");

            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("SettingsTitle") + "...", null, OpenSettings);

            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("Sobre"), null, OpenAbout);

            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("Fechar"), null, Exit);
        }

        // Abrir a janela Sobre:
        private void OpenAbout(object? sender, EventArgs e)
        {
            Form about = new Form { Text = LanguageManager.GetString("Sobre"), Size = new Size(350, 320), StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lblTitle = new Label { Text = "LiteShot v2.0.0", Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            Label lblShortcuts = new Label
            {
                Text = LanguageManager.GetString("AboutShortcuts"),
                Dock = DockStyle.Top,
                Height = 160,
                Padding = new Padding(20, 10, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            LinkLabel lnk = new LinkLabel { Text = LanguageManager.GetString("AboutGitHub"), Dock = DockStyle.Bottom, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
            lnk.LinkClicked += (s, ev) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/eugenio122/LiteShot") { UseShellExecute = true });

            about.Controls.Add(lblShortcuts);
            about.Controls.Add(lblTitle);
            about.Controls.Add(lnk);
            about.ShowDialog();
        }

        /// <summary>Carrega o ícone original e aplica um zoom para remover as bordas transparentes inúteis.</summary>
        private Icon CreateAppIcon()
        {
            try
            {
#pragma warning disable CS8603 
                Icon originalIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
#pragma warning restore CS8603

                using (Bitmap originalBmp = originalIcon.ToBitmap())
                {
                    Bitmap zoomedBmp = new Bitmap(originalBmp.Width, originalBmp.Height);
                    using (Graphics g = Graphics.FromImage(zoomedBmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        float zoom = 1.6f;
                        int newWidth = (int)(originalBmp.Width * zoom);
                        int newHeight = (int)(originalBmp.Height * zoom);

                        int offsetX = (originalBmp.Width - newWidth) / 2;
                        int offsetY = (originalBmp.Height - newHeight) / 2;

                        g.DrawImage(originalBmp, offsetX, offsetY, newWidth, newHeight);
                    }

                    return Icon.FromHandle(zoomedBmp.GetHicon());
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        /// <summary>Sincroniza as variáveis estáticas com os dados salvos no JSON.</summary>
        private void CarregarConfiguracoes()
        {
            AppSettings config = SettingsManager.Load();
            ShowNotifications = config.ShowNotifications;
            CaptureCursor = config.CaptureCursor;
            ImageFormat = config.ImageFormat;
            CurrentHotkeyModifier = config.HotkeyModifier;
            CurrentHotkey = config.Hotkey;
            LastColor = config.LastColor;
            LastHighlightColor = config.LastHighlightColor;
            CustomColors = config.CustomColors;
            FullScreenMode = config.FullScreenMode;
            NavbarVertical = config.NavbarVertical;
            LanguageManager.CurrentLanguage = config.Language;

            KeepSelection = config.KeepSelection;
            KeepNavbarPosition = config.KeepNavbarPosition;
            LastSelection = config.LastSelection;
            LastNavbarPosition = config.LastNavbarPosition;

            CaptureResolution = config.CaptureResolution ?? "";
        }

        /// <summary>Associa o atalho escolhido pelo usuário à janela oculta do Windows.</summary>
        public void RegisterGlobalHotkey()
        {
            HotkeyManager.UnregisterHotKey(messageWindow.Handle, 1);
            HotkeyManager.RegisterHotKey(messageWindow.Handle, 1, CurrentHotkeyModifier, CurrentHotkey);
        }

        private void OpenSettings(object? sender, EventArgs e)
        {
            SettingsForm settings = new SettingsForm(this);
            settings.Show();
        }

        /// <summary>Dispara a captura de tela e abre o overlay de edição (SelectionForm).</summary>
        public void TriggerScreenshot()
        {
            if (currentSelectionForm != null)
            {
                if (!currentSelectionForm.IsDisposed)
                {
                    currentSelectionForm.Close();
                    currentSelectionForm.Dispose();
                }
                currentSelectionForm = null;
            }

            Bitmap screenshot = ScreenCapture.CaptureAllScreens();
            currentSelectionForm = new SelectionForm(screenshot);

            currentSelectionForm.OnImageCopied += (bitmap) =>
            {
                _publisher?.Publish(bitmap);
            };

            currentSelectionForm.Show();
        }

        /// <summary>Exibe uma notificação elegante no canto inferior direito.</summary>
        public static void ShowToast(string message, Bitmap? thumbnail = null)
        {
            ToastForm toast = new ToastForm
            {
                Size = new Size(350, 80),
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.FromArgb(40, 40, 40),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                Opacity = 0.95
            };

            if (thumbnail != null)
            {
                PictureBox pb = new PictureBox
                {
                    Image = thumbnail,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(60, 60),
                    Location = new Point(10, 10)
                };
                toast.Controls.Add(pb);
            }

            Label lbl = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(80, 0),
                Size = new Size(240, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };
            toast.Controls.Add(lbl);

            Button btnClose = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Size = new Size(25, 25),
                Location = new Point(325, 5),
                Font = new Font("Arial", 8, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => toast.Close();
            toast.Controls.Add(btnClose);

            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            toast.Location = new Point(workingArea.Right - toast.Width - 20, workingArea.Bottom - toast.Height - 20);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 4000 };
            timer.Tick += (s, e) => { toast.Close(); timer.Stop(); timer.Dispose(); };

            toast.Show();
            timer.Start();
        }

        private void Exit(object? sender, EventArgs e)
        {
            if (_publisher != null || _isPluginMode)
            {
                this.Dispose();
            }
            else
            {
                Application.Exit();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (messageWindow != null)
                {
                    HotkeyManager.UnregisterHotKey(messageWindow.Handle, 1);
                    messageWindow.Dispose();
                }

                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }

                if (currentSelectionForm != null && !currentSelectionForm.IsDisposed)
                {
                    currentSelectionForm.Close();
                    currentSelectionForm.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private class HiddenMessageWindow : Form
        {
            private MainContext context;
            public HiddenMessageWindow(MainContext context) { this.context = context; this.ShowInTaskbar = false; this.WindowState = FormWindowState.Minimized; }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == HotkeyManager.WM_HOTKEY) context.TriggerScreenshot();
                base.WndProc(ref m);
            }
        }

        private class ToastForm : Form
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x80;
                    return cp;
                }
            }
        }
    }
}