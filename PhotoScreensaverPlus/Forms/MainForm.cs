using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using NLog;
using PhotoScreensaverPlus.State;
using PhotoScreensaverPlus.Draw;

namespace PhotoScreensaverPlus.Forms
{
    public partial class MainForm : Form
    {

        #region Preview API's

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        #endregion

        private MainController mainCl;

        public ImageCache ImageCache = new ImageCache(5);

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Constructors

        public MainForm(MainController mainCl)
        {
            InitializeComponent();
            this.mainCl = mainCl;
            //hide the cursor
            Cursor.Hide();
        }

        //This constructor is the handle to the select screensaver dialog preview window
        //It is used when in preview mode (/p)
        public MainForm(IntPtr PreviewHandle, MainController mainCl)
        {
            try
            {
                InitializeComponent();

                this.mainCl = mainCl;

                //set the preview window as the parent of this window
                SetParent(this.Handle, PreviewHandle);

                //make this a child window, so when the select screensaver dialog closes, this will also close
                SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));

                //set our window's size to the size of our window's new parent

                Rectangle ParentRect;
                GetClientRect(PreviewHandle, out ParentRect);
                this.Size = ParentRect.Size;

                //set our location at (0, 0)
                this.Location = new Point(0, 0);

                this.mainCl.AppState.IsPreviewMode = true;
            }
            catch (Exception e)
            {
                logger.Debug(e);
            }
        }

        #endregion

        #region UI logic

        //calculates rectangle (image position and size) from image width and height
        public Rectangle getRectangleForImageInForm(int imageWidth, int imageHeight)
        {
            int x = 0, y = 0, width = imageWidth, height = imageHeight;

            if (imageHeight >= imageWidth)
            {
                if (imageHeight > this.Height)
                {
                    height = this.Height;
                    width = (int)((float)height / ((float)imageHeight / (float)imageWidth));
                    //check width because of widescreen on portrait orientation
                    if (width > this.Width)
                    {
                        width = this.Width;
                        height = (int)((float)width * ((float)imageHeight / (float)imageWidth));
                    }
                }
            }
            else
            {
                if (imageWidth > this.Width)
                {
                    width = this.Width;
                    height = (int)((float)width / ((float)imageWidth / (float)imageHeight));
                    //check height because of widescreen
                    if (height > this.Height)
                    {
                        height = this.Height;
                        width = (int)((float)height * ((float)imageWidth / (float)imageHeight));
                    }
                }
            }

            x = (this.Width - width) / 2;
            y = (this.Height - height) / 2;

            return new Rectangle(x, y, width, height);
        }

        #endregion

        #region User Input

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!mainCl.AppState.IsPreviewMode) //disable exit functions for preview
            {
                if (e.KeyCode == Keys.N) //show name
                {
                    mainCl.ShowName();
                }
                else if (e.KeyCode == Keys.D) //show date
                {
                    mainCl.ShowDate();
                }
                else if (e.KeyCode == Keys.E) //show exif
                {
                    mainCl.ShowExif();
                }
                else if (e.KeyCode == Keys.T)
                {
                    mainCl.ShowTime();
                }
                else if (e.KeyCode == Keys.R)
                {
                    mainCl.RotateRight();
                }
                else if (e.KeyCode == Keys.L)
                {
                    mainCl.RotateLeft();
                }
                else if (e.KeyCode == Keys.M)
                {
                    mainCl.EditMetadata();
                }
                else if (e.KeyCode == Keys.C) //copy file to selected folder
                {
                    mainCl.Copy();
                }
                else if (e.Control && e.KeyCode == Keys.F) //force go through folder mode - have to be before "F" key (due to "alt)
                {
                    mainCl.GoThroughFolderON(false);
                }
                else if (e.Alt && e.KeyCode == Keys.F) //start GTF with start based on current image
                {
                    if (mainCl.AppState.GoThroughFolder == false)
                        mainCl.GoThroughFolderON(true);
                    else
                        mainCl.GoThroughFolderOFF();
                }
                else if (e.KeyCode == Keys.F)
                {
                    if (mainCl.AppState.GoThroughFolder == false)
                        mainCl.GoThroughFolderON(false);
                    else
                        mainCl.GoThroughFolderOFF();
                }
                else if (e.KeyCode == Keys.W)
                {
                    mainCl.VisitWeb();
                }
                else if (e.KeyCode == Keys.H)
                {
                    mainCl.ShowHelp();
                }
                else if (e.KeyCode == Keys.S)
                {
                    mainCl.ShowSettings();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    mainCl.Next();
                }
                else if (e.KeyCode == Keys.Left)
                {
                    mainCl.Previous();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    mainCl.Pause();
                }
                else if (e.KeyCode == Keys.Delete) //delete file
                {
                    mainCl.Delete();
                }
                else if (e.Alt && e.KeyCode == Keys.X)
                {
                    mainCl.ExcludeCurrentImageFolder();
                }
                else if (e.KeyCode == Keys.X)
                {
                    mainCl.ExcludeCurrentImage();
                }
                else if (e.KeyCode == Keys.Up)
                {
                    mainCl.SpeedUp();
                }
                else if (e.KeyCode == Keys.Down)
                {
                    mainCl.SpeedDown();
                }
                else if (e.Alt && e.KeyCode == Keys.F1)
                {
                    //mainCl.BuildImageListFromFile(AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF1 + AppState.SAVE_PATH_FILES_EXTENSION);
                    //mainCl.timer.Stop();
                    //AppState.SmoothHideCurrentImage = true; //zobrazení příštího obrázku bude s postupným skrytím předchozího
                    //mainCl.DrawNext();
                    //mainCl.timer.Start();
                }
                else if (e.KeyCode == Keys.F1)
                {
                    mainCl.SaveFileName(mainCl.AppState.SavePathToFileF1);
                }
                else if (e.KeyCode == Keys.F2)
                {
                    mainCl.SaveFileName(mainCl.AppState.SavePathToFileF2);
                }
                else if (e.KeyCode == Keys.F3)
                {
                    mainCl.SaveFileName(mainCl.AppState.SavePathToFileF3);
                }
                else if (e.KeyCode == Keys.F4)
                {
                    mainCl.SaveFileName(mainCl.AppState.SavePathToFileF4);
                }
                else if (e.KeyCode == Keys.F5)
                {
                    mainCl.SaveFileName(mainCl.AppState.SavePathToFileF5);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    mainCl.ApplicationExit();
                }
                else if (e.Alt && e.KeyCode == Keys.U)
                {
                    //MessageBox.Show("U pressed Alt+U");
                }
                else if (!e.Alt)
                    if (!mainCl.AppState.ExitOnlyWithEscape)
                        mainCl.ApplicationExit();
            }
        }

        private void MainForm_Click(object sender, EventArgs e)
        {
            if (!mainCl.AppState.IsPreviewMode) //disable exit functions for preview
                if (!mainCl.AppState.ExitOnlyWithEscape)
                    mainCl.ApplicationExit();
        }

        //start off OriginalLoction with an X and Y of int.MaxValue, because
        //it is impossible for the cursor to be at that position. That way, we
        //know if this variable has been set yet.
        Point OriginalLocation = new Point(int.MaxValue, int.MaxValue);

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mainCl.AppState.IsPreviewMode) //disable exit functions for preview 
            {
                //see if original location has been set
                if (OriginalLocation.X == int.MaxValue & OriginalLocation.Y == int.MaxValue)
                {
                    OriginalLocation = e.Location;
                }
                //see if the mouse has moved more than 20 pixels in any direction. If it has, close the application.
                if (Math.Abs(e.X - OriginalLocation.X) > 20 | Math.Abs(e.Y - OriginalLocation.Y) > 20)
                {
                    if (!mainCl.AppState.ExitOnlyWithEscape)
                        mainCl.ApplicationExit();
                    else
                        Cursor.Show();
                }
            }
        }
        #endregion

        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                if (this.mainCl.AppState.IsPreviewMode)
                {
                    var previewBitmap = mainCl.BtmGen.GeneratePreviewBitmap(this.Size);
                    var g = CreateGraphics();
                    g.DrawImage(previewBitmap, 0, 0);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
        }
    }
}
