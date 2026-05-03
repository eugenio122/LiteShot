using System.Drawing;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    /// <summary>
    /// A "Casca" das configurações (O Formulário nativo do Windows).
    /// Usado APENAS quando o LiteShot roda como .exe autônomo (Standalone).
    /// A sua única função é criar uma janela isolada e hospedar o SettingsControl (Miolo) no interior.
    /// Em modo Plugin (.dll), esta classe é completamente ignorada, pois a Nave-Mãe hospeda o Miolo.
    /// </summary>
    public partial class SettingsForm : Form
    {
        /// <summary>
        /// Construtor da janela de opções. Define o tamanho, título e injeta o UserControl.
        /// </summary>
        /// <param name="ctx">O "Cérebro" ativo do aplicativo para sincronização de atalhos e idioma.</param>
        public SettingsForm(MainContext ctx)
        {
            this.Text = LanguageManager.GetString("SettingsTitle");

            this.Size = new Size(450, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Instancia o Miolo (UserControl com a verdadeira inteligência)
            SettingsControl miolo = new SettingsControl(ctx);
            miolo.Dock = DockStyle.Fill; // Preenche a janela toda

            // Como removemos o evento RequestClose do miolo, a janela agora
            // só é fechada manualmente pelo utilizador no 'X' nativo do Windows.
            this.Controls.Add(miolo);
        }
    }
}