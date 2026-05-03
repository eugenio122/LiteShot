using System;
using System.Windows.Forms;
using LiteShot.UI;

namespace LiteShot
{
    /// <summary>
    /// Classe estática fundamental que atua como Porta de Entrada do Modo Standalone (.exe).
    /// Este código NÃO é executado quando o sistema é carregado como Plugin (.dll).
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Ponto de arranque (Entry Point) gerido pelo Windows.
        /// Prepara a renderização e lança a aplicação para a Bandeja do Sistema (Tray).
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Inicializa as configurações visuais e aplica o HighDpiMode definido no .csproj
            ApplicationConfiguration.Initialize();

            // Roda o Contexto de Aplicação (MainContext) em vez de um Form visível.
            // Isso permite que o programa fique invisível no System Tray a escutar o teclado.
            Application.Run(new MainContext());
        }
    }
}