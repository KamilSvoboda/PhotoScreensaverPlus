using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using PhotoScreensaverPlus.State;

namespace PhotoScreensaverPlus.Forms
{
    public partial class SettingsForm : Form
    {
        ApplicationState state = ApplicationState.getInstance();

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            this.Text = ApplicationState.APP_NAME_WITH_VERSION + " Settings";
            lblVersion.Text = Application.ProductVersion;

            lstImageFolders.Items.AddRange(state.ImagesRootFolders.ToArray());
            listBoxNoImageFolders.Items.AddRange(state.DontShowFolders.ToArray());
            listBoxNoImages.Items.AddRange(state.DontShowImages.ToArray());

            textBoxFile.Text = state.FileWithImagePaths;
            textBoxCopyToFolder.Text = state.CopyToFolder;
            textBoxSavePathToFolder.Text = state.SavePathToFolder;
            textBoxFile1.Text = state.SavePathToFileF1;
            textBoxFile2.Text = state.SavePathToFileF2;
            textBoxFile3.Text = state.SavePathToFileF3;
            textBoxFile4.Text = state.SavePathToFileF4;
            textBoxFile5.Text = state.SavePathToFileF5;
            numericUpDown1.Value = state.Interval;
            numericUpDown2.Value = state.MinDimension;
            checkBoxShowFileName.Checked = state.ShowFileName;
            checkBoxShowDate.Checked = state.ShowDate;
            checkBoxShowTime.Checked = state.ShowTime;
            checkBoxShowExif.Checked = state.ShowExif;
            checkBoxShowTextBackground.Checked = state.ShowTextBackground;
            checkBoxRotateByExif.Checked = state.RotateByExif;

            switch (state.RunInMode)
            {
                case 1:
                    radioButtonStartInNormalModel.Checked = true;
                    break;
                case 2:
                    radioButtonStartInGTF.Checked = true;
                    break;
                default:
                    radioButtonStartInLastMode.Checked = true;
                    break;
            }            

            checkBoxGTFRandom.Checked = state.GTFRandomNextFolder;
            checkBoxInterpolation.Checked = state.BestInterpolation;
            checkBoxCheck4Updates.Checked = state.Check4Updates;
            checkBoxExitOnlyWithEscape.Checked = state.ExitOnlyWithEscape;
            checkBoxSmoothHide.Checked = state.SmoothHidingEnabled;
            cbxFontSize.SelectedIndex = cbxFontSize.Items.IndexOf(Convert.ToString(state.FontSize));
            btnColor.BackColor = state.BackgroundColor;
            linkLabel1.Text = state.Url;
        }

        private void brnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
            System.Diagnostics.Process.Start(state.Url);
            Application.Exit();
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            colorDialog1 = new ColorDialog();
            colorDialog1.Color = Color.Black;
            if (colorDialog1.ShowDialog().Equals(DialogResult.OK))
                btnColor.BackColor = colorDialog1.Color;

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!"".Equals(textBoxCopyToFolder.Text) && Directory.Exists(textBoxCopyToFolder.Text))
            {
                List<string> imageRootFolders = new List<string>();                
                foreach (string item in lstImageFolders.Items)
                    imageRootFolders.Add(item);
                state.ImagesRootFolders = imageRootFolders;

                state.FileWithImagePaths = textBoxFile.Text;

                List<string> dontShowFolders = new List<string>();
                foreach (string item in listBoxNoImageFolders.Items)
                    dontShowFolders.Add(item);
                state.DontShowFolders = dontShowFolders;

                List<string> dontShowImages = new List<string>();
                foreach (string item in listBoxNoImages.Items)
                    dontShowImages.Add(item);
                state.DontShowImages = dontShowImages;

                state.SavePathToFolder = textBoxSavePathToFolder.Text;
                state.SavePathToFileF1 = textBoxFile1.Text;
                state.SavePathToFileF2 = textBoxFile2.Text;
                state.SavePathToFileF3 = textBoxFile3.Text;
                state.SavePathToFileF4 = textBoxFile4.Text;
                state.SavePathToFileF5 = textBoxFile5.Text;

                if (radioButtonStartInNormalModel.Checked)
                    state.RunInMode = 1;
                else if (radioButtonStartInGTF.Checked)
                    state.RunInMode = 2;
                else
                    state.RunInMode = 0;
                state.SaveRunInMode();

                state.CopyToFolder = textBoxCopyToFolder.Text;
                state.Interval = (int)numericUpDown1.Value;
                state.MinDimension = (int) numericUpDown2.Value;
                state.ShowFileName = checkBoxShowFileName.Checked;
                state.SaveShowFileName();
                state.ShowDate = checkBoxShowDate.Checked;
                state.SaveShowDate();
                state.ShowTime = checkBoxShowTime.Checked;
                state.SaveShowTime();
                state.ShowExif = checkBoxShowExif.Checked;
                state.SaveShowExif();
                state.ShowTextBackground = checkBoxShowTextBackground.Checked;
                state.SaveShowTextBackground();
                state.ShowExif = checkBoxShowExif.Checked;
                state.SaveShowExif();
                state.RotateByExif = checkBoxRotateByExif.Checked;
                state.SaveRotateByExif();
                state.GTFRandomNextFolder = checkBoxGTFRandom.Checked;
                state.SaveGTFRandom();
                state.BestInterpolation = checkBoxInterpolation.Checked;
                state.SaveBestInterpolation();
                state.Check4Updates = checkBoxCheck4Updates.Checked;
                state.SaveCheck4Updates();
                state.ExitOnlyWithEscape = checkBoxExitOnlyWithEscape.Checked;
                state.SaveExitOnlyWithEscape();
                state.SmoothHidingEnabled = checkBoxSmoothHide.Checked;
                state.SaveSmoothHideOfImage();

                state.FontSize = Convert.ToInt32(((string)cbxFontSize.SelectedItem));
                state.BackgroundColor = btnColor.BackColor;

                Application.Exit();
            }
            else
            {
                MessageBox.Show("Set \"copy to\" folder", "\"Copy to\" folder missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnSelectFolder1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select the folder with your images";
            string selected = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if ((lstImageFolders.SelectedItem != null) && Directory.Exists((string)lstImageFolders.SelectedItem))
                selected = (string)lstImageFolders.SelectedItem;
            if ((textBoxImageFolder.Text.Length > 0) && Directory.Exists(textBoxImageFolder.Text))
                selected = textBoxImageFolder.Text;

            this.folderBrowserDialog1.SelectedPath = selected;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
                AddFolderToList(folderBrowserDialog1.SelectedPath, lstImageFolders);
        }

        private void btnSelectFolder2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select the folder where to copy images";
            this.folderBrowserDialog1.SelectedPath = state.CopyToFolder;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK) textBoxCopyToFolder.Text = folderBrowserDialog1.SelectedPath;
        }

        private void btnAddImagesFolder_Click(object sender, EventArgs e)
        {
            AddFolderToList(textBoxImageFolder.Text, lstImageFolders);
        }

        private void lstImageFolders_SelectedValueChanged(object sender, EventArgs e)
        {
            if (lstImageFolders.SelectedItem != null)
            {
                textBoxImageFolder.Text = (string)lstImageFolders.SelectedItem;
                btnRemoveImagesFolder.Enabled = true;
            }
            else
                btnRemoveImagesFolder.Enabled = false;
        }

        private void btnRemoveImagesFolder_Click(object sender, EventArgs e)
        {
            if (lstImageFolders.SelectedItem != null)
            {
                object selected = lstImageFolders.SelectedItem;
                lstImageFolders.Items.Remove(selected);
            }
        }

        private void buttonSelectFile1_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.CheckFileExists = true;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) textBoxFile.Text = openFileDialog1.FileName;
        }


        //záložka Exclude

        private void buttonSelectNoFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select the folder which you want to exclude";
            string selected = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if ((listBoxNoImageFolders.SelectedItem != null) && Directory.Exists((string)listBoxNoImageFolders.SelectedItem))
                selected = (string)listBoxNoImageFolders.SelectedItem;
            if ((textBoxNoImageFolders.Text.Length > 0) && Directory.Exists(textBoxNoImageFolders.Text))
                selected = textBoxNoImageFolders.Text;

            this.folderBrowserDialog1.SelectedPath = selected;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
                AddFolderToList(folderBrowserDialog1.SelectedPath, listBoxNoImageFolders);
        }

        private void buttonAddNoImageFolders_Click(object sender, EventArgs e)
        {
            AddFolderToList(textBoxNoImageFolders.Text, listBoxNoImageFolders);
        }

        private void listBoxNoImageFolders_SelectedValueChanged(object sender, EventArgs e)
        {
            if (listBoxNoImageFolders.SelectedItem != null)
            {
                textBoxNoImageFolders.Text = (string)listBoxNoImageFolders.SelectedItem;
                buttonRemoveNo.Enabled = true;
            }
            else
                buttonRemoveNo.Enabled = false;
        }

        private void buttonRemoveNo_Click(object sender, EventArgs e)
        {
            if (listBoxNoImageFolders.SelectedItem != null)
            {
                object selected = listBoxNoImageFolders.SelectedItem;
                listBoxNoImageFolders.Items.Remove(selected);
            }
        }

        private void buttonSelectNoImages_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select image which you want to exclude";
            openFileDialog1.CheckFileExists = true;

            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string p = openFileDialog1.FileName;
                if (!listBoxNoImages.Items.Contains(p))
                {
                    int index = listBoxNoImages.Items.Add(p);
                    listBoxNoImages.SelectedIndex = index;
                }
            }
        }

        private void buttonAddToListNoImages_Click(object sender, EventArgs e)
        {
            string p = textBoxNoImages.Text;
            if (!"".Equals(p) && File.Exists(p))
            {
                if (!listBoxNoImages.Items.Contains(p))
                {
                    int index = listBoxNoImages.Items.Add(p);
                    listBoxNoImages.SelectedIndex = index;
                }
            }
            else
            {
                MessageBox.Show("Select or type existing file path", "File path missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void listBoxNoImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxNoImages.SelectedItem != null)
            {
                textBoxNoImages.Text = (string)listBoxNoImages.SelectedItem;
                buttonRemoveNoImages.Enabled = true;
            }
            else
                buttonRemoveNoImages.Enabled = false;
        }

        private void buttonRemoveNoImages_Click(object sender, EventArgs e)
        {
            if (listBoxNoImages.SelectedItem != null)
            {
                object selected = listBoxNoImages.SelectedItem;
                listBoxNoImages.Items.Remove(selected);
            }
        }

        //Záložka Save Path


        private void btnSelectFolder3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select the folder for files";
            this.folderBrowserDialog1.SelectedPath = state.SavePathToFolder;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK) textBoxSavePathToFolder.Text = folderBrowserDialog1.SelectedPath;
        }


        //adds folder to listbox
        private void AddFolderToList(string path, ListBox listBox)
        {
            if (!"".Equals(path) && Directory.Exists(path))
            {
                if (!listBox.Items.Contains(path))
                {
                    int index = listBox.Items.Add(path);
                    listBox.SelectedIndex = index;
                }
            }
            else
            {
                MessageBox.Show("Select or type existing folder please", "Folder missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
