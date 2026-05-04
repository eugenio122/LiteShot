using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteShot.Core;
using LiteTools.Interfaces;

namespace LiteShot.UI
{
    /// <summary>
    /// A "Lente da Câmera". Este é o formulário transparente que se sobrepõe a todos os ecrãs 
    /// para permitir o recorte e a marcação. É construído para ser descartável, rápido 
    /// e agir de forma assíncrona (Fire-and-Forget) para não travar o utilizador.
    /// </summary>
    public partial class SelectionForm : Form
    {
        private Bitmap originalScreenshot;
        private Bitmap drawingLayer;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelectionFinished = false;

        /// <summary>Espessura global atual para todas as ferramentas de desenho (Caneta, Setas, Formas).</summary>
        public static int PenWidth = 3;

        private Stack<Bitmap> historicoDesenho = new Stack<Bitmap>();
        private Stack<Bitmap> historicoRefazer = new Stack<Bitmap>();

        private Dictionary<string, Rectangle> toolbarButtons = new Dictionary<string, Rectangle>();
        private Rectangle fullNavbarRect;
        private Point navbarCustomPosition = Point.Empty;
        private bool isNavbarMoved = false;

        private ToolTip toolTip = new ToolTip();
        private string lastButtonHovered = "";

        /// <summary>Máquina de estados que define a ação atual do utilizador com o rato.</summary>
        private enum DragAction { None, Create, Move, ResizeTL, ResizeT, ResizeTR, ResizeR, ResizeBR, ResizeB, ResizeBL, ResizeL, MoveNavbar }
        private DragAction currentAction = DragAction.None;

        private Point dragStartPoint;
        private Rectangle originalSelectionRect;
        private const int HandleSize = 8;

        private string ferramentaAtual = "";
        private Color currentColor;
        private Color highlighterColor;
        private bool isDrawingToolActive = false;
        private Point drawStartPoint;
        private Point drawCurrentPoint;
        private TextBox txtInput;

        private string pressedButton = "";
        private bool isDraggingNavbar = false;

        // Referência à nova arquitetura Fire-and-Forget
        private IEventBus? _eventBus;

        public SelectionForm() { }

        /// <summary>
        /// Construtor principal da lente de captura.
        /// </summary>
        /// <param name="screenshot">A imagem gigante cobrindo todos os monitores (DPI-Aware).</param>
        /// <param name="eventBus">O barramento de comunicação do ecossistema LiteTools.</param>
        public SelectionForm(Bitmap screenshot, IEventBus? eventBus = null)
        {
            _eventBus = eventBus;
            this.originalScreenshot = screenshot;
            this.drawingLayer = new Bitmap(screenshot.Width, screenshot.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(this.drawingLayer))
            {
                g.Clear(Color.Transparent);
            }

            try { this.currentColor = ColorTranslator.FromHtml(MainContext.LastColor); } catch { this.currentColor = Color.Red; }
            try { this.highlighterColor = ColorTranslator.FromHtml(MainContext.LastHighlightColor); } catch { this.highlighterColor = Color.Yellow; }

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;

            // DPI-AWARENESS V2: Bounds físicos calculados via união de todas as telas.
            this.Bounds = ScreenCapture.GetPhysicalBounds();

            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;
            this.ShowInTaskbar = false;

            this.Activate();
            this.Focus();

            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;

            // Tema Global aplicado ao TextBox flutuante
            txtInput = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.None,
                BackColor = MainContext.IsDarkMode ? Color.FromArgb(40, 40, 40) : Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = this.currentColor
            };
            txtInput.KeyDown += TxtInput_KeyDown;
            txtInput.LostFocus += TxtInput_LostFocus;
            this.Controls.Add(txtInput);

            this.KeyPreview = true;

            this.Load += (s, e) => HotkeyManager.RegisterOverlayHotkeys(this.Handle);

            // Usamos isHandleCreated também no FormClosed para extrema segurança
            this.FormClosed += (s, e) => {
                if (this.IsHandleCreated) HotkeyManager.UnregisterOverlayHotkeys(this.Handle);
            };

            if (MainContext.FullScreenMode)
            {
                SelecionarMonitorAtual();
            }
            else if (MainContext.KeepSelection && !MainContext.LastSelection.IsEmpty)
            {
                if (this.Bounds.IntersectsWith(MainContext.LastSelection))
                {
                    selectionRect = MainContext.LastSelection;
                    isSelectionFinished = true;
                    CalcularPosicaoNavbar();
                }
            }
        }

        /// <summary>
        /// Interceta as mensagens de nível baixo do Windows para escutar as hotkeys 
        /// de edição (Ctrl+C, Ctrl+Z, Esc) exclusivas do overlay (Passe VIP), contornando perda de foco.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == HotkeyManager.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                switch (id)
                {
                    case HotkeyManager.HOTKEY_ID_CTRL_A: SelecionarMonitorAtual(); break;
                    case HotkeyManager.HOTKEY_ID_CTRL_Z: AcaoDesfazer(); break;
                    case HotkeyManager.HOTKEY_ID_CTRL_Y: AcaoRefazer(); break;
                    case HotkeyManager.HOTKEY_ID_CTRL_C: if (isSelectionFinished) ExecutarAcaoToolbar("Copiar"); break;
                    case HotkeyManager.HOTKEY_ID_CTRL_S: if (isSelectionFinished) ExecutarAcaoToolbar("Salvar"); break;
                    case HotkeyManager.HOTKEY_ID_ESC:
                        if (txtInput.Visible) { txtInput.Visible = false; txtInput.Text = ""; ferramentaAtual = ""; this.Focus(); this.Invalidate(); }
                        else if (!string.IsNullOrEmpty(ferramentaAtual)) { ferramentaAtual = ""; this.Invalidate(); }
                        else { CancelCapture(); }
                        break;
                }
            }
            base.WndProc(ref m);
        }

        /// <summary>Limpa o histórico de Refazer sempre que uma nova ação de desenho ocorre.</summary>
        private void LimparRefazer() { while (historicoRefazer.Count > 0) historicoRefazer.Pop().Dispose(); }

        /// <summary>Abre o diálogo nativo do Windows para o utilizador escolher uma cor personalizada para o desenho.</summary>
        private void AbrirSeletorDeCor()
        {
            bool isHighlighter = (ferramentaAtual == "Marcador");
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = isHighlighter ? this.highlighterColor : this.currentColor;
                cd.CustomColors = MainContext.CustomColors;
                cd.FullOpen = true;

                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    if (isHighlighter) { this.highlighterColor = cd.Color; MainContext.LastHighlightColor = ColorTranslator.ToHtml(cd.Color); }
                    else { this.currentColor = cd.Color; this.txtInput.ForeColor = cd.Color; MainContext.LastColor = ColorTranslator.ToHtml(cd.Color); }

                    MainContext.CustomColors = cd.CustomColors;
                    AppSettings config = SettingsManager.Load();
                    config.LastColor = MainContext.LastColor;
                    config.LastHighlightColor = MainContext.LastHighlightColor;
                    config.CustomColors = MainContext.CustomColors;
                    SettingsManager.Save(config);

                    this.Invalidate();
                }
            }
        }

        /// <summary>Carimba o texto digitado na textbox flutuante de forma permanente na camada Bitmap de desenho.</summary>
        private void FinalizarTexto()
        {
            if (txtInput.Visible && !string.IsNullOrWhiteSpace(txtInput.Text))
            {
                historicoDesenho.Push((Bitmap)drawingLayer.Clone());
                using (Graphics g = Graphics.FromImage(drawingLayer))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using (Brush b = new SolidBrush(currentColor)) g.DrawString(txtInput.Text, txtInput.Font, b, txtInput.Location);
                }
            }
            txtInput.Visible = false; txtInput.Text = ""; this.Invalidate();
        }

        private void TxtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FinalizarTexto(); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; txtInput.Visible = false; txtInput.Text = ""; ferramentaAtual = ""; this.Focus(); this.Invalidate(); }
        }

        private void TxtInput_LostFocus(object? sender, EventArgs e) => FinalizarTexto();

        /// <summary>Calcula e seleciona automaticamente o monitor físico onde o rato se encontra atualmente.</summary>
        private void SelecionarMonitorAtual()
        {
            FinalizarTexto();
            Screen currentScreen = Screen.FromPoint(Cursor.Position);
            Rectangle screenBounds = currentScreen.Bounds;
            int x = screenBounds.X - this.Left; int y = screenBounds.Y - this.Top;
            selectionRect = new Rectangle(x, y, screenBounds.Width, screenBounds.Height);
            currentAction = DragAction.None; isDrawingToolActive = false; ferramentaAtual = ""; isNavbarMoved = false; isSelectionFinished = true;
            CalcularPosicaoNavbar(); SalvarPosicoes(); this.Invalidate();
        }

        /// <summary>
        /// Gera os 8 pequenos retângulos (handles) nos cantos e arestas da seleção para permitir redimensionamento.
        /// Possui algoritmo anti-corte para que os handles nunca nasçam fora do ecrã físico.
        /// </summary>
        private Rectangle[] GetHandles()
        {
            int hs2 = HandleSize / 2;
            Rectangle[] handles = new Rectangle[] {
                new Rectangle(selectionRect.Left - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left + selectionRect.Width/2 - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Top + selectionRect.Height/2 - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left + selectionRect.Width/2 - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left - hs2, selectionRect.Top + selectionRect.Height/2 - hs2, HandleSize, HandleSize)
            };

            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].Left < 0) handles[i].X = 0; if (handles[i].Top < 0) handles[i].Y = 0;
                if (handles[i].Right > this.Width) handles[i].X = this.Width - HandleSize;
                if (handles[i].Bottom > this.Height) handles[i].Y = this.Height - HandleSize;
            }
            return handles;
        }

        /// <summary>Identifica o que o utilizador pretende fazer (arrastar, redimensionar, etc.) com base na posição do cursor.</summary>
        private DragAction GetDragAction(Point p)
        {
            if (!isSelectionFinished) return DragAction.None;
            var h = GetHandles();
            if (h[0].Contains(p)) return DragAction.ResizeTL; if (h[1].Contains(p)) return DragAction.ResizeT;
            if (h[2].Contains(p)) return DragAction.ResizeTR; if (h[3].Contains(p)) return DragAction.ResizeR;
            if (h[4].Contains(p)) return DragAction.ResizeBR; if (h[5].Contains(p)) return DragAction.ResizeB;
            if (h[6].Contains(p)) return DragAction.ResizeBL; if (h[7].Contains(p)) return DragAction.ResizeL;
            if (selectionRect.Contains(p) && string.IsNullOrEmpty(ferramentaAtual)) return DragAction.Move;
            return DragAction.None;
        }

        /// <summary>Lida com o clique inicial do rato (criar nova seleção, interagir com a UI ou ativar ferramentas).</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { if (!string.IsNullOrEmpty(ferramentaAtual)) { ferramentaAtual = ""; if (txtInput.Visible) { txtInput.Visible = false; txtInput.Text = ""; } this.Invalidate(); } return; }
            if (e.Button != MouseButtons.Left) return;
            if (txtInput.Visible && !txtInput.Bounds.Contains(e.Location)) FinalizarTexto();

            if (isSelectionFinished)
            {
                if (fullNavbarRect.Contains(e.Location))
                {
                    currentAction = DragAction.MoveNavbar; dragStartPoint = e.Location; isDraggingNavbar = false; pressedButton = "";
                    foreach (var btn in toolbarButtons) { if (btn.Value.Contains(e.Location) && btn.Key != "Separator") { pressedButton = btn.Key; break; } }
                    return;
                }

                if (!string.IsNullOrEmpty(ferramentaAtual))
                {
                    if (ferramentaAtual == "Texto") { txtInput.Location = e.Location; txtInput.Size = new Size(150, 25); txtInput.Visible = true; txtInput.Focus(); }
                    else { LimparRefazer(); historicoDesenho.Push((Bitmap)drawingLayer.Clone()); isDrawingToolActive = true; drawStartPoint = e.Location; drawCurrentPoint = e.Location; }
                    return;
                }

                currentAction = GetDragAction(e.Location);
                if (currentAction != DragAction.None) { dragStartPoint = e.Location; originalSelectionRect = selectionRect; return; }

                isSelectionFinished = false; isNavbarMoved = false; toolbarButtons.Clear(); ferramentaAtual = ""; this.Invalidate();
            }

            currentAction = DragAction.Create; isNavbarMoved = false; startPoint = e.Location; selectionRect = new Rectangle(e.X, e.Y, 0, 0);
        }

        /// <summary>Lida com o movimento contínuo (arrastar para selecionar, arrastar janelas ou desenhar os traços e vetores).</summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDrawingToolActive)
            {
                drawCurrentPoint = e.Location;
                if (ferramentaAtual == "Caneta" || ferramentaAtual == "Marcador")
                {
                    using (Graphics g = Graphics.FromImage(drawingLayer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        if (ferramentaAtual == "Caneta") using (Pen p = new Pen(currentColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        else { Color semiTrans = Color.FromArgb(80, highlighterColor); using (Pen p = new Pen(semiTrans, PenWidth * 4) { StartCap = LineCap.Square, EndCap = LineCap.Square }) g.DrawLine(p, drawStartPoint, drawCurrentPoint); }
                    }
                    drawStartPoint = drawCurrentPoint;
                }
                this.Invalidate();
            }
            else if (currentAction == DragAction.MoveNavbar)
            {
                int dx = e.X - dragStartPoint.X; int dy = e.Y - dragStartPoint.Y;
                if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3) isDraggingNavbar = true;
                if (isDraggingNavbar) { navbarCustomPosition.X += dx; navbarCustomPosition.Y += dy; dragStartPoint = e.Location; isNavbarMoved = true; CalcularPosicaoNavbar(); this.Invalidate(); }
            }
            else if (currentAction == DragAction.Create)
            {
                int x = Math.Min(startPoint.X, e.X); int y = Math.Min(startPoint.Y, e.Y);
                selectionRect = new Rectangle(x, y, Math.Abs(startPoint.X - e.X), Math.Abs(startPoint.Y - e.Y)); this.Invalidate();
            }
            else if (currentAction == DragAction.Move)
            {
                int dx = e.X - dragStartPoint.X; int dy = e.Y - dragStartPoint.Y;
                selectionRect = new Rectangle(originalSelectionRect.X + dx, originalSelectionRect.Y + dy, originalSelectionRect.Width, originalSelectionRect.Height); CalcularPosicaoNavbar(); this.Invalidate();
            }
            else if (currentAction != DragAction.None)
            {
                int dx = e.X - dragStartPoint.X; int dy = e.Y - dragStartPoint.Y;
                int l = originalSelectionRect.Left; int r = originalSelectionRect.Right; int t = originalSelectionRect.Top; int b = originalSelectionRect.Bottom;
                if (currentAction == DragAction.ResizeTL) { l += dx; t += dy; }
                if (currentAction == DragAction.ResizeT) { t += dy; }
                if (currentAction == DragAction.ResizeTR) { r += dx; t += dy; }
                if (currentAction == DragAction.ResizeR) { r += dx; }
                if (currentAction == DragAction.ResizeBR) { r += dx; b += dy; }
                if (currentAction == DragAction.ResizeB) { b += dy; }
                if (currentAction == DragAction.ResizeBL) { l += dx; b += dy; }
                if (currentAction == DragAction.ResizeL) { l += dx; }
                selectionRect = new Rectangle(Math.Min(l, r), Math.Min(t, b), Math.Abs(r - l), Math.Abs(b - t)); CalcularPosicaoNavbar(); this.Invalidate();
            }
            else if (isSelectionFinished)
            {
                string hoveredBtn = "";
                foreach (var btn in toolbarButtons) { if (btn.Value.Contains(e.Location)) { hoveredBtn = btn.Key; break; } }
                if (hoveredBtn != lastButtonHovered) { lastButtonHovered = hoveredBtn; if (!string.IsNullOrEmpty(hoveredBtn)) toolTip.Show(LanguageManager.GetString(hoveredBtn), this, e.X, e.Y + 25, 3000); else toolTip.Hide(this); }

                if (!string.IsNullOrEmpty(hoveredBtn)) this.Cursor = Cursors.Hand;
                else if (fullNavbarRect.Contains(e.Location)) this.Cursor = Cursors.SizeAll;
                else if (!string.IsNullOrEmpty(ferramentaAtual)) { this.Cursor = Cursors.Cross; this.Invalidate(); }
                else
                {
                    var action = GetDragAction(e.Location);
                    if (action == DragAction.Move) this.Cursor = Cursors.SizeAll;
                    else if (action == DragAction.ResizeTL || action == DragAction.ResizeBR) this.Cursor = Cursors.SizeNWSE;
                    else if (action == DragAction.ResizeTR || action == DragAction.ResizeBL) this.Cursor = Cursors.SizeNESW;
                    else if (action == DragAction.ResizeT || action == DragAction.ResizeB) this.Cursor = Cursors.SizeNS;
                    else if (action == DragAction.ResizeL || action == DragAction.ResizeR) this.Cursor = Cursors.SizeWE;
                    else this.Cursor = Cursors.Default;
                }
            }
        }

        /// <summary>Finaliza as lógicas de arrasto, atualiza o desenho geométrico contínuo e consolida as posições.</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (currentAction == DragAction.MoveNavbar)
            {
                if (!isDraggingNavbar && !string.IsNullOrEmpty(pressedButton)) ExecutarAcaoToolbar(pressedButton);
                else if (isDraggingNavbar) SalvarPosicoes();
                currentAction = DragAction.None; this.Invalidate(); return;
            }

            if (isDrawingToolActive)
            {
                isDrawingToolActive = false;
                if (ferramentaAtual == "Linha" || ferramentaAtual == "Seta" || ferramentaAtual == "Forma")
                {
                    using (Graphics g = Graphics.FromImage(drawingLayer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (Pen p = new Pen(currentColor, PenWidth))
                        {
                            if (ferramentaAtual == "Linha") g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                            else if (ferramentaAtual == "Seta") { p.CustomEndCap = new AdjustableArrowCap(PenWidth + 2, PenWidth + 2); g.DrawLine(p, drawStartPoint, drawCurrentPoint); }
                            else if (ferramentaAtual == "Forma") { int x = Math.Min(drawStartPoint.X, drawCurrentPoint.X); int y = Math.Min(drawStartPoint.Y, drawCurrentPoint.Y); g.DrawRectangle(p, x, y, Math.Abs(drawStartPoint.X - drawCurrentPoint.X), Math.Abs(drawStartPoint.Y - drawCurrentPoint.Y)); }
                        }
                    }
                }
                this.Invalidate();
            }
            else if (currentAction != DragAction.None)
            {
                currentAction = DragAction.None;
                if (selectionRect.Width > 20 && selectionRect.Height > 20) { isSelectionFinished = true; CalcularPosicaoNavbar(); SalvarPosicoes(); }
                else { isSelectionFinished = false; toolbarButtons.Clear(); }
                this.Invalidate();
            }
        }

        /// <summary>Guarda as posições de ecrã (seleção e navbar) no JSON caso as flags persistentes estejam ativas no Settings.</summary>
        private void SalvarPosicoes()
        {
            bool mudouAlgo = false; AppSettings config = SettingsManager.Load();
            if (MainContext.KeepSelection && isSelectionFinished) { MainContext.LastSelection = selectionRect; config.LastSelection = MainContext.LastSelection; mudouAlgo = true; }
            if (MainContext.KeepNavbarPosition && isNavbarMoved) { MainContext.LastNavbarPosition = navbarCustomPosition; config.LastNavbarPosition = MainContext.LastNavbarPosition; mudouAlgo = true; }
            if (mudouAlgo) SettingsManager.Save(config);
        }

        /// <summary>
        /// Calcula a posição geométrica da barra de ferramentas (Navbar) no ecrã.
        /// Avalia a orientação, dimensão da seleção e garante por matemática absoluta que a barra nunca 
        /// será desenhada fora dos limites do ecrã físico atual.
        /// </summary>
        private void CalcularPosicaoNavbar()
        {
            toolbarButtons.Clear();
            string[] ferramentas = { "Caneta", "Linha", "Seta", "Forma", "Marcador", "Texto", "Cor", "Separator", "Desfazer", "Refazer", "TelaCheia", "Salvar", "Copiar", "Fechar" };
            int bW = 34, bH = 34, margin = 2, paddingBorder = 4, sepSize = 8, totalW = paddingBorder * 2 - margin, totalH = paddingBorder * 2 - margin;

            foreach (string btn in ferramentas)
            {
                if (MainContext.NavbarVertical) { totalH += (btn == "Separator" ? sepSize : bH) + margin; totalW = bW + paddingBorder * 2; }
                else { totalW += (btn == "Separator" ? sepSize : bW) + margin; totalH = bH + paddingBorder * 2; }
            }

            if (!isNavbarMoved)
            {
                if (MainContext.KeepNavbarPosition && !MainContext.LastNavbarPosition.IsEmpty)
                {
                    navbarCustomPosition = MainContext.LastNavbarPosition; isNavbarMoved = true;
                    if (navbarCustomPosition.X + totalW > this.Width) navbarCustomPosition.X = this.Width - totalW - paddingBorder;
                    if (navbarCustomPosition.Y + totalH > this.Height) navbarCustomPosition.Y = this.Height - totalH - paddingBorder;
                    if (navbarCustomPosition.X < 0) navbarCustomPosition.X = paddingBorder; if (navbarCustomPosition.Y < 0) navbarCustomPosition.Y = paddingBorder;
                }
                else
                {
                    int currentX = selectionRect.Right - totalW - paddingBorder; int toolbarY = selectionRect.Bottom - totalH - paddingBorder;
                    if (currentX < selectionRect.Left) currentX = selectionRect.Left + paddingBorder;
                    if (toolbarY < selectionRect.Top) toolbarY = selectionRect.Bottom + paddingBorder;
                    if (currentX < 0) currentX = paddingBorder; if (currentX + totalW > this.Width) currentX = this.Width - totalW - paddingBorder;
                    if (toolbarY + totalH > this.Height) toolbarY = this.Height - totalH - paddingBorder;
                    navbarCustomPosition = new Point(currentX, toolbarY);
                }
            }

            fullNavbarRect = new Rectangle(navbarCustomPosition.X, navbarCustomPosition.Y, totalW, totalH);
            int btnX = navbarCustomPosition.X + paddingBorder; int btnY = navbarCustomPosition.Y + paddingBorder;

            foreach (string btn in ferramentas)
            {
                int w = btn == "Separator" ? sepSize : bW; int h = btn == "Separator" ? sepSize : bH;
                if (MainContext.NavbarVertical) { toolbarButtons.Add(btn, new Rectangle(btnX, btnY, bW, h)); btnY += h + margin; }
                else { toolbarButtons.Add(btn, new Rectangle(btnX, btnY, w, bH)); btnX += w + margin; }
            }
        }

        /// <summary>Regressa um passo atrás no histórico de desenhos manipulando a pilha (Stack).</summary>
        private void AcaoDesfazer() { if (historicoDesenho.Count > 0) { historicoRefazer.Push((Bitmap)drawingLayer.Clone()); Bitmap oldLayer = drawingLayer; drawingLayer = historicoDesenho.Pop(); oldLayer.Dispose(); this.Invalidate(); } }

        /// <summary>Avança um passo que foi previamente desfeito na pilha de histórico (Stack).</summary>
        private void AcaoRefazer() { if (historicoRefazer.Count > 0) { historicoDesenho.Push((Bitmap)drawingLayer.Clone()); Bitmap oldLayer = drawingLayer; drawingLayer = historicoRefazer.Pop(); oldLayer.Dispose(); this.Invalidate(); } }

        /// <summary>
        /// MODO FIRE-AND-FORGET: Oculta a janela instantaneamente e emite um evento no barramento 
        /// avisando o ecossistema (como LiteFlow) que a ação foi cancelada.
        /// </summary>
        private void CancelCapture()
        {
            // Failsafe: Liberta o teclado global imediatamente antes de desaparecer
            if (this.IsHandleCreated) HotkeyManager.UnregisterOverlayHotkeys(this.Handle);

            this.Hide();
            if (_eventBus != null)
            {
                Task.Run(() => _eventBus.Publish(new LiteTools.Interfaces.ImageCaptureCanceledEvent()));
            }
            this.Close();
        }

        /// <summary>Gerencia os cliques e executa os botões da barra de ferramentas flutuante.</summary>
        private void ExecutarAcaoToolbar(string acao)
        {
            FinalizarTexto();
            switch (acao)
            {
                case "Caneta": case "Linha": case "Seta": case "Forma": case "Marcador": case "Texto": ferramentaAtual = (ferramentaAtual == acao ? "" : acao); this.Invalidate(); break;
                case "Cor": AbrirSeletorDeCor(); break;
                case "Desfazer": AcaoDesfazer(); break;
                case "Refazer": AcaoRefazer(); break;
                case "TelaCheia":
                    MainContext.FullScreenMode = !MainContext.FullScreenMode; AppSettings config = SettingsManager.Load(); config.FullScreenMode = MainContext.FullScreenMode; SettingsManager.Save(config);
                    if (MainContext.FullScreenMode) SelecionarMonitorAtual(); this.Invalidate(); break;
                case "Copiar": ProcessAndPublishImage(); break;
                case "Salvar": ProcessAndSaveImage(); break;
                case "Fechar": CancelCapture(); break;
            }
        }

        /// <summary>
        /// MODO FIRE-AND-FORGET: Processamento Assíncrono com 0ms Delay (Apenas Lente).
        /// Oculta o form de imediato, delega o peso do downscale e composição a uma Task paralela.
        /// Envia para a Área de Transferência e publica o Bitmap pronto no EventBus do Host.
        /// </summary>
        private void ProcessAndPublishImage()
        {
            // Failsafe: Liberta o teclado global imediatamente antes de enviar para background
            if (this.IsHandleCreated) HotkeyManager.UnregisterOverlayHotkeys(this.Handle);
            this.Hide();

            Rectangle rectProcess = selectionRect;
            string resProcess = MainContext.CaptureResolution;
            bool showToast = MainContext.ShowNotifications;

            Task.Run(() =>
            {
                Bitmap cropped = GetCroppedImageProcess(rectProcess, resProcess);

                this.Invoke((MethodInvoker)delegate {
                    try { Clipboard.SetImage(cropped); } catch { /* Evita crash se o Windows bloquear o clipboard temporariamente */ }
                    if (showToast) MainContext.ShowToast(LanguageManager.GetString("ToastCopied"), cropped);
                });

                if (_eventBus != null)
                {
                    _eventBus.Publish(new LiteTools.Interfaces.ImageCapturedEvent(cropped));
                }

                this.Invoke((MethodInvoker)delegate { this.Close(); });
            });
        }

        /// <summary>
        /// Similar ao ProcessAndPublishImage, mas focado na ação de abrir um SaveFileDialog
        /// e gravar a imagem assincronamente no disco local do utilizador.
        /// </summary>
        private void ProcessAndSaveImage()
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Title = "LiteShot", Filter = "PNG|*.png|JPG|*.jpg|BMP|*.bmp", FileName = "LiteShot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    FinalizarTexto();

                    // Failsafe: Liberta o teclado global imediatamente antes de gravar
                    if (this.IsHandleCreated) HotkeyManager.UnregisterOverlayHotkeys(this.Handle);
                    this.Hide();

                    Rectangle rectProcess = selectionRect;
                    string resProcess = MainContext.CaptureResolution;
                    bool showToast = MainContext.ShowNotifications;
                    string fileName = sfd.FileName;

                    Task.Run(() =>
                    {
                        Bitmap cropped = GetCroppedImageProcess(rectProcess, resProcess);
                        cropped.Save(fileName);

                        this.Invoke((MethodInvoker)delegate {
                            if (showToast) MainContext.ShowToast(LanguageManager.GetString("ToastSaved"), cropped);
                            this.Close();
                        });
                    });
                }
            }
        }

        /// <summary>
        /// O "Coração Matemático". Combina a camada de desenho transparente sobre a captura de ecrã original.
        /// Recorta a área selecionada pelo utilizador e aplica downscale geométrico (HighQualityBicubic) 
        /// se a imagem ultrapassar o limite de resolução definido, poupando memória ao ecossistema.
        /// </summary>
        private Bitmap GetCroppedImageProcess(Rectangle rect, string resolution)
        {
            Bitmap cropped = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(originalScreenshot, new Rectangle(0, 0, cropped.Width, cropped.Height), rect, GraphicsUnit.Pixel);
                g.DrawImage(drawingLayer, new Rectangle(0, 0, cropped.Width, cropped.Height), rect, GraphicsUnit.Pixel);
            }

            if (resolution != "Auto")
            {
                string[] resParts = resolution.Split('x');
                if (resParts.Length == 2 && int.TryParse(resParts[0], out int maxW) && int.TryParse(resParts[1], out int maxH))
                {
                    if (cropped.Width > maxW || cropped.Height > maxH)
                    {
                        float ratio = Math.Min((float)maxW / cropped.Width, (float)maxH / cropped.Height);
                        int newW = (int)(cropped.Width * ratio);
                        int newH = (int)(cropped.Height * ratio);

                        Bitmap resized = new Bitmap(newW, newH);
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.DrawImage(cropped, 0, 0, newW, newH);
                        }
                        cropped.Dispose();
                        cropped = resized;
                    }
                }
            }
            return cropped;
        }

        /// <summary>
        /// Rotina de desenho baseada em vetores nativos (Graphics/Pen).
        /// Adapta dinamicamente as cores dos ícones para alto contraste consoante o Tema (Dark/Light).
        /// </summary>
        private void DrawIcon(Graphics g, string nome, Rectangle rect, bool isDarkMode)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; int pad = 8;
            Rectangle inner = new Rectangle(rect.X + pad, rect.Y + pad, rect.Width - pad * 2, rect.Height - pad * 2);

            Color iconColor = isDarkMode ? Color.White : Color.Black;

            using (Pen pTheme = new Pen(iconColor, 2f))
            {
                switch (nome)
                {
                    case "Caneta": g.DrawBezier(pTheme, inner.Left, inner.Bottom, inner.Left + 5, inner.Top, inner.Right - 5, inner.Bottom, inner.Right, inner.Top); break;
                    case "Linha": g.DrawLine(pTheme, inner.Left, inner.Top, inner.Right, inner.Bottom); break;
                    case "Seta": using (Pen pArrow = new Pen(iconColor, 2f) { CustomEndCap = new AdjustableArrowCap(4, 4) }) g.DrawLine(pArrow, inner.Left, inner.Bottom, inner.Right, inner.Top); break;
                    case "Forma": g.DrawRectangle(pTheme, inner); break;
                    case "Marcador": using (Pen pMark = new Pen(Color.FromArgb(180, Color.Yellow), 4f) { StartCap = LineCap.Square, EndCap = LineCap.Square }) g.DrawLine(pMark, inner.Left, inner.Bottom - 2, inner.Right, inner.Top + 2); break;
                    case "Texto": using (Font f = new Font("Segoe UI", 12, FontStyle.Bold)) { SizeF s = g.MeasureString("T", f); using (Brush brTheme = new SolidBrush(iconColor)) g.DrawString("T", f, brTheme, rect.X + (rect.Width - s.Width) / 2, rect.Y + (rect.Height - s.Height) / 2); } break;
                    case "Cor": Color drawColor = ferramentaAtual == "Marcador" ? highlighterColor : currentColor; using (Brush bColor = new SolidBrush(drawColor)) g.FillRectangle(bColor, inner); g.DrawRectangle(pTheme, inner); break;
                    case "Separator": if (MainContext.NavbarVertical) g.DrawLine(Pens.DimGray, rect.Left + 4, rect.Top + rect.Height / 2, rect.Right - 4, rect.Top + rect.Height / 2); else g.DrawLine(Pens.DimGray, rect.Left + rect.Width / 2, rect.Top + 4, rect.Left + rect.Width / 2, rect.Bottom - 4); break;
                    case "Desfazer": int arcW = inner.Width - 6; int arcH = inner.Height - 6; g.DrawArc(pTheme, inner.Left + 4, inner.Top + 6, arcW, arcH, 90, -180); g.DrawLine(pTheme, inner.Left + 4 + arcW / 2, inner.Top + 6, inner.Left, inner.Top + 6); g.DrawLine(pTheme, inner.Left, inner.Top + 6, inner.Left + 4, inner.Top + 2); g.DrawLine(pTheme, inner.Left, inner.Top + 6, inner.Left + 4, inner.Top + 10); break;
                    case "Refazer": int rArcW = inner.Width - 6; int rArcH = inner.Height - 6; g.DrawArc(pTheme, inner.Left + 2, inner.Top + 6, rArcW, rArcH, 90, 180); g.DrawLine(pTheme, inner.Left + 2 + rArcW / 2, inner.Top + 6, inner.Right, inner.Top + 6); g.DrawLine(pTheme, inner.Right, inner.Top + 6, inner.Right - 4, inner.Top + 2); g.DrawLine(pTheme, inner.Right, inner.Top + 6, inner.Right - 4, inner.Top + 10); break;
                    case "TelaCheia": g.DrawRectangle(pTheme, inner.X + 3, inner.Y + 3, inner.Width - 6, inner.Height - 6); g.DrawLine(pTheme, inner.X, inner.Y, inner.X + 4, inner.Y); g.DrawLine(pTheme, inner.X, inner.Y, inner.X, inner.Y + 4); g.DrawLine(pTheme, inner.Right, inner.Y, inner.Right - 4, inner.Y); g.DrawLine(pTheme, inner.Right, inner.Y, inner.Right, inner.Y + 4); g.DrawLine(pTheme, inner.X, inner.Bottom, inner.X + 4, inner.Bottom); g.DrawLine(pTheme, inner.X, inner.Bottom, inner.X, inner.Bottom - 4); g.DrawLine(pTheme, inner.Right, inner.Bottom, inner.Right - 4, inner.Bottom); g.DrawLine(pTheme, inner.Right, inner.Bottom, inner.Right, inner.Bottom - 4); break;
                    case "Salvar": g.DrawRectangle(pTheme, inner.X, inner.Y, inner.Width, inner.Height); g.DrawRectangle(pTheme, inner.X + 3, inner.Y, inner.Width - 6, 4); using (Brush solidTheme = new SolidBrush(iconColor)) g.FillRectangle(solidTheme, inner.X + 3, inner.Bottom - 5, inner.Width - 6, 5); break;
                    case "Copiar": g.DrawRectangle(pTheme, inner.X, inner.Y + 4, inner.Width - 4, inner.Height - 4); g.DrawLine(pTheme, inner.X + 4, inner.Y, inner.Right, inner.Y); g.DrawLine(pTheme, inner.Right, inner.Y, inner.Right, inner.Bottom - 4); break;
                    case "Fechar": g.DrawLine(pTheme, inner.Left, inner.Top, inner.Right, inner.Bottom); g.DrawLine(pTheme, inner.Right, inner.Top, inner.Left, inner.Bottom); break;
                }
            }
        }

        /// <summary>Interceta teclas para lidar com atalhos dinâmicos locais (como aumentar/diminuir espessura do pincel).</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)) { if (PenWidth < 50) PenWidth++; this.Invalidate(); e.Handled = true; return; }
            if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)) { if (PenWidth > 1) PenWidth--; this.Invalidate(); e.Handled = true; return; }
        }

        /// <summary>
        /// O Motor de Renderização de Ecrã (~60 FPS). 
        /// Pinta o fundo translúcido escuro, ilumina a área da seleção (com recorte), desenha os vetores ativos 
        /// e exibe o preview avançado da espessura/cor da ferramenta ativa diretamente na ponta do cursor.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.DrawImage(originalScreenshot, Point.Empty);
            using (Brush overlayBrush = new SolidBrush(Color.FromArgb(120, Color.Black))) g.FillRectangle(overlayBrush, this.ClientRectangle);

            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                g.DrawImage(originalScreenshot, selectionRect, selectionRect, GraphicsUnit.Pixel); g.DrawImage(drawingLayer, selectionRect, selectionRect, GraphicsUnit.Pixel);

                if (isDrawingToolActive && (ferramentaAtual == "Linha" || ferramentaAtual == "Seta" || ferramentaAtual == "Forma"))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(currentColor, PenWidth))
                    {
                        if (ferramentaAtual == "Linha") g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        else if (ferramentaAtual == "Seta") { p.CustomEndCap = new AdjustableArrowCap(PenWidth + 2, PenWidth + 2); g.DrawLine(p, drawStartPoint, drawCurrentPoint); }
                        else if (ferramentaAtual == "Forma") { int x = Math.Min(drawStartPoint.X, drawCurrentPoint.X); int y = Math.Min(drawStartPoint.Y, drawCurrentPoint.Y); g.DrawRectangle(p, x, y, Math.Abs(drawStartPoint.X - drawCurrentPoint.X), Math.Abs(drawStartPoint.Y - drawCurrentPoint.Y)); }
                    }
                }

                // Tríplice barreira geométrica para garantir que os recortes nunca se percam na imagem do monitor.
                using (Pen pBlack = new Pen(Color.Black, 1f)) using (Pen pWhite = new Pen(Color.White, 1f))
                {
                    g.DrawRectangle(pBlack, selectionRect.X, selectionRect.Y, selectionRect.Width - 1, selectionRect.Height - 1);
                    g.DrawRectangle(pWhite, selectionRect.X + 1, selectionRect.Y + 1, selectionRect.Width - 3, selectionRect.Height - 3);
                    g.DrawRectangle(pBlack, selectionRect.X + 2, selectionRect.Y + 2, selectionRect.Width - 5, selectionRect.Height - 5);
                }
            }

            if (isSelectionFinished && string.IsNullOrEmpty(ferramentaAtual))
            {
                using (Brush hb = new SolidBrush(Color.White)) using (Pen hp = new Pen(Color.Black, 1f))
                {
                    foreach (var h in GetHandles()) { g.FillRectangle(hb, h); g.DrawRectangle(hp, h); }
                }
            }

            if (isSelectionFinished && toolbarButtons.Count > 0)
            {
                // TEMA GLOBAL APLICADO NA NAVBAR FLUTUANTE
                Color navColor = MainContext.IsDarkMode ? Color.FromArgb(200, 30, 30, 30) : Color.FromArgb(200, 240, 240, 240);
                Color btnActiveColor = MainContext.IsDarkMode ? Color.DimGray : Color.LightGray;

                using (Brush navbarBg = new SolidBrush(navColor)) { g.FillRectangle(navbarBg, fullNavbarRect); g.DrawRectangle(Pens.Gray, fullNavbarRect); }

                foreach (var btn in toolbarButtons)
                {
                    if (btn.Key != "Separator") { if (ferramentaAtual == btn.Key || (btn.Key == "TelaCheia" && MainContext.FullScreenMode)) using (Brush activeBrush = new SolidBrush(btnActiveColor)) g.FillRectangle(activeBrush, btn.Value); }

                    // Envia a diretriz de tema para o ícone saber que cor desenhar-se
                    DrawIcon(g, btn.Key, btn.Value, MainContext.IsDarkMode);
                }
            }

            if (isSelectionFinished && !isDrawingToolActive && !string.IsNullOrEmpty(ferramentaAtual) && ferramentaAtual != "Texto" && ferramentaAtual != "Cor")
            {
                Point mousePos = this.PointToClient(Cursor.Position);
                if (selectionRect.Contains(mousePos) && !fullNavbarRect.Contains(mousePos))
                {
                    int pWidth = ferramentaAtual == "Marcador" ? PenWidth * 4 : PenWidth; Color pColor = ferramentaAtual == "Marcador" ? Color.FromArgb(180, highlighterColor) : currentColor;
                    using (SolidBrush pb = new SolidBrush(pColor)) using (Pen pBorder = new Pen(Color.White, 1f))
                    {
                        int radius = pWidth / 2; if (radius < 1) radius = 1;
                        g.FillEllipse(pb, mousePos.X - radius, mousePos.Y - radius, pWidth, pWidth); g.DrawEllipse(pBorder, mousePos.X - radius, mousePos.Y - radius, pWidth, pWidth);
                    }
                }
            }
        }

        /// <summary>
        /// Liberta explicitamente da memória os Bitmaps gigantes, gráficos intermédios e pilhas de anotações
        /// garantindo zero memory leak (vazamentos) após a janela ser descartada pela Nave-Mãe.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // FAILSAFE guardado contra recriação de handle "zumbi" no momento da destruição
                if (this.IsHandleCreated)
                {
                    HotkeyManager.UnregisterOverlayHotkeys(this.Handle);
                }

                originalScreenshot?.Dispose(); drawingLayer?.Dispose(); toolTip.Dispose();
                while (historicoDesenho.Count > 0) historicoDesenho.Pop().Dispose();
                while (historicoRefazer.Count > 0) historicoRefazer.Pop().Dispose();
            }
            base.Dispose(disposing);
        }
    }
}