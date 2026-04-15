using System.Windows.Forms;
using LiteTools.Interfaces;
using LiteShot.UI;

namespace LiteShot
{
    /// <summary>
    /// A Porta de Entrada oficial do LiteShot quando executado no Modo Dual (.dll).
    /// O Host (LiteTools) vai procurar e instanciar esta classe automaticamente.
    /// </summary>
    public class LiteShotPlugin : ILitePlugin
    {
        // Mantemos a referência ao contexto para que o Garbage Collector não o destrua
        private MainContext? _appContext;

        public string Name => "LiteShot";

        public string Version => "2.0.0";

        /// <summary>
        /// O Host chama este método passando o canal de comunicação e o idioma global atual.
        /// </summary>
        public void Initialize(IImagePublisher publisher, string currentLanguage)
        {
            // Instanciamos o Cérebro do app passando o publisher e o Idioma do Host
            _appContext = new MainContext(publisher, currentLanguage);

            // Esconde o ícone da bandeja, já que quem manda agora é o LiteTools
            _appContext.IsPluginMode = true;
        }

        /// <summary>
        /// O Host pede a interface gráfica de configurações do LiteShot.
        /// </summary>
        public UserControl GetSettingsUI()
        {
            // Passamos o cérebro real (_appContext) em vez de null!
            // Agora o SettingsControl consegue chamar o contexto para atualizar as hotkeys na hora.
            var ui = new SettingsControl(_appContext);
            return ui;
        }

        /// <summary>
        /// Chamado pelo Host (LiteTools) quando o sistema está a ser encerrado.
        /// </summary>
        public void Shutdown()
        {
            if (_appContext != null)
            {
                // Limpa os recursos do LiteShot da memória do Windows
                _appContext.Dispose();
                _appContext = null;
            }
        }
    }
}