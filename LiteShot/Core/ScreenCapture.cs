using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LiteShot.Core
{
    /// <summary>
    /// Utilitário estático responsável pelo motor de captura de imagem do ecrã.
    /// Utiliza a matemática de união física de ecrãs (DPI-Awareness V2) para suportar
    /// múltiplos monitores com resoluções e escalas diferentes sem distorção.
    /// </summary>
    public static class ScreenCapture
    {
        /// <summary>
        /// Tira uma "foto" de toda a área de trabalho física (todos os monitores combinados).
        /// Lê a configuração de captura de cursor em tempo real e embute-o na imagem se necessário.
        /// </summary>
        /// <returns>Um objeto Bitmap contendo a imagem completa de todos os ecrãs sem cortes.</returns>
        public static Bitmap CaptureAllScreens()
        {
            // 1. Calcula o Bounding Box físico exato unindo todos os monitores
            Rectangle bounds = GetPhysicalBounds();

            // 2. Gera o Bitmap com o tamanho perfeito da união
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                // 3. Copia os pixels da tela para o nosso objeto Bitmap respeitando o Offset
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                // 4. Adiciona o cursor do rato se a opção estiver ativada nas configurações
                AppSettings config = SettingsManager.Load();
                if (config.CaptureCursor)
                {
                    try
                    {
                        // Calcula a posição do rato relativa à imagem capturada física
                        Point mousePos = new Point(Cursor.Position.X - bounds.X, Cursor.Position.Y - bounds.Y);

                        // Desenha o cursor padrão do Windows em cima da imagem
                        Cursors.Default.Draw(g, new Rectangle(mousePos, Cursors.Default.Size));
                    }
                    catch (Exception ex)
                    {
                        // Registra o erro silenciosamente no Output do Visual Studio.
                        // Evita que o aplicativo "crashe" se o Windows bloquear o acesso ao cursor
                        // (ex: telas de UAC, cursores de hardware exclusivos, etc).
                        Debug.WriteLine($"[LiteShot] Aviso: Não foi possível desenhar o cursor do rato. Erro: {ex.Message}");
                    }
                }
            }

            return screenshot;
        }

        /// <summary>
        /// Calcula o retângulo global (Bounding Box) que engloba fisicamente 
        /// todos os monitores ligados ao sistema operativo.
        /// </summary>
        /// <returns>Um Rectangle representando a união matemática exata de todos os ecrãs.</returns>
        public static Rectangle GetPhysicalBounds()
        {
            Rectangle bounds = Rectangle.Empty;
            foreach (Screen screen in Screen.AllScreens)
            {
                bounds = Rectangle.Union(bounds, screen.Bounds);
            }
            return bounds;
        }
    }
}