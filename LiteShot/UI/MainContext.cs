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
    /// direto na bandeja do sistema (System Tray) ou atue como um serviço invisível em modo Plugin.
    /// </summary>
    public class MainContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private HiddenMessageWindow messageWindow;
        private SelectionForm? currentSelectionForm;

        /// <summary>Estado global do tema atual (Claro/Escuro).</summary>
        public static bool IsDarkMode = true;

        /// <summary>Indica se a área de seleção deve iniciar maximizada (Ecrã Inteiro).</summary>
        public static bool FullScreenMode = false;

        /// <summary>Indica se deve exibir toasts (notificações) após ações como copiar ou salvar.</summary>
        public static bool ShowNotifications = true;

        /// <summary>Indica se o cursor do rato deve ser capturado junto com a imagem.</summary>
        public static bool CaptureCursor = false;

        /// <summary>Formato de exportação padrão (PNG, JPG, BMP).</summary>
        public static string ImageFormat = "PNG";

        /// <summary>Resolução limite aplicada ao gerar a imagem final. 'Auto' mantém o tamanho original.</summary>
        public static string CaptureResolution = "1920x1080";

        /// <summary>Modificador da tecla de atalho global (ex: CTRL, SHIFT, ALT).</summary>
        public static uint CurrentHotkeyModifier = HotkeyManager.MOD_NONE;

        /// <summary>Tecla virtual do atalho global (Padrão: PrintScreen).</summary>
        public static uint CurrentHotkey = HotkeyManager.VK_PRINTSCREEN;

        /// <summary>Cor hexadecimal ativa para as ferramentas de desenho em geral.</summary>
        public static string LastColor = "#FF0000";

        /// <summary>Cor hexadecimal ativa especificamente para a ferramenta Marcador.</summary>
        public static string LastHighlightColor = "#FFFF00";

        /// <summary>Matriz com as cores personalizadas guardadas na paleta do utilizador.</summary>
        public static int[] CustomColors = new int[16];

        /// <summary>Indica se a barra de ferramentas flutuante deve ser desenhada na vertical.</summary>
        public static bool NavbarVertical = false;

        /// <summary>Indica se o sistema deve memorizar a posição da última seleção feita.</summary>
        public static bool KeepSelection = false;

        /// <summary>Indica se o sistema deve memorizar a posição livre da barra de ferramentas.</summary>
        public static bool KeepNavbarPosition = false;

        /// <summary>Coordenadas do último recorte realizado.</summary>
        public static Rectangle LastSelection = Rectangle.Empty;

        /// <summary>Coordenadas da última posição da barra de ferramentas (Navbar).</summary>
        public static Point LastNavbarPosition = Point.Empty;

        // Dependências da nova arquitetura Fire-and-Forget
        private ILiteHostContext? _hostContext;
        private IEventBus? _eventBus;
        private bool _isPluginMode = false;

        /// <summary>Evento disparado internamente para notificar as janelas (UI) de que o tema mudou.</summary>
        public event Action<bool>? OnThemeUpdated;

        /// <summary>
        /// Define se o LiteShot está a rodar dentro do LiteTools (.dll) ou sozinho (.exe).
        /// Ao ativar o modo plugin, o ícone da bandeja é ocultado automaticamente.
        /// </summary>
        public bool IsPluginMode
        {
            get => _isPluginMode;
            set
            {
                _isPluginMode = value;
                if (trayIcon != null)
                {
                    trayIcon.Visible = !_isPluginMode;
                }
            }
        }

        /// <summary>
        /// Construtor utilizado quando o LiteShot é executado de forma autônoma (.exe).
        /// Inicia sem injetar dependências do LiteTools.
        /// </summary>
        public MainContext()
        {
            InitCore(false, null, null, null);
        }

        /// <summary>
        /// Construtor utilizado quando o LiteShot é carregado como Plugin (.dll).
        /// Recebe as interfaces do ecossistema LiteTools para comunicação Fire-and-Forget.
        /// </summary>
        /// <param name="hostContext">Contexto global da Nave-Mãe.</param>
        /// <param name="eventBus">Barramento de eventos do ecossistema.</param>
        /// <param name="hostLanguage">O idioma global forçado pelo Host.</param>
        public MainContext(ILiteHostContext hostContext, IEventBus eventBus, string hostLanguage)
        {
            InitCore(true, hostContext, eventBus, hostLanguage);
        }

        /// <summary>
        /// Rotina de inicialização centralizada. Carrega configurações, aplica regras de 
        /// resolução de acordo com a arquitetura e prepara o ícone e os atalhos.
        /// </summary>
        private void InitCore(bool isPlugin, ILiteHostContext? hostContext, IEventBus? eventBus, string? hostLanguage)
        {
            _isPluginMode = isPlugin;
            _hostContext = hostContext;
            _eventBus = eventBus;

            CarregarConfiguracoes();

            // Sincronização Global de Idioma (Nave-Mãe dita a regra)
            if (_isPluginMode && !string.IsNullOrEmpty(hostLanguage))
            {
                LanguageManager.CurrentLanguage = hostLanguage;
            }

            // Aplica as Regras da Resolução: 
            // .exe sempre tira print em tamanho real (Auto).
            // .dll protege a memória limitando resoluções gigantes a Full HD por padrão.
            if (!_isPluginMode)
            {
                CaptureResolution = "Auto";

                // Verificação do Tema Global: O modo escuro é o padrão, mas tentamos detectar o tema do Windows para alinhar a experiência visual.
                IsDarkMode = CheckWindowsDarkMode();
            }
            else
            {
                if (string.IsNullOrEmpty(CaptureResolution))
                {
                    int nativeWidth = Screen.PrimaryScreen.Bounds.Width;
                    CaptureResolution = nativeWidth > 1920 ? "1920x1080" : "Auto";
                }
            }

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

            // Assinatura do Tema Global: Se a Nave-Mãe avisar que o tema mudou, nós aplicamos!
            if (_eventBus != null)
            {
                _eventBus.Subscribe<ThemeChangedEvent>(e =>
                {
                    IsDarkMode = e.IsDarkMode;
                    OnThemeUpdated?.Invoke(IsDarkMode);
                });
            }

            messageWindow = new HiddenMessageWindow(this);
            RegisterGlobalHotkey();
        }

        /// <summary>
        /// Reconstrói o menu de contexto (botão direito no ícone da bandeja)
        /// puxando os textos atualizados do LanguageManager.
        /// </summary>
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

        /// <summary>
        /// Exibe a janela de informações "Sobre o LiteShot", aplicando os textos localizados 
        /// e ajustando as cores de acordo com o tema atual (Claro/Escuro).
        /// </summary>
        private void OpenAbout(object? sender, EventArgs e)
        {
            Form about = new Form { Text = LanguageManager.GetString("Sobre"), Size = new Size(350, 320), StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            Label lblTitle = new Label { Text = "LiteShot v2.0.0", Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            Label lblShortcuts = new Label { Text = LanguageManager.GetString("AboutShortcuts"), Dock = DockStyle.Top, Height = 160, Padding = new Padding(20, 10, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
            LinkLabel lnk = new LinkLabel { Text = LanguageManager.GetString("AboutGitHub"), Dock = DockStyle.Bottom, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
            lnk.LinkClicked += (s, ev) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/eugenio122/LiteShot") { UseShellExecute = true });

            about.Controls.Add(lblShortcuts);
            about.Controls.Add(lblTitle);
            about.Controls.Add(lnk);

            // Aplica o tema localmente no Form Sobre
            about.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            about.ForeColor = IsDarkMode ? Color.White : Color.Black;

            about.ShowDialog();
        }

        /// <summary>
        /// Carrega o ícone da própria aplicação e aplica um redimensionamento interpolado (HighQualityBicubic)
        /// para remover bordas transparentes excessivas e deixá-lo mais bonito na bandeja do Windows.
        /// </summary>
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

        /// <summary>
        /// Sincroniza as variáveis estáticas locais com os dados gravados no ficheiro JSON de configuração.
        /// </summary>
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

        /// <summary>
        /// Regista a combinação de teclas escolhida pelo utilizador no sistema operativo,
        /// associando-a a uma janela oculta que fica à escuta do pressionar da tecla.
        /// </summary>
        public void RegisterGlobalHotkey()
        {
            HotkeyManager.UnregisterHotKey(messageWindow.Handle, 1);
            HotkeyManager.RegisterHotKey(messageWindow.Handle, 1, CurrentHotkeyModifier, CurrentHotkey);
        }

        /// <summary>
        /// Instancia e abre o painel de opções do LiteShot (Apenas útil em modo .exe autônomo).
        /// </summary>
        private void OpenSettings(object? sender, EventArgs e)
        {
            SettingsForm settings = new SettingsForm(this);
            settings.Show();
        }

        /// <summary>
        /// Ativa a "Lente da Câmera". Captura o ecrã instantaneamente e sobrepõe a interface
        /// escura de recorte, injetando o barramento de eventos para processamento Fire-and-Forget.
        /// </summary>
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

            // A captura agora obedece à união de todos os monitores via DPI-Awareness V2
            Bitmap screenshot = ScreenCapture.CaptureAllScreens();

            // Passamos o EventBus diretamente para a UI tratar a assincronicidade
            currentSelectionForm = new SelectionForm(screenshot, _eventBus);
            currentSelectionForm.Show();
        }

        /// <summary>
        /// Cria e exibe uma notificação elegante e flutuante no canto inferior direito do ecrã,
        /// que não rouba o foco do utilizador e desaparece sozinha após alguns segundos.
        /// </summary>
        /// <param name="message">A mensagem a ser exibida na notificação.</param>
        /// <param name="thumbnail">Uma miniatura opcional da imagem capturada para ilustrar o toast.</param>
        public static void ShowToast(string message, Bitmap? thumbnail = null)
        {
            ToastForm toast = new ToastForm
            {
                Size = new Size(350, 80),
                FormBorderStyle = FormBorderStyle.None,
                BackColor = IsDarkMode ? Color.FromArgb(40, 40, 40) : Color.White,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                ShowInTaskbar = false,
                Opacity = 0.95
            };

            if (thumbnail != null)
            {
                PictureBox pb = new PictureBox { Image = thumbnail, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(60, 60), Location = new Point(10, 10) };
                toast.Controls.Add(pb);
            }

            Label lbl = new Label { Text = message, ForeColor = IsDarkMode ? Color.White : Color.Black, Font = new Font("Segoe UI", 9, FontStyle.Regular), Location = new Point(80, 0), Size = new Size(240, 80), TextAlign = ContentAlignment.MiddleLeft };
            toast.Controls.Add(lbl);

            Button btnClose = new Button { Text = "✕", FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, BackColor = Color.Transparent, Size = new Size(25, 25), Location = new Point(325, 5), Font = new Font("Arial", 8, FontStyle.Bold) };
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

        /// <summary>
        /// Encerra a execução do LiteShot. Em modo plugin, apenas destrói os recursos em memória;
        /// em modo autônomo, derruba todo o processo da aplicação.
        /// </summary>
        private void Exit(object? sender, EventArgs e)
        {
            if (_eventBus != null || _isPluginMode)
            {
                this.Dispose();
            }
            else
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Limpa os recursos não geridos da memória (Hooks de Teclado, Ícones e Forms Abertos)
        /// para evitar vazamentos (memory leaks) ao encerrar ou recarregar o módulo.
        /// </summary>
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

        /// <summary>
        /// Um formulário "fantasma" que não aparece no ecrã. O seu único objetivo
        /// é processar mensagens de nível baixo do sistema operativo (como teclas globais pressionadas).
        /// </summary>
        private class HiddenMessageWindow : Form
        {
            private MainContext context;
            public HiddenMessageWindow(MainContext context)
            {
                this.context = context;
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == HotkeyManager.WM_HOTKEY)
                    context.TriggerScreenshot();
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Verifica se o Windows está em modo escuro.
        /// </summary>
        private bool CheckWindowsDarkMode()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null && key.GetValue("AppsUseLightTheme") != null)
                    {
                        int val = (int)key.GetValue("AppsUseLightTheme");
                        return val == 0; // 0 significa Dark Mode, 1 significa Light Mode
                    }
                }
            }
            catch { }
            return true; // Se der erro ou for Windows antigo, assume Escuro por padrão
        }

        /// <summary>
        /// Um formulário base para as notificações. Modifica os parâmetros de criação 
        /// para forçar o Windows a desenhar o painel sem roubar o foco ativo do utilizador (ExStyle 0x80).
        /// </summary>
        private class ToastForm : Form
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                    return cp;
                }
            }
        }
    }
}