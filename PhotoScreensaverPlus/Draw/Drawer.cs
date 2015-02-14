using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Collections;
using PhotoScreensaverPlus.State;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus.Draw
{
    class Drawer
    {
        private ApplicationState state;
        private BitmapGenerator bitmg;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public List<ScreenDefinition> ScreenDefinitions = new List<ScreenDefinition>();

        public Drawer(ApplicationState state, BitmapGenerator generator)
        {
            this.state = state;
            this.bitmg = generator;
        }

        /// <summary>
        /// Draws image composition with background image from file 
        /// and other foreground information, based on AppState of screensaver
        /// </summary>
        /// <param name="imageFile">image file info</param>
        /// <param name="suppressImageCache">true to draw image from file</param>
        public void DrawComposition(FileInfo imageFile, Boolean suppressImageCache)
        {
            logger.Debug("Draw image file " + imageFile.Name);
            Bitmap loadedBitmap = null;
            IDictionary<String, String> exifDictionary = new Dictionary<String, String>();

            Graphics bufferedBmpGraphics = null; //used to doublebuffering technique
            Bitmap bufferedBmp = null; //used to doublebuffering technique

            Graphics doubleBufferedBmpGraphics = null; //double buffer
            Bitmap doubleBufferedBmp = null;    //double buffer graphics

            ImageCacheEntry entry = null;

            //logger.Debug(imageFile.FullName);
            try
            {
                state.currentImageFileInfo = imageFile; //global variable    

                //odstran obrazek z cache, pokud nema byt pouzita a pritom se tam nachazi (je potreba 
                //napr. pri otaceni
                if (suppressImageCache)
                    foreach (ScreenDefinition sd in ScreenDefinitions)
                    {
                        sd.ScreenForm.ImageCache.RemoveAll(c => c.FullName.Equals(imageFile.FullName));
                    }

                SmoothHideOfCureentImage();

                //postupně pro jednotlivé obrazovky
                foreach (ScreenDefinition sd in ScreenDefinitions)
                {
                    //get bitmap and image from cache
                    entry = sd.ScreenForm.ImageCache.Get(imageFile.FullName);

                    if (null != entry)
                    {
                        bufferedBmp = entry.InterpolatedBitmap;
                        bufferedBmpGraphics = Graphics.FromImage(bufferedBmp);
                        exifDictionary = entry.ExifDictionary;
                    }
                    else
                    {
                        //load bitmap from image file
                        if (loadedBitmap == null) //if there is not loaded image (from previous screendefinition)
                        {
                            loadedBitmap = new Bitmap(imageFile.FullName);
                            exifDictionary = ExifSupport.GetExifDictionary(loadedBitmap);
                            if (state.RotateByExif)
                                CheckExifOrientation(loadedBitmap, exifDictionary);
                        }

                        //create buffered bitmap
                        bufferedBmp = new Bitmap(sd.ScreenForm.Width, sd.ScreenForm.Height, sd.FormGraphics);
                        bufferedBmpGraphics = Graphics.FromImage(bufferedBmp);

                        //fill image with background color - it is faster then clear graphics
                        bufferedBmpGraphics.FillRectangle(new SolidBrush(state.BackgroundColor), 0, 0, bufferedBmp.Width, bufferedBmp.Height);

                        if (state.BestInterpolation)
                            bufferedBmpGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        else
                            bufferedBmpGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;

                        //draw original image to buffered bitmap
                        bufferedBmpGraphics.DrawImage(loadedBitmap, sd.ScreenForm.getRectangleForImageInForm(loadedBitmap.Width, loadedBitmap.Height));

                        sd.ScreenForm.ImageCache.Add(new ImageCacheEntry(imageFile.FullName, bufferedBmp, exifDictionary));
                    }

                    //doublebuffering
                    //vytvořím si druhou bitmapu pro vykreslení kompozice - obrázku do pozadí a k tomu popředí - tj. exif a pod.
                    doubleBufferedBmp = new Bitmap(bufferedBmp.Width, bufferedBmp.Height, bufferedBmpGraphics);
                    doubleBufferedBmpGraphics = Graphics.FromImage(doubleBufferedBmp);

                    //draw scaled image to the background of doublebuffered bitmap
                    doubleBufferedBmpGraphics.DrawImage(bufferedBmp, 0, 0);

                    //draw exif to doublebuffered bitmap
                    if (state.ShowExif)
                        DrawExif(exifDictionary, doubleBufferedBmpGraphics);
                    if (state.GoThroughFolder)
                        DrawGTFInfo(doubleBufferedBmpGraphics, sd.ScreenForm.Width, sd.ScreenForm.Height);
                    if (state.ShowDate)
                        DrawDate(exifDictionary, doubleBufferedBmpGraphics, sd.ScreenForm.Width);
                    if (state.ShowFileName)
                        DrawFileName(doubleBufferedBmpGraphics, sd.ScreenForm.Height);
                    if (state.ShowTime)
                        DrawTime(doubleBufferedBmpGraphics, sd.ScreenForm.Width, sd.ScreenForm.Height);

                    //draw composition to the form graphics
                    sd.FormGraphics.DrawImage(doubleBufferedBmp, 0, 0);
                }
            }
            catch (Exception e)
            {
                logger.Fatal("Can't draw image file '" + imageFile.FullName + "'", e);
                //WindowsLogWriter.WriteLog("DrawComposition - Can't draw image file '" + imageFile.FullName + "', exception = " + e.Message, EventLogEntryType.Error);
                DrawImageToAllForms(bitmg.GenerateErrorBitmap("Error while draw image '" + imageFile.FullName + "'"));
            }
            finally
            {
                if (loadedBitmap != null)
                    loadedBitmap.Dispose();

                //toto tady nemůže být, protože tím zruším bitmapu, která je v cache
                //if (bufferedBmp != null)
                //bufferedBmp.Dispose();

                if (bufferedBmpGraphics != null)
                    bufferedBmpGraphics.Dispose();

                if (doubleBufferedBmpGraphics != null)
                    doubleBufferedBmpGraphics.Dispose();

                if (doubleBufferedBmp != null)
                    doubleBufferedBmp.Dispose();
            }
        }

        //draws image to all the forms
        public void DrawImageToAllForms(Image img)
        {
            if (img != null)
            {
                try
                {
                    foreach (ScreenDefinition sd in ScreenDefinitions)
                    {
                        if (state.BestInterpolation)
                            sd.FormGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        else
                            sd.FormGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;

                        sd.FormGraphics.Clear(state.BackgroundColor);
                        sd.FormGraphics.DrawImage(img, sd.ScreenForm.getRectangleForImageInForm(img.Width, img.Height));
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal("Error while drawing image", ex);
                    //WindowsLogWriter.WriteLog("Error while drawing image: " + ex.Message, EventLogEntryType.Error);
                }
            }

        }

        /// <summary>
        /// Draws exif informations
        /// </summary>
        /// <param name="exifDictionary">Dictionary with exif</param>
        /// <param name="graphics">where to draw exif</param>
        private void DrawExif(IDictionary<String, String> exifDictionary, Graphics graphics)
        {
            if (null != exifDictionary)
            {
                try
                {
                    string text = "";

                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_TITLE))
                    {
                        String ImageTitle = exifDictionary[ExifSupport.IMAGE_TITLE];
                        if (null != ImageTitle && ImageTitle.Trim().Length > 0)
                            text = text + "Title: " + ImageTitle + "\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_DESCRIPTION))
                    {
                        String ImageDescription = exifDictionary[ExifSupport.IMAGE_DESCRIPTION];
                        if (null != ImageDescription && ImageDescription.Trim().Length > 0)
                            text = text + "Description: " + ImageDescription + "\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_COMMENT))
                    {
                        String ImageUserComment = exifDictionary[ExifSupport.IMAGE_COMMENT];
                        if (null != ImageUserComment && ImageUserComment.Trim().Length > 0)
                            text = text + "User comment: " + ImageUserComment + "\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_DATE) && exifDictionary.ContainsKey(ExifSupport.IMAGE_TIME))
                    {
                        String ImageDateTime = exifDictionary[ExifSupport.IMAGE_DATE] + " " + exifDictionary[ExifSupport.IMAGE_TIME];
                        if (null != ImageDateTime && ImageDateTime.Trim().Length > 0)
                            text = text + "Created: " + ImageDateTime + "\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_CAMERA_MODEL))
                    {
                        String CameraModel = exifDictionary[ExifSupport.IMAGE_CAMERA_MODEL];
                        if (null != CameraModel && CameraModel.Trim().Length > 0)
                            text = text + "Camera: " + CameraModel + "\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_EXPOSURE_TIME))
                    {
                        String ExposureTime = exifDictionary[ExifSupport.IMAGE_EXPOSURE_TIME];
                        if (null != ExposureTime && ExposureTime.Trim().Length > 0)
                            text = text + "Exposure time: " + ExposureTime + "sec.\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_LENS_APERTURE))
                    {
                        String LensAperture = exifDictionary[ExifSupport.IMAGE_LENS_APERTURE];
                        if (null != LensAperture && LensAperture.Trim().Length > 0)
                            text = text + "Lens aperture: " + LensAperture + "f\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_FOCAL_LENGHT))
                    {
                        String FocalLenght = exifDictionary[ExifSupport.IMAGE_FOCAL_LENGHT];
                        if (null != FocalLenght && FocalLenght.Trim().Length > 0)
                            text = text + "Focal lenght: " + FocalLenght + "mm\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_ISO_SPEED))
                    {
                        String ISOSpeed = exifDictionary[ExifSupport.IMAGE_ISO_SPEED];
                        if (null != ISOSpeed && ISOSpeed.Trim().Length > 0)
                            text = text + "ISO speed: " + ISOSpeed + " ASA\n\r";
                    }
                    if (exifDictionary.ContainsKey(ExifSupport.IMAGE_FLASH_FIRED))
                    {
                        String FlashFired = exifDictionary[ExifSupport.IMAGE_FLASH_FIRED];
                        if (null != FlashFired && FlashFired.Trim().Length > 0)
                            text = text + FlashFired;
                    }
                    
                    if ((text.Length > 0) && (text.Substring(text.Length - 2).Equals("\n\r"))) //odstran prebytecny enter
                        text = text.Remove(text.Length - 2);

                    DrawText(text, 0, new Point(10, 10), StringAlignment.Near, StringAlignment.Near, graphics);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't draw exif of the file: '" + state.currentImageFileInfo.FullName + "'", e);
                    //WindowsLogWriter.WriteLog("DrawExif - Can't draw exif of the file: '" + AppState.currentImageFileInfo.FullName + "', exception = " + e.Message, EventLogEntryType.Error);
                    DrawImageToAllForms(bitmg.GenerateErrorBitmap("Can't draw exif of the file: '" + state.currentImageFileInfo.FullName + "'"));
                }
            }
        }

        /// <summary>
        /// Draws date
        /// </summary>
        /// <param name="exifDictionary">Dictionary with exif</param>
        /// <param name="graphics">Graphics of the bitmap</param>
        /// <param name="width">Width of the bitmap</param>
        private void DrawDate(IDictionary<String, String> exifDictionary, Graphics graphics, int width)
        {
            if (null != state.currentImageFileInfo)
            {
                try
                {
                    string ImageDate = exifDictionary[ExifSupport.IMAGE_DATE];

                    if (null == ImageDate || ImageDate.Length == 0)
                        ImageDate = state.currentImageFileInfo.LastWriteTime.ToShortDateString();

                    if (ImageDate.Length > 0)
                        DrawText(ImageDate, state.FontSize + 14, new Point(width - 10, 10), StringAlignment.Far, StringAlignment.Near, graphics);

                    string ImageTime = exifDictionary[ExifSupport.IMAGE_TIME];
                    if (null != ImageTime || ImageTime.Length != 0)
                        DrawText(ImageTime, state.FontSize + 5, new Point(width - 10, 56), StringAlignment.Far, StringAlignment.Near, graphics);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't draw date of the file: '" + state.currentImageFileInfo.FullName + "'", e);
                    //WindowsLogWriter.WriteLog("DrawDate - Can't draw date of the file: '" + AppState.currentImageFileInfo.FullName + "', exception = " + e.Message, EventLogEntryType.Error);
                    DrawImageToAllForms(bitmg.GenerateErrorBitmap("Can't draw date of the file: '" + state.currentImageFileInfo.FullName + "'"));
                }
            }
        }

        /// <summary>
        /// Draws file name
        /// </summary>
        /// <param name="graphics">Graphics of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        private void DrawFileName(Graphics graphics, int height)
        {
            if (null != state.currentImageFileInfo)
            {
                try
                {
                    DrawText(state.currentImageFileInfo.FullName, 0, new Point(10, height - 5), StringAlignment.Near, StringAlignment.Far, graphics);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't draw file name: '" + state.currentImageFileInfo.FullName + "'", e);
                    //WindowsLogWriter.WriteLog("DrawFileName - Can't draw file name: '" + AppState.currentImageFileInfo.FullName + "', exception = " + e.Message, EventLogEntryType.Error);
                    DrawImageToAllForms(bitmg.GenerateErrorBitmap("Can't draw file name of the file '" + state.currentImageFileInfo.FullName + "'"));
                }
            }
        }

        /// <summary>
        /// Draws time
        /// </summary>
        /// <param name="graphics">Graphics of the bitmap</param>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        private void DrawTime(Graphics graphics, int width, int height)
        {
            if (null != state.currentImageFileInfo)
            {
                try
                {
                    DrawText(System.DateTime.Now.ToShortTimeString(), state.FontSize + 5, new Point(width - 10, height - 30), StringAlignment.Far, StringAlignment.Far, graphics);
                    DrawText(state.ImageNo + " images - " + getRunnigTime(), 0, new Point(width - 10, height - 5), StringAlignment.Far, StringAlignment.Far, graphics);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't draw current time", e);
                    //WindowsLogWriter.WriteLog("DrawTime - Can't draw current time, exception = " + e.Message, EventLogEntryType.Error);
                }
            }
        }

        /// <summary>
        /// Draws info of "go through folder"
        /// </summary>
        /// <param name="graphics">Graphics of the bitmap</param>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        private void DrawGTFInfo(Graphics graphics, int width, int height)
        {
            if (null != state.folderImagesFileInfoList && state.FolderImagesCount > 0)
            {
                try
                {
                    int remains = state.folderImagesFileInfoList.Count;
                    int indexOfCurrent = 0;
                    bool showIt = true;
                    if ((state.shownFileInfoList.Count - 1) != state.shownFileInfoListIndex) //pokud se vracime do historie, je nutne snizit i zobrazovany index fotky
                    {
                        indexOfCurrent = (state.FolderImagesCount - remains) - ((state.shownFileInfoList.Count - 1) - state.shownFileInfoListIndex);
                        if (indexOfCurrent < 1) //jsme v historii tak hluboko, ze jsme mimo fotky z procházeného adresáře
                            showIt = false;
                    }
                    else
                        indexOfCurrent = state.FolderImagesCount - remains;
                    if (showIt)
                        DrawText(indexOfCurrent + " / " + state.FolderImagesCount, 0, new Point(width / 2, height - 5), StringAlignment.Center, StringAlignment.Far, graphics);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't draw info for 'go through folder'", e);
                    //WindowsLogWriter.WriteLog("DrawGTFInfo - Can't draw info for 'go through folder', exception = " + e.Message, EventLogEntryType.Error);
                }
            }
        }

        /// <summary>
        /// Draws semitransparent text
        /// </summary>
        /// <param name="text">Text to draw</param>
        /// <param name="fontSize">Size of font, 0 - take size from AppState.FontSize</param>
        /// <param name="point">Position of the text on the screen</param>
        /// <param name="verticalAlignment">Vertical alignment of the text</param>
        /// <param name="horizontalAlignment">Horiznotal alignment of the text</param>
        /// <param name="graphics">Graphics object for drawing</param>
        private void DrawText(string text, int fontSize, PointF point, StringAlignment verticalAlignment, StringAlignment horizontalAlignment, Graphics graphics)
        {
            //graphics.DrawString(text, new Font("Arial", 10, FontStyle.Regular), Brushes.Lime, new PointF(5, 5));
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (fontSize == 0)
                fontSize = state.FontSize;
            Font font = new Font("Arial", fontSize, FontStyle.Regular);

            StringFormat format = new StringFormat();
            format.Alignment = verticalAlignment;
            format.LineAlignment = horizontalAlignment;
            format.Trimming = StringTrimming.EllipsisCharacter;

            //draw gray rectangle
            if (state.ShowTextBackground)
            {
                SizeF textSize = graphics.MeasureString(text, font);
                int x = (int)point.X - 2;
                int y = (int)point.Y - 2;
                if (verticalAlignment == StringAlignment.Far)
                    x = ((x - (int)textSize.Width));
                if (verticalAlignment == StringAlignment.Center)
                    x = (x - (int)(textSize.Width / 2));
                if (horizontalAlignment == StringAlignment.Far)
                    y = ((y - (int)textSize.Height) - 2);

                graphics.FillRectangle(new SolidBrush(Color.FromArgb(120, 50, 50, 50)), x, y, textSize.Width + 4, textSize.Height + 4);
            }

            SolidBrush semiTransBrush2 = new SolidBrush(Color.FromArgb(150, 0, 0, 0));

            graphics.DrawString(text, font, semiTransBrush2, new PointF(point.X + 2, point.Y + 2), format);

            SolidBrush semiTransBrush = new SolidBrush(
                         Color.FromArgb(200, 255, 255, 255));

            graphics.DrawString(text, font, semiTransBrush, point, format);
        }

        /// <summary>
        /// Draws shadow overlay over all of forms
        /// </summary>
        /// <param name="alpha">alpha value (for example 120)</param>
        public void DrawOverlay(int alpha)
        {
            foreach (ScreenDefinition sd in ScreenDefinitions)
            {
                sd.FormGraphics.FillRectangle(new SolidBrush(Color.FromArgb(alpha, state.BackgroundColor.R, state.BackgroundColor.G, state.BackgroundColor.B)), 0, 0, sd.ScreenForm.Width, sd.ScreenForm.Height);
            }
        }

        /// <summary>
        /// Postupné ztmavení předešlého obrázku (pouze pokud přecházíme na další obrázek pomocí timeru)
        /// dělá se pomocí opakovaného překreslení formuláře obdélníkem s barvou pozadí a předanou úrovní alpha
        /// existuje i moznost transformace ColorMatrix pro zobrazeny obrázek, kde bychom mohli změnit alpha, ale
        /// musi se take znovu vykreslit, tak nevim jestli by to bylo plynulejší
        /// </summary>
        private void SmoothHideOfCureentImage()
        {
            if (state.SmoothHidingEnabled && (state.SmoothHideCurrentImage))
            {
                int alpha = 80;
                Brush brush = new SolidBrush(Color.FromArgb(alpha, state.BackgroundColor.R, state.BackgroundColor.G, state.BackgroundColor.B));
                for (int i = 0; i < 13; i++)
                {
                    foreach (ScreenDefinition sd in ScreenDefinitions)
                    {
                        sd.FormGraphics.FillRectangle(brush, 0, 0, sd.ScreenForm.Width, sd.ScreenForm.Height);
                    }
                }
                /*
                Brush brush = new SolidBrush(Color.FromArgb(255, AppState.BackgroundColor.R, AppState.BackgroundColor.G, AppState.BackgroundColor.B));
                foreach (ScreenDefinition sd in ScreenDefinitions)
                {
                    int squareHeight = (int)(sd.ScreenForm.Height / 50);
                    for (int i = 0 ; i < sd.ScreenForm.Height ; i = i + squareHeight)
                    {
                        sd.FormGraphics.FillRectangle(brush, 0, i, sd.ScreenForm.Width, squareHeight);
                        Thread.Sleep(10);
                    }

                }
                */
            }
            state.SmoothHideCurrentImage = false;  //vyresetujeme postupné skrytí pro příští zobrazení
        }

        /// <summary>
        /// Prepares string with running time
        /// </summary>
        /// <returns>Runnig time of the screensaver</returns>
        private String getRunnigTime()
        {
            TimeSpan running = System.DateTime.Now - state.startTime;
            String runningString = "";

            if (running.Hours > 1)
                runningString = running.Hours + "hours ";
            else if (running.Hours > 0)
                runningString = running.Hours + "hour ";
            if (running.Minutes == 0 && running.Hours > 0)
                runningString = runningString + "00min ";
            else if (running.Minutes > 0)
                if (running.Minutes < 10 && running.Hours > 0)
                    runningString = runningString + "0" + running.Minutes + "min ";
                else
                    runningString = runningString + running.Minutes + "min ";
            if (running.Seconds > 0)
                if (running.Seconds < 10 && running.Minutes > 0)
                    runningString = runningString + "0" + running.Seconds + "sec";
                else
                    runningString = runningString + running.Seconds + "sec";
            else
                runningString = runningString + "00sec";
            return runningString;
        }

        //rotates image base on exif
        private void CheckExifOrientation(Bitmap bmp, IDictionary<String, String> exifDictionary)
        {
            try
            {
                short rotation = Convert.ToInt16(exifDictionary[ExifSupport.IMAGE_ROTATION]);
                if (rotation == 3)
                    bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                else if (rotation == 6)
                    bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                else if (rotation == 8)
                    bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }
            catch (Exception ex)
            {
                logger.Fatal("Can't rotate image", ex);
                //WindowsLogWriter.WriteLog("CheckExifOrientation - Can't rotate image, exception = " + ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
