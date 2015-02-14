using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using PhotoScreensaverPlus.State;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus.Draw
{
    public class BitmapGenerator
    {
        private static BitmapGenerator instance = null;

        private ApplicationState state;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private BitmapGenerator(ApplicationState state)
        {
            this.state = state;
        }

        public static BitmapGenerator getInstance(ApplicationState state)
        {
            if (instance == null)
                instance = new BitmapGenerator(state);
            return instance;
        }

        //generates a Welcome bitmap
        public Bitmap GenerateWelcomeBitmap()
        {
            Bitmap Welcome = null;
            try
            {
                Welcome = new Bitmap(640, 480);
                Graphics WelcomeGraphics = Graphics.FromImage(Welcome);

                WelcomeGraphics.FillRectangle(new SolidBrush(state.BackgroundColor), new Rectangle(0, 0, 640, 480));
                string WelcomeText = ApplicationState.APP_NAME_WITH_VERSION + "\r\n\r\n" + state.Url;
                string helpText = "PRESS H FOR HELP";
                string waitString = "... directory scanning";
                WelcomeGraphics.DrawString(WelcomeText, new Font("Lucida Console", 13, FontStyle.Regular), Brushes.White, new PointF(10, 20));
                WelcomeGraphics.DrawString(helpText, new Font("Lucida Console", 13, FontStyle.Regular), Brushes.White, new PointF(10, 110));
                WelcomeGraphics.DrawString(waitString, new Font("Lucida Console", 10, FontStyle.Regular), Brushes.White, new PointF(440, 450));
                WelcomeGraphics.Dispose();
            }
            catch (Exception e)
            {
                logger.Fatal("Can't generate welcome bitmap", e);
                //WindowsLogWriter.WriteLog("GenerateWelcomeBitmap - Can't generate welcome bitmap: exception = " + e.Message, EventLogEntryType.Error);
                //drawer.DrawImageToAllForms(GenerateErrorBitmap("Error while generate welcome bitmap"));
            }
            return Welcome;
        }


        //generates a error bitmap
        public Bitmap GenerateErrorBitmap(string errorMessage)
        {
            Bitmap Error = null;
            try
            {
                Error = new Bitmap(640, 480);
                Graphics ErrorGraphics = Graphics.FromImage(Error);
                ErrorGraphics.FillRectangle(new SolidBrush(state.BackgroundColor), new Rectangle(0, 0, 640, 480));
                ErrorGraphics.DrawString(errorMessage, new Font("Lucida Console", 10, FontStyle.Regular), Brushes.White, new Rectangle(0, 0, 640, 480));
                ErrorGraphics.Dispose();
            }
            catch (Exception e)
            {
                //WindowsLogWriter.WriteLog("GenerateErrorBitmap - Can't generate error bitmap: exception = " + e.Message, EventLogEntryType.Error);
                logger.Fatal("Can't generate error bitmap", e);
            }
            return Error;
        }


        /// <summary>
        /// Generates bitmap for preview screen
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public Bitmap GeneratePreviewBitmap(Size size)
        {
            Bitmap preview = null;
            String text = Application.ProductName + "\n\r" + "version " + Application.ProductVersion + "\n\r\n\r" + "please visit" + "\n\r" + state.Url;
            try
            {
                var thisExe = Assembly.GetExecutingAssembly();
                var imageName = thisExe.GetName().Name + ".Resources.pssp_background.png";
                var file = thisExe.GetManifestResourceStream(imageName);

                if (file != null)
                {
                    var backgroundImg = Image.FromStream(file);

                    preview = new Bitmap(size.Width, size.Height);
                    Graphics previewGraphics;
                    using (previewGraphics = Graphics.FromImage(preview))
                    {
                        previewGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        previewGraphics.DrawImage(backgroundImg, 0, 0, preview.Width, preview.Height);
                        previewGraphics.DrawString(text, new Font("Lucida Console", 7, FontStyle.Bold), Brushes.Black, new Rectangle(5, 10, size.Width, size.Height));
                        previewGraphics.Dispose();
                    }
                }
                else
                    logger.Error("Background image '" + imageName + "' not found!");
            }
            catch (Exception e)
            {
                logger.Fatal("Can't generate welcome bitmap", e);
                //WindowsLogWriter.WriteLog("GeneratePreviewBitmap - Can't generate preview bitmap: exception = " + e.Message, EventLogEntryType.Error);
            }
            return preview;
        }
    }
}
