using System;
using System.Collections.Generic;
using System.Text;

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
            }

            return bitmap;
        }
    }
}
