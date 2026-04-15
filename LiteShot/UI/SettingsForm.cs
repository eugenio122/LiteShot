using System.Drawing;
using System.Windows.Forms;
using LiteShot.Core;

namespace LiteShot.UI
{
    /// <summary>
    /// A Casca das configurações (O Form).
    /// Usado apenas quando o LiteShot roda como .exe autônomo.
    /// A sua única função é criar uma janela do Windows e colocar o SettingsControl dentro.
    /// </summary>
    public partial class SettingsForm : Form
    {
        public SettingsForm(MainContext ctx)
        {
            this.Text = LanguageManager.GetString("SettingsTitle");

            this.Size = new Size(450, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Instancia o Miolo
            SettingsControl miolo = new SettingsControl(ctx);
            miolo.Dock = DockStyle.Fill; // Preenche a janela toda

            // Como removemos o evento RequestClose do miolo, a janela agora
            // só é fechada manualmente pelo utilizador no 'X' nativo do Windows.

            this.Controls.Add(miolo);
        }
    }
}