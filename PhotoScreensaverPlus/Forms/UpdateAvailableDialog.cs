using PhotoScreensaverPlus.State;
using System;
using System.Windows.Forms;

namespace PhotoScreensaverPlus.Forms
{
    public partial class UpdateAvailableDialog : Form
    {
        ApplicationState state = ApplicationState.getInstance();
        int countDown = 15;
        public UpdateAvailableDialog()
        {
            InitializeComponent();
            timer1.Interval = 1000;
            Text = "New version available";
            label1.Text = "Do you want to visit screensaver home page?";
        }

        private void UpdateAvailableDialog_Shown(object sender, EventArgs e)
        {
            //AppState.ExitOnlyWithEscape = true;
            timer1.Start();
            buttonNo.Text = "NO (" + countDown.ToString() + ")";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            countDown--;
            buttonNo.Text = "NO ("+ countDown.ToString()+ ")";

            if (countDown == 0)
            {
                //AppState.ExitOnlyWithEscape = false;
                this.Close();
            }
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            //AppState.ExitOnlyWithEscape = false;
            this.Close();
        }

        private void buttonYes_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(state.Url);
            this.Close();
            Application.Exit();
        }
    }
}
