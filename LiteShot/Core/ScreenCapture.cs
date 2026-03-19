using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LiteShot.Core
{
    /// <summary>
    /// Utilitário estático para capturar a imagem do monitor.
    /// </summary>
    public static class ScreenCapture
    {
        /// <summary>Tira uma "foto" de toda a área de trabalho virtual (todos os monitores combinados).</summary>
        public static Bitmap CaptureAllScreens()
        {
            // VirtualScreen pega a área total de todos os monitores
            Rectangle bounds = SystemInformation.VirtualScreen;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Copia os pixels da tela para o nosso objeto Bitmap
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                // Adiciona o cursor do mouse se a opção estiver ativada
                AppSettings config = SettingsManager.Load();
                if (config.CaptureCursor)
                {
                    try
                    {
                        // Calcula a posição do mouse relativa à imagem capturada
                        Point mousePos = new Point(Cursor.Position.X - bounds.X, Cursor.Position.Y - bounds.Y);

                        // Desenha o cursor padrão do Windows em cima da imagem
                        Cursors.Default.Draw(g, new Rectangle(mousePos, Cursors.Default.Size));
                    }
                    catch (Exception ex)
                    {
                        // Registra o erro silenciosamente no Output do Visual Studio.
                        // Evita que o aplicativo "crashe" se o Windows bloquear o acesso ao cursor
                        // (ex: telas de UAC, cursores de hardware exclusivos, etc).
                        Debug.WriteLine($"[LiteShot] Aviso: Não foi possível desenhar o cursor do mouse. Erro: {ex.Message}");
                    }
                }
            }

            return bitmap;
        }
    }
}