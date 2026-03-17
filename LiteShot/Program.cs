using LiteShot.UI;

namespace LiteShot
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para a aplicação.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Inicializa configurações visuais e de DPI com base no LiteShot.csproj
            ApplicationConfiguration.Initialize();

            // Roda o Contexto de Aplicação (Bandeja do Sistema) em vez de um Form
            Application.Run(new MainContext());
        }
    }
}