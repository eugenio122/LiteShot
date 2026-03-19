using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteShot.Core;

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

        public static uint CurrentHotkeyModifier = HotkeyManager.MOD_NONE;
        public static uint CurrentHotkey = HotkeyManager.VK_PRINTSCREEN;

        public static string LastColor = "#FF0000";
        public static int[] CustomColors = new int[16];

        /// <summary>Construtor: Carrega configurações, cria o ícone da bandeja e registra o atalho global.</summary>
        public MainContext()
        {
            CarregarConfiguracoes();

            trayIcon = new NotifyIcon()
            {
                Icon = CreateAppIcon(),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
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
            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("SettingsTitle") + "...", null, OpenSettings);

            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("Sobre"), null, OpenAbout);

            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add(LanguageManager.GetString("Fechar"), null, Exit);
        }

        // Abrir a janela Sobre:
        private void OpenAbout(object? sender, EventArgs e)
        {
            Form about = new Form { Text = LanguageManager.GetString("Sobre"), Size = new Size(320, 180), StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            Label lbl = new Label { Text = "LiteShot v1.0.0\n\nDesenvolvido para máxima produtividade.", Dock = DockStyle.Top, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            LinkLabel lnk = new LinkLabel { Text = "Página no GitHub", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
            lnk.LinkClicked += (s, ev) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/eugenio122/LiteShot") { UseShellExecute = true });
            about.Controls.Add(lnk);
            about.Controls.Add(lbl);
            about.ShowDialog();
        }

        /// <summary>Desenha o ícone do Pincel/Stylus dinamicamente na memória (sem usar arquivo .ico externo).</summary>
        private Icon CreateAppIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pHandle = new Pen(Color.FromArgb(100, 100, 100), 4f))
                    {
                        pHandle.StartCap = LineCap.Round;
                        pHandle.EndCap = LineCap.Round;
                        g.DrawLine(pHandle, 8, 24, 20, 12);
                    }

                    GraphicsPath path = new GraphicsPath();
                    path.AddBezier(new Point(20, 12), new Point(22, 6), new Point(28, 4), new Point(24, 10));
                    path.CloseFigure();

                    using (Brush bTip = new SolidBrush(Color.Turquoise))
                    {
                        g.FillPath(bTip, path);
                    }

                    g.DrawPath(Pens.White, path);
                }
                return Icon.FromHandle(bmp.GetHicon());
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
            CustomColors = config.CustomColors;
            FullScreenMode = config.FullScreenMode;
            LanguageManager.CurrentLanguage = config.Language;
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
            if (currentSelectionForm != null && !currentSelectionForm.IsDisposed) return;
            Bitmap screenshot = ScreenCapture.CaptureAllScreens();
            currentSelectionForm = new SelectionForm(screenshot);
            currentSelectionForm.Show();
        }

        /// <summary>Exibe uma notificação elegante no canto inferior direito, opcionalmente com a miniatura da imagem.</summary>
        public static void ShowToast(string message, Bitmap? thumbnail = null)
        {
            Form toast = new Form
            {
                Size = new Size(350, 80),
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.FromArgb(40, 40, 40),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                Opacity = 0.95
            };

            // Miniatura
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

            // Mensagem
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

            // Botão Fechar
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

            // Posiciona no canto inferior direito
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            toast.Location = new Point(workingArea.Right - toast.Width - 20, workingArea.Bottom - toast.Height - 20);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 4000 };
            timer.Tick += (s, e) => { toast.Close(); timer.Stop(); timer.Dispose(); };

            toast.Show();
            timer.Start();
        }

        private void Exit(object? sender, EventArgs e)
        {
            HotkeyManager.UnregisterHotKey(messageWindow.Handle, 1);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }

        /// <summary>Classe aninhada: Uma janela invisível cuja única função é escutar a mensagem WM_HOTKEY do Windows.</summary>
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
    }
}