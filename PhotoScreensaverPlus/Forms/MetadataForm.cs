using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PhotoScreensaverPlus.Draw;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus
{
    public partial class MetadataForm : Form
    {
        FileInfo imgInfo;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private MetadataForm()
        {
            InitializeComponent();
        }

        public MetadataForm(FileInfo imgInfo)
        {
            InitializeComponent();
            this.Text = "Metadata for " + imgInfo.FullName;
            this.imgInfo = imgInfo;
            button1.DialogResult = DialogResult.OK;
            button2.DialogResult = DialogResult.Cancel;

            FileStream fileStream = null;
            try
            {
                fileStream = imgInfo.OpenRead();
                Image img = Image.FromStream(fileStream);

                //aktuální codepage uživatele
                int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
                Encoding encoding = Encoding.GetEncoding(codePage);

                string ImageTitle = ExifSupport.GetExifString((Bitmap)img, 0x0320, encoding);
                string ImageDescription = ExifSupport.GetExifString((Bitmap)img, 0x010E, encoding);
                string ImageUserComment = ExifSupport.GetImageUserComment((Bitmap)img);
                if (null != ImageUserComment && ImageUserComment.Length > 0 && ImageUserComment.Substring(0, 1).Contains("\0")) ImageUserComment = ""; //remove empty comment

                textBox1.Text = ImageTitle;
                textBox2.Text = ImageDescription;
                textBox3.Text = ImageUserComment;
            }
            catch (Exception e)
            {
                logger.Fatal("Can't open image file", e);
                //WindowsLogWriter.WriteLog("MetadataForm() - Can't open image file: " + e.Message, EventLogEntryType.Error);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ExifSupport.WriteNewMetadata(imgInfo, textBox1.Text, textBox2.Text, textBox3.Text);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
