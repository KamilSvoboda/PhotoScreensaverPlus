using PhotoScreensaverPlus.Forms;
using System.Drawing;

namespace PhotoScreensaverPlus.State
{
    /// <summary>
    /// Holds grafics of the form
    /// </summary>
    class ScreenDefinition
    {
        public MainForm ScreenForm { get; set; }
        public Graphics FormGraphics { get; set; }
        public bool IsPrimaryScreen { get; set; }
        public ScreenDefinition(MainForm form, Graphics graphics, bool isPrimaryScreen)
        {
            ScreenForm = form;
            FormGraphics = graphics;
            IsPrimaryScreen = isPrimaryScreen;
        }
    }
}
