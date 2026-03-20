using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    /// <summary>
    /// O Overlay principal do aplicativo (A tela escura onde se desenha e corta a imagem).
    /// Gerencia a área de seleção, ferramentas de desenho e a barra de ferramentas móvel.
    /// </summary>
    public partial class SelectionForm : Form
    {
        private Bitmap originalScreenshot;
        private Bitmap drawingLayer;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelectionFinished = false;

        // Histórico para o Ctrl+Z (Desfazer)
        private Stack<Bitmap> historicoDesenho = new Stack<Bitmap>();

        // Navbar
        private Dictionary<string, Rectangle> toolbarButtons = new Dictionary<string, Rectangle>();
        private Rectangle fullNavbarRect;
        private Point navbarCustomPosition = Point.Empty;
        private bool isNavbarMoved = false;

        // Tooltip
        private ToolTip toolTip = new ToolTip();
        private string lastButtonHovered = "";

        // Lógica de Movimentação/Redimensionamento
        private enum DragAction { None, Create, Move, ResizeTL, ResizeT, ResizeTR, ResizeR, ResizeBR, ResizeB, ResizeBL, ResizeL, MoveNavbar }
        private DragAction currentAction = DragAction.None;
        private Point dragStartPoint;
        private Rectangle originalSelectionRect;
        private const int HandleSize = 8;

        // --- SISTEMA DE DESENHO ---
        private string ferramentaAtual = "";
        private Color currentColor;
        private bool isDrawingToolActive = false;
        private Point drawStartPoint;
        private Point drawCurrentPoint;
        private TextBox txtInput;

        // Variáveis para a nova Navbar arrastável
        private string pressedButton = "";
        private bool isDraggingNavbar = false;

        public SelectionForm() { }

        /// <summary>Construtor: Recebe a print limpa, escurece a tela e aguarda a ação do usuário.</summary>
        public SelectionForm(Bitmap screenshot)
        {
            this.originalScreenshot = screenshot;
            this.drawingLayer = new Bitmap(screenshot.Width, screenshot.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(this.drawingLayer))
            {
                g.Clear(Color.Transparent);
            }

            try { this.currentColor = ColorTranslator.FromHtml(MainContext.LastColor); }
            catch { this.currentColor = Color.Red; }

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = SystemInformation.VirtualScreen;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;
            this.ShowInTaskbar = false;

            // Configuração do ToolTip para tradução
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;

            txtInput = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = this.currentColor
            };
            txtInput.KeyDown += TxtInput_KeyDown;
            txtInput.LostFocus += TxtInput_LostFocus;
            this.Controls.Add(txtInput);

            // 1. Garante que o atalho Ctrl+A é lido a qualquer momento
            this.KeyPreview = true;

            // 2. Se a expansão automática estiver ativa, preenche logo a tela
            if (MainContext.FullScreenMode)
            {
                SelecionarMonitorAtual();
            }
            // 3. Restaura apenas a seleção (se a opção estiver ativa)
            else if (MainContext.KeepSelection && !MainContext.LastSelection.IsEmpty)
            {
                if (SystemInformation.VirtualScreen.IntersectsWith(MainContext.LastSelection))
                {
                    selectionRect = MainContext.LastSelection;
                    isSelectionFinished = true;
                    CalcularPosicaoNavbar();
                }
            }
        }

        // --- SISTEMA DE DESENHO E FERRAMENTAS ---
        /// <summary>Abre o seletor nativo do Windows para escolher a cor das anotações.</summary>
        private void AbrirSeletorDeCor()
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = this.currentColor;
                cd.CustomColors = MainContext.CustomColors;
                cd.FullOpen = true;

                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    this.currentColor = cd.Color;
                    this.txtInput.ForeColor = cd.Color;

                    MainContext.LastColor = ColorTranslator.ToHtml(cd.Color);
                    MainContext.CustomColors = cd.CustomColors;

                    AppSettings config = SettingsManager.Load();
                    config.LastColor = MainContext.LastColor;
                    config.CustomColors = MainContext.CustomColors;
                    SettingsManager.Save(config);

                    this.Invalidate();
                }
            }
        }

        /// <summary>Grava o texto digitado (TextBox) definitivamente na camada de desenho (Bitmap transparente).</summary>
        private void FinalizarTexto()
        {
            if (txtInput.Visible && !string.IsNullOrWhiteSpace(txtInput.Text))
            {
                // Salva o estado atual na pilha ANTES de estampar o texto na imagem
                historicoDesenho.Push((Bitmap)drawingLayer.Clone());

                using (Graphics g = Graphics.FromImage(drawingLayer))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using (Brush b = new SolidBrush(currentColor))
                    {
                        g.DrawString(txtInput.Text, txtInput.Font, b, txtInput.Location);
                    }
                }
            }
            txtInput.Visible = false;
            txtInput.Text = "";
            this.Invalidate();
        }

        private void TxtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FinalizarTexto();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                txtInput.Visible = false;
                txtInput.Text = "";
                ferramentaAtual = "";
                this.Focus();
                this.Invalidate();
            }
        }

        private void TxtInput_LostFocus(object? sender, EventArgs e) => FinalizarTexto();

        // --- SISTEMA DE SELEÇÃO E REDIMENSIONAMENTO ---
        /// <summary>Atalho Ctrl+A: Pega os limites do monitor onde o mouse está e preenche a seleção.</summary>
        private void SelecionarMonitorAtual()
        {
            FinalizarTexto();
            Screen currentScreen = Screen.FromPoint(Cursor.Position);
            Rectangle screenBounds = currentScreen.Bounds;
            int x = screenBounds.X - this.Left;
            int y = screenBounds.Y - this.Top;
            selectionRect = new Rectangle(x, y, screenBounds.Width, screenBounds.Height);
            currentAction = DragAction.None;
            isDrawingToolActive = false;
            ferramentaAtual = "";
            isNavbarMoved = false;
            isSelectionFinished = true;
            CalcularPosicaoNavbar();
            this.Invalidate();
        }

        /// <summary>Calcula as coordenadas dos 8 "quadradinhos" (handles) de redimensionamento da borda.</summary>
        private Rectangle[] GetHandles()
        {
            int hs2 = HandleSize / 2;
            return new Rectangle[] {
                new Rectangle(selectionRect.Left - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left + selectionRect.Width/2 - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Top - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Top + selectionRect.Height/2 - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Right - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left + selectionRect.Width/2 - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left - hs2, selectionRect.Bottom - hs2, HandleSize, HandleSize),
                new Rectangle(selectionRect.Left - hs2, selectionRect.Top + selectionRect.Height/2 - hs2, HandleSize, HandleSize)
            };
        }

        /// <summary>Verifica em qual parte da borda o mouse está para determinar o tipo de cursor e ação (Resize, Move, etc).</summary>
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

        // --- EVENTOS DO MOUSE (O CORAÇÃO DO OVERLAY) ---
        /// <summary>Inicia uma ação: Criar seleção, redimensionar, mover navbar ou iniciar um desenho.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (!string.IsNullOrEmpty(ferramentaAtual))
                {
                    ferramentaAtual = "";
                    if (txtInput.Visible)
                    {
                        txtInput.Visible = false;
                        txtInput.Text = "";
                    }
                    this.Invalidate();
                }
                return;
            }

            if (e.Button != MouseButtons.Left) return;
            if (txtInput.Visible && !txtInput.Bounds.Contains(e.Location)) FinalizarTexto();

            if (isSelectionFinished)
            {
                // UX: Ao invés de executar o botão logo no clique (MouseDown),
                // registra onde clicou e espera o MouseMove ou MouseUp.
                if (fullNavbarRect.Contains(e.Location))
                {
                    currentAction = DragAction.MoveNavbar;
                    dragStartPoint = e.Location;
                    isDraggingNavbar = false;
                    pressedButton = "";

                    foreach (var btn in toolbarButtons)
                    {
                        if (btn.Value.Contains(e.Location))
                        {
                            pressedButton = btn.Key;
                            break;
                        }
                    }
                    return;
                }

                if (!string.IsNullOrEmpty(ferramentaAtual))
                {
                    if (ferramentaAtual == "Texto")
                    {
                        txtInput.Location = e.Location;
                        txtInput.Size = new Size(150, 25);
                        txtInput.Visible = true;
                        txtInput.Focus();
                    }
                    else
                    {
                        // Salva o estado atual da camada de desenho ANTES de começar um novo traço/forma
                        historicoDesenho.Push((Bitmap)drawingLayer.Clone());

                        isDrawingToolActive = true;
                        drawStartPoint = e.Location;
                        drawCurrentPoint = e.Location;
                    }
                    return;
                }

                currentAction = GetDragAction(e.Location);
                if (currentAction != DragAction.None)
                {
                    dragStartPoint = e.Location;
                    originalSelectionRect = selectionRect;
                    return;
                }

                isSelectionFinished = false;
                isNavbarMoved = false;
                toolbarButtons.Clear();
                ferramentaAtual = "";
                this.Invalidate();
            }

            currentAction = DragAction.Create;
            isNavbarMoved = false;
            startPoint = e.Location;
            selectionRect = new Rectangle(e.X, e.Y, 0, 0);
        }

        /// <summary>Executa a ação contínua: Arrastar a área, esticar bordas ou riscar a tela.</summary>
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
                        if (ferramentaAtual == "Caneta")
                        {
                            using (Pen p = new Pen(currentColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                                g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        }
                        else
                        {
                            Color semiTrans = Color.FromArgb(80, currentColor);
                            using (Pen p = new Pen(semiTrans, 16f) { StartCap = LineCap.Square, EndCap = LineCap.Square })
                                g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        }
                    }
                    drawStartPoint = drawCurrentPoint;
                }
                this.Invalidate();
            }
            else if (currentAction == DragAction.MoveNavbar)
            {
                int dx = e.X - dragStartPoint.X;
                int dy = e.Y - dragStartPoint.Y;

                // Se o mouse moveu mais de 3 pixels, consideramos arrasto e não clique!
                if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                    isDraggingNavbar = true;

                if (isDraggingNavbar)
                {
                    navbarCustomPosition.X += dx;
                    navbarCustomPosition.Y += dy;
                    dragStartPoint = e.Location;
                    isNavbarMoved = true;
                    CalcularPosicaoNavbar();
                    this.Invalidate();
                }
            }
            else if (currentAction == DragAction.Create)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                selectionRect = new Rectangle(x, y, Math.Abs(startPoint.X - e.X), Math.Abs(startPoint.Y - e.Y));
                this.Invalidate();
            }
            else if (currentAction == DragAction.Move)
            {
                int dx = e.X - dragStartPoint.X;
                int dy = e.Y - dragStartPoint.Y;
                selectionRect = new Rectangle(originalSelectionRect.X + dx, originalSelectionRect.Y + dy, originalSelectionRect.Width, originalSelectionRect.Height);
                CalcularPosicaoNavbar();
                this.Invalidate();
            }
            else if (currentAction != DragAction.None)
            {
                int dx = e.X - dragStartPoint.X; int dy = e.Y - dragStartPoint.Y;
                int l = originalSelectionRect.Left; int r = originalSelectionRect.Right;
                int t = originalSelectionRect.Top; int b = originalSelectionRect.Bottom;
                if (currentAction == DragAction.ResizeTL) { l += dx; t += dy; }
                if (currentAction == DragAction.ResizeT) { t += dy; }
                if (currentAction == DragAction.ResizeTR) { r += dx; t += dy; }
                if (currentAction == DragAction.ResizeR) { r += dx; }
                if (currentAction == DragAction.ResizeBR) { r += dx; b += dy; }
                if (currentAction == DragAction.ResizeB) { b += dy; }
                if (currentAction == DragAction.ResizeBL) { l += dx; b += dy; }
                if (currentAction == DragAction.ResizeL) { l += dx; }
                selectionRect = new Rectangle(Math.Min(l, r), Math.Min(t, b), Math.Abs(r - l), Math.Abs(b - t));
                CalcularPosicaoNavbar();
                this.Invalidate();
            }
            else if (isSelectionFinished)
            {
                string hoveredBtn = "";
                foreach (var btn in toolbarButtons)
                {
                    if (btn.Value.Contains(e.Location))
                    {
                        hoveredBtn = btn.Key;
                        break;
                    }
                }

                // Gerenciamento de ToolTip Traduzido
                if (hoveredBtn != lastButtonHovered)
                {
                    lastButtonHovered = hoveredBtn;
                    if (!string.IsNullOrEmpty(hoveredBtn))
                    {
                        toolTip.Show(LanguageManager.GetString(hoveredBtn), this, e.X, e.Y + 25, 3000);
                    }
                    else
                    {
                        toolTip.Hide(this);
                    }
                }

                if (!string.IsNullOrEmpty(hoveredBtn)) this.Cursor = Cursors.Hand;
                else if (fullNavbarRect.Contains(e.Location)) this.Cursor = Cursors.SizeAll;
                else if (!string.IsNullOrEmpty(ferramentaAtual)) this.Cursor = Cursors.Cross;
                else
                {
                    var action = GetDragAction(e.Location);
                    if (action == DragAction.Move) this.Cursor = Cursors.SizeAll;
                    else if (action == DragAction.ResizeTL || action == DragAction.ResizeBR) this.Cursor = Cursors.SizeNWSE; // Canto superior-esquerdo e inferior-direito
                    else if (action == DragAction.ResizeTR || action == DragAction.ResizeBL) this.Cursor = Cursors.SizeNESW; // Canto superior-direito e inferior-esquerdo
                    else if (action == DragAction.ResizeT || action == DragAction.ResizeB) this.Cursor = Cursors.SizeNS; // Cima e Baixo
                    else if (action == DragAction.ResizeL || action == DragAction.ResizeR) this.Cursor = Cursors.SizeWE; // Esquerda e Direita
                    else this.Cursor = Cursors.Default;
                }
            }
        }

        /// <summary>Finaliza a ação atual (soltar o clique).</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Se soltou o clique sem arrastar, ativa o botão!
            if (currentAction == DragAction.MoveNavbar)
            {
                if (!isDraggingNavbar && !string.IsNullOrEmpty(pressedButton))
                {
                    ExecutarAcaoToolbar(pressedButton);
                }
                else if (isDraggingNavbar)
                {
                    // Se moveu a navbar, guarda independentemente da seleção (se a opção estiver ativa)
                    SalvarPosicoes();
                }
                currentAction = DragAction.None;
                this.Invalidate();
                return;
            }

            if (isDrawingToolActive)
            {
                isDrawingToolActive = false;
                if (ferramentaAtual == "Linha" || ferramentaAtual == "Seta" || ferramentaAtual == "Forma")
                {
                    using (Graphics g = Graphics.FromImage(drawingLayer))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (Pen p = new Pen(currentColor, 3f))
                        {
                            if (ferramentaAtual == "Linha") g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                            else if (ferramentaAtual == "Seta")
                            {
                                p.CustomEndCap = new AdjustableArrowCap(5, 5);
                                g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                            }
                            else if (ferramentaAtual == "Forma")
                            {
                                int x = Math.Min(drawStartPoint.X, drawCurrentPoint.X);
                                int y = Math.Min(drawStartPoint.Y, drawCurrentPoint.Y);
                                g.DrawRectangle(p, x, y, Math.Abs(drawStartPoint.X - drawCurrentPoint.X), Math.Abs(drawStartPoint.Y - drawCurrentPoint.Y));
                            }
                        }
                    }
                }
                this.Invalidate();
            }
            else if (currentAction != DragAction.None)
            {
                currentAction = DragAction.None;
                if (selectionRect.Width > 20 && selectionRect.Height > 20)
                {
                    isSelectionFinished = true;
                    CalcularPosicaoNavbar();

                    // Salva as novas dimensões isoladamente
                    SalvarPosicoes();
                }
                else
                {
                    isSelectionFinished = false;
                    toolbarButtons.Clear();
                }
                this.Invalidate();
            }
        }

        /// <summary>Guarda as posições ativas independentemente umas das outras.</summary>
        private void SalvarPosicoes()
        {
            bool mudouAlgo = false;
            AppSettings config = SettingsManager.Load();

            if (MainContext.KeepSelection && isSelectionFinished)
            {
                MainContext.LastSelection = selectionRect;
                config.LastSelection = MainContext.LastSelection;
                mudouAlgo = true;
            }

            if (MainContext.KeepNavbarPosition && isNavbarMoved)
            {
                MainContext.LastNavbarPosition = navbarCustomPosition;
                config.LastNavbarPosition = MainContext.LastNavbarPosition;
                mudouAlgo = true;
            }

            if (mudouAlgo) SettingsManager.Save(config);
        }


        // --- NAVBAR (BARRA DE FERRAMENTAS) ---
        /// <summary>Calcula as coordenadas da barra de ferramentas, mantendo-a dentro dos limites visíveis do monitor.</summary>
        private void CalcularPosicaoNavbar()
        {
            toolbarButtons.Clear();
            string[] ferramentas = { "Caneta", "Linha", "Seta", "Forma", "Marcador", "Texto", "Cor", "TelaCheia", "Salvar", "Copiar", "Fechar" };

            int bW = 34, bH = 34, margin = 4;
            int paddingBorder = 8;

            int totalWidth = MainContext.NavbarVertical ? (bW + paddingBorder * 2) : ((bW + margin) * ferramentas.Length - margin + paddingBorder * 2);
            int totalHeight = MainContext.NavbarVertical ? ((bH + margin) * ferramentas.Length - margin + paddingBorder * 2) : (bH + paddingBorder * 2);

            if (!isNavbarMoved)
            {
                // Se o utilizador pediu para manter a posição da navbar, ignora o grude padrão
                if (MainContext.KeepNavbarPosition && !MainContext.LastNavbarPosition.IsEmpty)
                {
                    navbarCustomPosition = MainContext.LastNavbarPosition;
                    isNavbarMoved = true; // "Finge" que o usuário a moveu para a travar ali

                    // Limites de segurança para garantir que a barra não sai do ecrã se mudarmos de monitor
                    if (navbarCustomPosition.X + totalWidth > this.Width) navbarCustomPosition.X = this.Width - totalWidth - paddingBorder;
                    if (navbarCustomPosition.Y + totalHeight > this.Height) navbarCustomPosition.Y = this.Height - totalHeight - paddingBorder;
                    if (navbarCustomPosition.X < 0) navbarCustomPosition.X = paddingBorder;
                    if (navbarCustomPosition.Y < 0) navbarCustomPosition.Y = paddingBorder;
                }
                else
                {
                    int currentX = selectionRect.Right - totalWidth - paddingBorder;
                    int toolbarY = selectionRect.Bottom - totalHeight - paddingBorder;
                    if (currentX < selectionRect.Left) currentX = selectionRect.Left + paddingBorder;
                    if (toolbarY < selectionRect.Top) toolbarY = selectionRect.Bottom + paddingBorder;
                    if (currentX < 0) currentX = paddingBorder;
                    if (currentX + totalWidth > this.Width) currentX = this.Width - totalWidth - paddingBorder;
                    if (toolbarY + totalHeight > this.Height) toolbarY = this.Height - totalHeight - paddingBorder;
                    navbarCustomPosition = new Point(currentX, toolbarY);
                }
            }

            fullNavbarRect = new Rectangle(navbarCustomPosition.X, navbarCustomPosition.Y, totalWidth, totalHeight);
            int btnX = navbarCustomPosition.X + paddingBorder;
            int btnY = navbarCustomPosition.Y + paddingBorder;

            foreach (string btn in ferramentas)
            {
                toolbarButtons.Add(btn, new Rectangle(btnX, btnY, bW, bH));

                if (MainContext.NavbarVertical)
                    btnY += bH + margin;
                else
                    btnX += bW + margin;
            }
        }

        /// <summary>Gerencia cliques nos botões da navbar (Trocar ferramenta, Salvar, Copiar).</summary>
        private void ExecutarAcaoToolbar(string acao)
        {
            FinalizarTexto();
            switch (acao)
            {
                case "Caneta": case "Linha": case "Seta": case "Forma": case "Marcador": case "Texto": ferramentaAtual = (ferramentaAtual == acao ? "" : acao); this.Invalidate(); break;
                case "Cor": AbrirSeletorDeCor(); break;
                case "TelaCheia":
                    MainContext.FullScreenMode = !MainContext.FullScreenMode;
                    AppSettings config = SettingsManager.Load();
                    config.FullScreenMode = MainContext.FullScreenMode;
                    SettingsManager.Save(config);

                    if (MainContext.FullScreenMode) SelecionarMonitorAtual();
                    this.Invalidate();
                    break;
                case "Copiar":
                    Bitmap cropped = GetCroppedImage();
                    Clipboard.SetImage(cropped);
                    if (MainContext.ShowNotifications) MainContext.ShowToast(LanguageManager.GetString("ToastCopied"), cropped);
                    this.Close();
                    break;
                case "Salvar": SalvarImagem(); break;
                case "Fechar": this.Close(); break;
            }
        }

        /// <summary>Desenha manualmente (vetorialmente) os ícones das ferramentas dentro dos botões.</summary>
        private void DrawIcon(Graphics g, string nome, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = 8;
            Rectangle inner = new Rectangle(rect.X + pad, rect.Y + pad, rect.Width - pad * 2, rect.Height - pad * 2);
            using (Pen pWhite = new Pen(Color.White, 2f))
            {
                switch (nome)
                {
                    case "Caneta": g.DrawBezier(pWhite, inner.Left, inner.Bottom, inner.Left + 5, inner.Top, inner.Right - 5, inner.Bottom, inner.Right, inner.Top); break;
                    case "Linha": g.DrawLine(pWhite, inner.Left, inner.Top, inner.Right, inner.Bottom); break;
                    case "Seta": using (Pen pArrow = new Pen(Color.White, 2f) { CustomEndCap = new AdjustableArrowCap(4, 4) }) g.DrawLine(pArrow, inner.Left, inner.Bottom, inner.Right, inner.Top); break;
                    case "Forma": g.DrawRectangle(pWhite, inner); break;
                    case "Marcador": using (Pen pMark = new Pen(Color.FromArgb(180, Color.Yellow), 4f) { StartCap = LineCap.Square, EndCap = LineCap.Square }) g.DrawLine(pMark, inner.Left, inner.Bottom - 2, inner.Right, inner.Top + 2); break;
                    case "Texto": using (Font f = new Font("Segoe UI", 12, FontStyle.Bold)) { SizeF s = g.MeasureString("T", f); g.DrawString("T", f, Brushes.White, rect.X + (rect.Width - s.Width) / 2, rect.Y + (rect.Height - s.Height) / 2); } break;
                    case "Cor": using (Brush bColor = new SolidBrush(currentColor)) g.FillRectangle(bColor, inner); g.DrawRectangle(Pens.White, inner); break;
                    case "TelaCheia":
                        // Desenha um pequeno quadrado central e bordas de expansão nos cantos
                        g.DrawRectangle(pWhite, inner.X + 3, inner.Y + 3, inner.Width - 6, inner.Height - 6);
                        g.DrawLine(pWhite, inner.X, inner.Y, inner.X + 4, inner.Y);
                        g.DrawLine(pWhite, inner.X, inner.Y, inner.X, inner.Y + 4);
                        g.DrawLine(pWhite, inner.Right, inner.Y, inner.Right - 4, inner.Y);
                        g.DrawLine(pWhite, inner.Right, inner.Y, inner.Right, inner.Y + 4);
                        g.DrawLine(pWhite, inner.X, inner.Bottom, inner.X + 4, inner.Bottom);
                        g.DrawLine(pWhite, inner.X, inner.Bottom, inner.X, inner.Bottom - 4);
                        g.DrawLine(pWhite, inner.Right, inner.Bottom, inner.Right - 4, inner.Bottom);
                        g.DrawLine(pWhite, inner.Right, inner.Bottom, inner.Right, inner.Bottom - 4);
                        break;
                    case "Salvar": g.DrawRectangle(pWhite, inner.X, inner.Y, inner.Width, inner.Height); g.DrawRectangle(pWhite, inner.X + 3, inner.Y, inner.Width - 6, 4); g.FillRectangle(Brushes.White, inner.X + 3, inner.Bottom - 5, inner.Width - 6, 5); break;
                    case "Copiar": g.DrawRectangle(pWhite, inner.X, inner.Y + 4, inner.Width - 4, inner.Height - 4); g.DrawLine(pWhite, inner.X + 4, inner.Y, inner.Right, inner.Y); g.DrawLine(pWhite, inner.Right, inner.Y, inner.Right, inner.Bottom - 4); break;
                    case "Fechar": g.DrawLine(pWhite, inner.Left, inner.Top, inner.Right, inner.Bottom); g.DrawLine(pWhite, inner.Right, inner.Top, inner.Left, inner.Bottom); break;
                }
            }
        }

        // --- SAÍDA DE IMAGEM E RENDERIZAÇÃO ---
        /// <summary>Abre o FileDialog para salvar a imagem combinada diretamente no disco (PNG, JPG, BMP).</summary>
        private void SalvarImagem()
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Title = "LiteShot", Filter = "PNG|*.png|JPG|*.jpg|BMP|*.bmp", FileName = "LiteShot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    FinalizarTexto();
                    Bitmap cropped = GetCroppedImage();
                    cropped.Save(sfd.FileName);
                    if (MainContext.ShowNotifications) MainContext.ShowToast(LanguageManager.GetString("ToastSaved"), cropped);
                    this.Close();
                }
            }
        }

        /// <summary>Junta o print original + os desenhos (camada transparente) dentro da área selecionada.</summary>
        private Bitmap GetCroppedImage()
        {
            Bitmap cropped = new Bitmap(selectionRect.Width, selectionRect.Height);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(originalScreenshot, new Rectangle(0, 0, cropped.Width, cropped.Height), selectionRect, GraphicsUnit.Pixel);
                g.DrawImage(drawingLayer, new Rectangle(0, 0, cropped.Width, cropped.Height), selectionRect, GraphicsUnit.Pixel);
            }
            return cropped;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { if (!string.IsNullOrEmpty(ferramentaAtual)) { ferramentaAtual = ""; this.Invalidate(); } else this.Close(); }

            // --- CORREÇÃO DE VAZAMENTOS E ATALHOS ---
            if (e.Control && e.KeyCode == Keys.C && isSelectionFinished)
            {
                e.SuppressKeyPress = true; e.Handled = true; // Bloqueia o vazamento
                ExecutarAcaoToolbar("Copiar");
            }

            if (e.Control && e.KeyCode == Keys.S && isSelectionFinished)
            {
                e.SuppressKeyPress = true; e.Handled = true; // Bloqueia o vazamento
                ExecutarAcaoToolbar("Salvar");
            }

            if (e.Control && e.KeyCode == Keys.A)
            {
                e.SuppressKeyPress = true; e.Handled = true; // Bloqueia o vazamento
                SelecionarMonitorAtual();
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                e.SuppressKeyPress = true; e.Handled = true; // Bloqueia o vazamento
                if (historicoDesenho.Count > 0)
                {
                    Bitmap oldLayer = drawingLayer;
                    drawingLayer = historicoDesenho.Pop();
                    oldLayer.Dispose();
                    this.Invalidate();
                }
            }
        }

        /// <summary>Motor de renderização: Desenha o fundo escuro, a seleção iluminada, as ferramentas ativas e os botões 60x por segundo.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(originalScreenshot, Point.Empty);
            using (Brush overlayBrush = new SolidBrush(Color.FromArgb(120, Color.Black)))
                g.FillRectangle(overlayBrush, this.ClientRectangle);

            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                g.DrawImage(originalScreenshot, selectionRect, selectionRect, GraphicsUnit.Pixel);
                g.DrawImage(drawingLayer, selectionRect, selectionRect, GraphicsUnit.Pixel);

                if (isDrawingToolActive && (ferramentaAtual == "Linha" || ferramentaAtual == "Seta" || ferramentaAtual == "Forma"))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(currentColor, 3f))
                    {
                        if (ferramentaAtual == "Linha") g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        else if (ferramentaAtual == "Seta")
                        {
                            p.CustomEndCap = new AdjustableArrowCap(5, 5);
                            g.DrawLine(p, drawStartPoint, drawCurrentPoint);
                        }
                        else if (ferramentaAtual == "Forma")
                        {
                            int x = Math.Min(drawStartPoint.X, drawCurrentPoint.X);
                            int y = Math.Min(drawStartPoint.Y, drawCurrentPoint.Y);
                            g.DrawRectangle(p, x, y, Math.Abs(drawStartPoint.X - drawCurrentPoint.X), Math.Abs(drawStartPoint.Y - drawCurrentPoint.Y));
                        }
                    }
                }
                g.DrawRectangle(Pens.White, selectionRect);
            }

            if (isSelectionFinished && string.IsNullOrEmpty(ferramentaAtual))
            {
                using (Brush hb = new SolidBrush(Color.White))
                    foreach (var h in GetHandles()) g.FillRectangle(hb, h);
            }

            if (isSelectionFinished && toolbarButtons.Count > 0)
            {
                using (Brush navbarBg = new SolidBrush(Color.FromArgb(200, 30, 30, 30)))
                {
                    g.FillRectangle(navbarBg, fullNavbarRect);
                    g.DrawRectangle(Pens.Gray, fullNavbarRect);
                }
                foreach (var btn in toolbarButtons)
                {
                    if (ferramentaAtual == btn.Key || (btn.Key == "TelaCheia" && MainContext.FullScreenMode))
                    {
                        using (Brush activeBrush = new SolidBrush(Color.DimGray))
                        {
                            g.FillRectangle(activeBrush, btn.Value);
                        }
                    }
                    DrawIcon(g, btn.Key, btn.Value);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                originalScreenshot?.Dispose();
                drawingLayer?.Dispose();
                toolTip.Dispose();

                // Limpa a memória das imagens guardadas na pilha
                while (historicoDesenho.Count > 0)
                {
                    historicoDesenho.Pop().Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}