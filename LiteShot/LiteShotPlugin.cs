using System.Windows.Forms;
using LiteTools.Interfaces;
using LiteShot.UI;

namespace LiteShot
{
    /// <summary>
    /// A Porta de Entrada oficial do LiteShot quando executado no Modo Plugin (.dll).
    /// O Host (LiteTools) vai procurar e instanciar esta classe automaticamente usando Reflection.
    /// É o ponto de ligação vital entre o ecossistema LiteTools e a lente de captura.
    /// </summary>
    public class LiteShotPlugin : ILitePlugin
    {
        // Mantemos a referência ao contexto para que o Garbage Collector não o destrua
        private MainContext? _appContext;

        /// <summary>Nome público do plugin reconhecido pela Nave-Mãe.</summary>
        public string Name => "LiteShot";

        /// <summary>Versão da arquitetura (Atualizada para o padrão EventBus / Mediator).</summary>
        public string Version => "2.0.0";

        /// <summary>
        /// Método de arranque invocado pelo Host (LiteTools). 
        /// Injeta o Contexto Global, o Barramento de Eventos e obriga o LiteShot a falar o Idioma do Host.
        /// </summary>
        /// <param name="hostContext">Memória partilhada da Nave-Mãe.</param>
        /// <param name="eventBus">O canal de comunicação assíncrona (Pub/Sub).</param>
        /// <param name="currentLanguage">O idioma global ditado pelo utilizador no Host.</param>
        public void Initialize(ILiteHostContext hostContext, IEventBus eventBus, string currentLanguage)
        {
            // Instanciamos o Cérebro do app injetando a nova arquitetura
            _appContext = new MainContext(hostContext, eventBus, currentLanguage);

            // Esconde o ícone da bandeja, já que quem manda agora é o LiteTools
            _appContext.IsPluginMode = true;
        }

        /// <summary>
        /// O Host pede a interface gráfica de configurações do LiteShot para embutir na sua própria janela.
        /// </summary>
        /// <returns>O UserControl (Miolo) contendo as opções de resolução, atalhos, etc.</returns>
        public UserControl GetSettingsUI()
        {
            return new SettingsControl(_appContext);
        }

        /// <summary>
        /// Chamado pelo Host (LiteTools) quando o sistema global está a ser encerrado.
        /// Desmonta hooks de teclado, liberta a RAM e encerra o módulo com segurança.
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