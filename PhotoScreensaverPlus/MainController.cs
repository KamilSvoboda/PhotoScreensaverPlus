using NLog;
using PhotoScreensaverPlus.Draw;
using PhotoScreensaverPlus.FilesAndFolders;
using PhotoScreensaverPlus.Forms;
using PhotoScreensaverPlus.Logging;
using PhotoScreensaverPlus.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace PhotoScreensaverPlus
{
    /// <summary>
    /// Základní třída, která řídí celé chování screensaveru
    /// </summary>
    public class MainController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private ApplicationState _appState;
        public ApplicationState AppState { get { return _appState; }}

        private BitmapGenerator _btmGen;
        public BitmapGenerator BtmGen {get { return _btmGen; }}

        private Drawer drawer;
        private DirectoryHelper dirHelper;
        private Random random; //Random by se neměl inicializovat s každým požadavkem na nové náhodné číslo!

        public System.Windows.Forms.Timer timer;

        public MainController()
        {
            _appState = ApplicationState.getInstance();
            _btmGen = BitmapGenerator.getInstance(AppState);

            dirHelper = new DirectoryHelper(AppState);
            drawer = new Drawer(AppState, BtmGen);
            timer = new System.Windows.Forms.Timer();
            timer.Interval = AppState.Interval * 1000;
            timer.Tick += new EventHandler(t_Tick);
            random = new Random();
        }

        /// <summary>
        /// Spustí samotný screensaver
        /// </summary>
        public void Start()
        {
            //loops through all the computer's screens (monitors)
            foreach (Screen screen in Screen.AllScreens)
            {
                //creates a form just for that screen and passes it the bounds of that screen
                MainForm screensaverForm = new MainForm(this);
                screensaverForm.Text = Application.ProductName + " " + Application.ProductVersion;
                if (screen.Primary)
                    screensaverForm.Text = screensaverForm.Text + " - primary screen";
                else
                    screensaverForm.Text = screensaverForm.Text + " - " + screen.DeviceName;
                screensaverForm.StartPosition = FormStartPosition.Manual;

                if (AppState.DebugMode) //pokud se jedná o debug mode
                {
                    screensaverForm.WindowState = FormWindowState.Normal;
                    screensaverForm.Bounds = new Rectangle(150, 50, 640, 480);
                }
                else
                {
                    //screensaverForm.WindowState = FormWindowState.Maximized;
                    screensaverForm.Bounds = screen.Bounds;
                }
                screensaverForm.BackColor = AppState.BackgroundColor;
                screensaverForm.Show();
                screensaverForm.Update(); //this is needed to correct draw of the form (I dont know why)
                drawer.ScreenDefinitions.Add(new ScreenDefinition(screensaverForm, screensaverForm.CreateGraphics(), screen.Primary));
                logger.Debug("Screensaver started on " + screen.DeviceName, EventLogEntryType.Information);
            }

            //get focus on primary screen (to show dialogs)
            foreach (ScreenDefinition sd in drawer.ScreenDefinitions)
                if (sd.IsPrimaryScreen)
                    sd.ScreenForm.Activate();

            //v debug modu nejde vyskočit pouhým pohnutím myši
            if (AppState.DebugMode) AppState.ExitOnlyWithEscape = true;

            //sestavení adresářů k procházení - pouze tehdy, když neděláme slideshow adresáře
            if (!AppState.IsFolderSlideShowMode)
            {
                logger.Debug("Draw welcome screen");
                //draw welcome screen
                drawer.DrawImageToAllForms(BtmGen.GenerateWelcomeBitmap());

                dirHelper.BuildDirectoryIndex(null);
                dirHelper.BuildImageListFromFile(AppState.FileWithImagePaths);
            }


            //načti historii, pokud nepouštíme slideshow
            if (!AppState.IsFolderSlideShowMode)
            {
                //load history from previous screensaver run
                AppState.shownFileInfoList = AppState.loadHistory();

                //pokud normálně promítáme náhodné fotky
                if (!AppState.GoThroughFolder)
                {
                    //pokud je v historii aspon jeden, tak nastavime index tak
                    //aby se první ukázal poslední z minula
                    if (AppState.shownFileInfoList.Count > 0)
                        AppState.shownFileInfoListIndex = AppState.shownFileInfoList.Count - 2;

                    //protože se ImageNo zvedá až s každým "nově vybraným" obrázkem (v metodě DrawOneOfImages) a my začneme první obrázek
                    //z historie, tak tady zvedneme ImageNo na jedničku na tvrdo - neplatí to ale pro slideshow, které nezačíná v historii
                    AppState.ImageNo++;
                }
                //pokud jedem GTF, tak vezmeme poslední fotku v historii a od ní pustíme GTF
                else
                {
                    if (AppState.shownFileInfoList.Count > 0)
                    {
                        //vytáhneme si poslední fotku
                        FileInfo lastFile = AppState.shownFileInfoList[AppState.shownFileInfoList.Count - 1];
                        //vyřadíme ji z historie, aby se neopakovala
                        AppState.shownFileInfoList.RemoveAt(AppState.shownFileInfoList.Count - 1);
                        AppState.shownFileInfoListIndex = AppState.shownFileInfoList.Count - 1;
                        //nastavíme GTF od tohoto obrázku
                        SetGoThroughFolder(lastFile.Directory, lastFile, SearchOption.AllDirectories);
                    }
                }
            }

            DrawNext();
            timer.Start();

            if (AppState.Check4Updates && !AppState.DebugMode)
                Check4Update();
        }

        void t_Tick(object sender, EventArgs e)
        {
            //logger.Debug("Tick");
            timer.Stop();
            AppState.SmoothHideCurrentImage = true; //zobrazení příštího obrázku bude s postupným skrytím předchozího
            DrawNext();
            timer.Start();
        }

        /// <summary>
        /// Vykreslení další fotky
        /// </summary>
        public void DrawNext()
        {
            FileInfo fileInfo = null;
            logger.Debug("Draw next");
            try
            {
                //pokud jsme v historii, tak promítni fotku dle aktuálního indexu
                if (!AppState.forceNextImageIsNew && AppState.shownFileInfoListIndex < AppState.shownFileInfoList.Count - 1)
                {
                    //protože jsme v historii, tak ten obrázek zobrazíme jinak, nez metodou showImage()
                    AppState.shownFileInfoListIndex++;
                    fileInfo = AppState.shownFileInfoList[AppState.shownFileInfoListIndex];
                    //pokud ten soubor není v zakázaných
                    if (dirHelper.isNotExcludedFile(fileInfo) && dirHelper.isNotExcludedDirectory(fileInfo.Directory) && ((fileInfo.Directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                    {
                        drawer.DrawComposition(fileInfo, false);
                    }
                    else
                        DrawNext(); //promítni další                 
                }
                //pokud máme něco v AppState.toShowFileInfoList (fotky, které se "mají" promítnout, než se promítnou další náhodné)
                else if (AppState.toShowFileInfoList.Count > 0)
                {
                    //vezmeme náhodnou fotku (to je zejména kvůli procházení fotek ze souboru)
                    fileInfo = AppState.toShowFileInfoList[random.Next(AppState.toShowFileInfoList.Count)];
                    AppState.toShowFileInfoList.Remove(fileInfo);
                    //pokud ten soubor není v zakázaných
                    if (dirHelper.isNotExcludedFile(fileInfo) && dirHelper.isNotExcludedDirectory(fileInfo.Directory) && ((fileInfo.Directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                    {
                        showImage(fileInfo);
                    }
                    else
                        DrawNext(); //promítni další                       
                }
                //vyber náhodnou fotku podle adresáře, nebo procházej adresáře v modu GTF (včetně slideshow)
                else
                {
                    if (AppState.forceNextImageIsNew)
                        AppState.forceNextImageIsNew = false; //reset forceNextImageIsNew
                    DrawOneOfImages();
                }

            }
            catch (Exception ex)
            {
                if (fileInfo == null)
                    logger.Fatal("Error while drawing next image: shownIndex = " + AppState.shownFileInfoListIndex + ", shownFileInfoList.Count = " + AppState.shownFileInfoList.Count, ex);
                //WindowsLogWriter.WriteLog("DrawNext - Error while drawing next image: shownIndex = " + AppState.shownFileInfoListIndex + ", shownFileInfoList.Count = " + AppState.shownFileInfoList.Count + ", exception = " + ex.Message, EventLogEntryType.Error);
                else
                    logger.Fatal("Error while drawing next image: '" + fileInfo.FullName + "'", ex);
                //WindowsLogWriter.WriteLog("DrawNext - Error while drawing next image: '" + fileInfo.FullName + "', exception = " + ex.Message, EventLogEntryType.Error);
                drawer.DrawImageToAllForms(BtmGen.GenerateErrorBitmap("Error while drawing next image"));
            }
        }

        /// <summary>
        /// Draws previous image
        /// </summary>
        public void DrawPrevious()
        {
            FileInfo fileInfo = null;
            logger.Debug("Draw previous");
            try
            {
                if (AppState.shownFileInfoList.Count > 0)
                {
                    if (AppState.shownFileInfoListIndex > 0)
                    {
                        AppState.shownFileInfoListIndex--;
                        fileInfo = AppState.shownFileInfoList[AppState.shownFileInfoListIndex];
                        //pokud ten soubor není v zakázaných
                        if (dirHelper.isNotExcludedFile(fileInfo) && dirHelper.isNotExcludedDirectory(fileInfo.Directory) && ((fileInfo.Directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                        {
                            drawer.DrawComposition(fileInfo, false);
                        }
                        else
                            DrawPrevious();  //promítni další                
                    }
                }
            }
            catch (Exception ex)
            {
                if (fileInfo != null)
                    logger.Fatal("Error while drawing previous image: shownIndex = " + AppState.shownFileInfoListIndex + ", shownFileInfoList.Count = " + AppState.shownFileInfoList.Count, ex);
                //WindowsLogWriter.WriteLog("DrawPrevious - Error while drawing previous image: shownIndex = " + AppState.shownFileInfoListIndex + ", shownFileInfoList.Count = " + AppState.shownFileInfoList.Count + ", exception = " + ex.Message, EventLogEntryType.Error);
                else
                    logger.Fatal("Error while drawing previous image: '" + fileInfo.FullName + "'", ex);
                //WindowsLogWriter.WriteLog("DrawPrevious - Error while drawing previous image: '" + fileInfo.FullName + "', exception = " + ex.Message, EventLogEntryType.Error);
                drawer.DrawImageToAllForms(BtmGen.GenerateErrorBitmap("Error while drawing previous image"));
            }
        }

        /// <summary>
        /// Vykreslení náhodné fotky
        /// </summary>
        private void DrawOneOfImages()
        {
            try
            {
                FileInfo imageFile = null;
                //když je zapnutý mod Go Through Folder (GTF), nebo se dělá slideshow adresáře
                if (AppState.GoThroughFolder)
                {
                    logger.Debug("Draw next image in folder");
                    //pokud máme normálně co promítat z adresáře
                    if (AppState.folderImagesFileInfoList.Count > 0)
                    {
                        imageFile = AppState.folderImagesFileInfoList[0]; //take first of folder images
                        logger.Debug("GTF - " + (AppState.FolderImagesCount - AppState.folderImagesFileInfoList.Count + 1) + ". image of " + AppState.FolderImagesCount + " - " + imageFile.FullName);
                        AppState.folderImagesFileInfoList.Remove(imageFile); //remove it
                        //pokud je ten soubor v zakázaných, najdi další
                        if (!dirHelper.isNotExcludedFile(imageFile))
                        {
                            DrawNext();
                            return;
                        }
                    }
                    //již jsme promítli všechny fotky z adresáře (ale není to slideshow adresáře) takže musíme najít další adresář
                    else if (!AppState.IsFolderSlideShowMode)
                    {
                        logger.Debug("Find new folder for GTF");
                        DirectoryInfo newDir = null;
                        //zkusíme vzít další folder v pořadí - prochází se všechny načtené adresáře, nezávisle na tom, co už bylo promítnuto
                        if (!AppState.GTFRandomNextFolder && AppState.currentImageFileInfo != null)
                        {
                            //vytáhneme si folder poslední promítané fotky
                            DirectoryInfo currDir = AppState.currentImageFileInfo.Directory;
                            //najdeme ho v načtených adresářích
                            int i = -1;
                            foreach (DirectoryInfo di in AppState.directoryInfoList)
                                if (string.Compare(di.FullName, currDir.FullName, true) == 0)
                                    i = AppState.directoryInfoList.IndexOf(di);

                            //logger.Debug("Current folder: " + currDir.FullName + " with index " + i);

                            //pokud jsme našli adresář poslední promítané fotky vezmeme následující a podle něj vytáhneme adresář
                            if (i != -1)
                            {
                                i++; //vezmem následující adresář
                                //pokud jsme na posledním adresáři, vezmem to od začátku
                                if (i >= AppState.directoryInfoList.Count)
                                    i = 0;

                                newDir = AppState.directoryInfoList[i];
                                logger.Debug("GTF - selected folder: " + newDir.FullName);

                                //musíme ověřit, zda tam jsou obrázky, nebo není excluded
                                if (dirHelper.getImagesInfosFromDirectory(newDir, AppState.ImagesPatterns, SearchOption.TopDirectoryOnly).Count == 0)
                                {
                                    i++; //verzmeme následující adresář
                                    newDir = null;
                                    logger.Debug("GTF - no images found!");
                                    //zkusíme najít další adresář - projedeme všechny načtené adresáře
                                    for (int j = 0; j < AppState.directoryInfoList.Count; j++)
                                    {
                                        //index je sice od nuly, ale bereme nejdřív fotky od "i" do konce a pak od začátku do "i"
                                        if ((i + j) < AppState.directoryInfoList.Count)
                                            newDir = AppState.directoryInfoList[i + j];
                                        else
                                            newDir = AppState.directoryInfoList[j];
                                        logger.Debug("GTF - selected folder: " + newDir.FullName);

                                        //ověříme přítomnost obrázků a excludování a případně vyskočíme z cyklu
                                        if (dirHelper.getImagesInfosFromDirectory(newDir, AppState.ImagesPatterns, SearchOption.TopDirectoryOnly).Count > 0)
                                            break;

                                        logger.Debug("GTF - no images found");
                                        newDir = null;
                                    }
                                }
                            }
                        }

                        //pokud nemáme následující adresář tak zkus náhodný
                        if (newDir == null)
                        {
                            logger.Debug("New random folder for GTF");
                            newDir = dirHelper.getRandomDirectory(random);
                        }

                        //pokud máme následující, nebo náhodný adresář
                        if (newDir != null)
                        {
                            //načteme obrázky z toho adresáře
                            AppState.folderImagesFileInfoList = dirHelper.getImagesInfosFromDirectory(newDir, AppState.ImagesPatterns, SearchOption.TopDirectoryOnly);
                            AppState.FolderImagesCount = AppState.folderImagesFileInfoList.Count; //store original count of images in the folder
                            logger.Debug("GTF - number of images in folder: " + AppState.FolderImagesCount);
                            if (AppState.FolderImagesCount > 0)
                            {
                                imageFile = AppState.folderImagesFileInfoList[0]; //take first of folder images
                                logger.Debug("GTF - " + (AppState.FolderImagesCount - AppState.folderImagesFileInfoList.Count + 1) + ". image of " + AppState.FolderImagesCount + " - " + imageFile.FullName);
                                AppState.folderImagesFileInfoList.Remove(imageFile); //remove selected image
                                //pokud je to zakázaný soubor
                                if (!dirHelper.isNotExcludedFile(imageFile))
                                {
                                    logger.Debug("GTF - image: " + imageFile.FullName + " is excluded");
                                    DrawNext(); //zkusíme jinou fotku
                                    return;
                                }
                            }
                            else
                            {
                                logger.Debug("GTF - try another folder");
                                DrawNext(); //když v adresáři nejsou žádné fotky, zkusíme jiný adresář
                                return;
                            }
                        }
                        else if (AppState.directoryInfoIndexes.Count > 0) //tahle podmínka tady musí být, aby se nám to nezacyklilo, když nemůžeme najít následující, ani náhodny adresář - provede se nové načtení adresářů
                        {
                            logger.Debug("GTF - no folder found, try another");
                            DrawNext(); //když jsme nenašli žádný adresář, zkusíme jiný 
                            return;
                        }
                    }
                    //pokud jsme promítli všechno z adresáře a bylo to slideshow, tak se to dole vypne
                }
                // Běžné vybrání náhodné fotky z náhodného adresáře a promítnutí
                else if (AppState.directoryInfoIndexes.Count > 0)
                {
                    DirectoryInfo dir = dirHelper.getRandomDirectory(random);

                    //pokud se podařilo najít adresář
                    if (dir != null)
                    {
                        //načteme obrázky
                        List<FileInfo> images = dirHelper.getImagesInfosFromDirectory(dir, AppState.ImagesPatterns, SearchOption.TopDirectoryOnly);
                        AppState.FolderImagesCount = images.Count; //store original count of images in the folder
                        logger.Debug(AppState.FolderImagesCount + " images in folder");
                        if (images.Count > 0)
                        {
                            int randIndex = random.Next(images.Count);
                            //jeden náhodně vybereme
                            imageFile = images[randIndex];
                            logger.Debug((randIndex + 1) + ". image seleted: " + imageFile.Name);
                            //pokud je to zakázaný soubor
                            if (!dirHelper.isNotExcludedFile(imageFile))
                            {
                                logger.Debug("File is excluded - will try another image in same folder");
                                DrawNext(); //zkusíme jiný adresář
                                return;
                            }
                        }
                        else
                        {
                            logger.Debug("No images in the folder - will try another folder");
                            //zkusíme jiný adresář
                            DrawNext();
                            return;
                        }
                    }
                    else
                    {
                        logger.Debug("Folder not found - try another");
                        //zkusíme jiný adresář
                        DrawNext();
                        return;
                    }
                }

                //promítnutí nalezené fotky
                if (null != imageFile)
                {
                    showImage(imageFile);
                }
                //pokud nemáme fotky na promítnutí a je mod procházení adresáře "IsFolderSlideShowMode", pak vypni screensaver
                else if (AppState.IsFolderSlideShowMode)
                {
                    logger.Debug("All images from folder where showed");
                    timer.Stop();
                    timer.Dispose();
                    //if any image wasn't presented
                    if (AppState.shownFileInfoList.Count == 0)
                    {
                        drawer.DrawImageToAllForms(BtmGen.GenerateErrorBitmap("sorry, there is no image of the type " + AppState.ImagesPatterns + " in the selected folder!"));
                        Thread.Sleep(4000);
                    }
                    ApplicationExit();
                }
                //pokud nemáme co promítnout, zkusíme znovu sestavit adresáře
                else
                {
                    //pokud nemáme co promítnout a dosud jsme nic neukázali, je někde chyba
                    //POZN.: jednička tam je proto, že se po spuštění promítla jedna fotka z historie (která existuje)
                    //ale podle nového nastavení screensaveru není co promítat
                    if (AppState.shownFileInfoList.Count <= 1)
                    {
                        logger.Debug("There aren't any images to show!");
                        timer.Stop();
                        string text = "sorry, there is no file of the type ";
                        foreach (var item in AppState.ImagesPatterns)
                        {
                            text += item.ToString() + ", ";
                        }
                        text += "in \n\r\n\r1) selected folder or ";
                        text += "\n\r\n\r2) in any of pre-defined folders:\n\r";
                        foreach (string folder in AppState.ImagesRootFolders)
                            text = text + "\n\r    " + folder;

                        if (null != AppState.FileWithImagePaths && AppState.FileWithImagePaths.Length > 0)
                            text += "\n\r\n\r3) or in the file '" + AppState.FileWithImagePaths + "'";

                        text += "\n\r\n\r\n\r\n\rPLEASE CHECK YOUR SCREENSAVER CONFIGURATION";
                        drawer.DrawImageToAllForms(BtmGen.GenerateErrorBitmap(text));
                    }
                    else
                    {
                        //znovu naplníme list adresářů/indexů a zkusíme vybrat fotku
                        dirHelper.BuildDirectoryIndex(null);
                        dirHelper.BuildImageListFromFile(AppState.FileWithImagePaths);
                        DrawNext();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Can't draw image file: '" + AppState.currentImageFileInfo.FullName + "'", ex);
                //logger.Debug("DrawOneOfImages - Can't draw image file: '" + AppState.currentImageFileInfo.FullName + "', exception = " + ex.Message);
                //WindowsLogWriter.WriteLog("DrawOneOfImages - Can't draw image file: '" + AppState.currentImageFileInfo.FullName + "', exception = " + ex.Message, EventLogEntryType.Error);
                drawer.DrawImageToAllForms(BtmGen.GenerateErrorBitmap("Error while draw image file: '" + AppState.currentImageFileInfo.FullName + "'"));
            }
        }

        /// <summary>
        /// Volání zobrazení obrázku
        /// </summary>
        /// <param name="imageFile"></param>
        private void showImage(FileInfo imageFile)
        {
            //přidáme fotku do historie
            AppState.shownFileInfoList.Add(imageFile);

            //zkontrolujeme a případně posuneme historii
            if (AppState.shownFileInfoList.Count > ApplicationState.HISTORY_SIZE)
                AppState.shownFileInfoList.RemoveAt(0);

            AppState.shownFileInfoListIndex = AppState.shownFileInfoList.Count - 1;

            drawer.DrawComposition(imageFile, false);
            AppState.ImageNo++;
        }

        #region User input

        public void ShowName()
        {
            if (AppState.ShowFileName)
            {
                logger.Debug("Hide file name");
                AppState.ShowFileName = false;
            }
            else
            {
                logger.Debug("Show file name");
                AppState.ShowFileName = true;
            }

            if (null != AppState.currentImageFileInfo)
                drawer.DrawComposition(AppState.currentImageFileInfo, false);
        }

        public void ShowDate()
        {
            if (AppState.ShowDate)
            {
                logger.Debug("Hide date");
                AppState.ShowDate = false;
            }
            else
            {
                logger.Debug("Show date");
                AppState.ShowDate = true;
            }
            if (null != AppState.currentImageFileInfo)
                drawer.DrawComposition(AppState.currentImageFileInfo, false);
        }

        public void ShowExif()
        {
            if (AppState.ShowExif)
            {
                logger.Debug("Hide exif");
                AppState.ShowExif = false;
            }
            else
            {
                logger.Debug("Show exif");
                AppState.ShowExif = true;
            }
            if (null != AppState.currentImageFileInfo)
                drawer.DrawComposition(AppState.currentImageFileInfo, false);
        }

        public void ShowTime()
        {
            if (AppState.ShowTime)
            {
                logger.Debug("Hide time");
                AppState.ShowTime = false;
            }
            else
            {
                logger.Debug("Show time");
                AppState.ShowTime = true;
            }
            if (null != AppState.currentImageFileInfo)
                drawer.DrawComposition(AppState.currentImageFileInfo, false);
        }

        public void RotateRight()
        {
            if (!AppState.currentImageFileInfo.IsReadOnly)
            {
                logger.Debug("Rotate right");
                timer.Stop();
                drawer.DrawOverlay(120);

                ExifSupport.RotateImage(AppState.currentImageFileInfo, true);
                drawer.DrawComposition(AppState.currentImageFileInfo, true);

                timer.Start();
            }
        }

        public void RotateLeft()
        {
            if (!AppState.currentImageFileInfo.IsReadOnly)
            {
                logger.Debug("Rotate left");
                timer.Stop();
                drawer.DrawOverlay(120);

                ExifSupport.RotateImage(AppState.currentImageFileInfo, false);
                drawer.DrawComposition(AppState.currentImageFileInfo, true);

                timer.Start();
            }
        }

        public void EditMetadata()
        {
            if (!AppState.currentImageFileInfo.IsReadOnly)
            {
                logger.Debug("Edit metadata");
                timer.Stop();
                drawer.DrawOverlay(120);
                //AppState.ExitOnlyWithEscape = true;
                //Cursor.Show();

                MetadataForm metadataForm = new MetadataForm(AppState.currentImageFileInfo);
                DialogResult result = metadataForm.ShowDialog();

                if (result == DialogResult.OK)
                    drawer.DrawComposition(AppState.currentImageFileInfo, true);
                else
                    DrawNext();

                //Cursor.Hide();
                //AppState.ExitOnlyWithEscape = false;
                timer.Start();
            }
        }

        public void Copy()
        {
            if (null != AppState.currentImageFileInfo)
            {
                FileInfo toCopy = AppState.shownFileInfoList[AppState.shownFileInfoListIndex];
                string newFileName = "";
                try
                {
                    drawer.DrawOverlay(120);

                    newFileName = AppState.CopyToFolder + "\\" + toCopy.Name;
                    string nameWithoutExtension = toCopy.Name.Substring(0, (toCopy.Name.Length - toCopy.Extension.Length));
                    int order = 1;
                    while (File.Exists(newFileName))
                    {
                        newFileName = AppState.CopyToFolder + "\\" + nameWithoutExtension + "_" + Convert.ToString(order) + toCopy.Extension;
                        order++;
                    }

                    toCopy.CopyTo(newFileName, true);
                    MessageBox.Show("File " + toCopy.FullName + " copied to " + newFileName + "\n\r(press enter)", "File copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    logger.Debug("File " + toCopy.FullName + " copied to " + newFileName);
                    //redraw composition without shadow
                    if (AppState.currentImageFileInfo != null)
                        drawer.DrawComposition(AppState.currentImageFileInfo, false);
                }
                catch (Exception e3)
                {
                    logger.Fatal("Can't copy file '" + toCopy.FullName + "' to '" + newFileName + "'", e3);
                    //WindowsLogWriter.WriteLog("MainForm_KeyDown - Can't copy file '" + toCopy.FullName + "' to '" + newFileName + "', exception = " + e3.Message, EventLogEntryType.Error);
                    MessageBox.Show("Can't copy file " + toCopy.FullName + " to " + newFileName + "\n\r(press enter)", "File not copied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Sets "go through folder" based on given directory info
        /// </summary>
        /// <param name="dir">directory to pass through</param>
        /// <param name="startFromFile">file from which to start</param>
        /// <param name="searchOption">search options</param>
        public void SetGoThroughFolder(DirectoryInfo dir, FileInfo startFromFile, SearchOption searchOption)
        {
            if (null == dir && AppState.toShowFileInfoList.Count > 0)
                dir = AppState.toShowFileInfoList[0].Directory;
            if (null != dir)
            {
                logger.Debug("GTF ON - '" + dir.FullName + "'");
                AppState.GoThroughFolder = true;
                AppState.forceNextImageIsNew = true; //aby se nepokračovalo v historii (pokud tam jsme, ale šlo se na nový)
                AppState.folderImagesFileInfoList.Clear();
                AppState.folderImagesFileInfoList = dirHelper.getImagesInfosFromDirectory(dir, AppState.ImagesPatterns, searchOption); //take other images in the folder
                AppState.FolderImagesCount = AppState.folderImagesFileInfoList.Count; //store original count of images in the folder

                //pokud máme začít od nějakého adresáře, tak všechny před ním vyřaď
                if (startFromFile != null)
                {
                    for (int i = 0; i < AppState.FolderImagesCount; i++)
                    {
                        if (AppState.folderImagesFileInfoList[0].FullName.Equals(startFromFile.FullName))
                            break;
                        AppState.folderImagesFileInfoList.RemoveAt(0);
                    }
                }
            }
            AppState.SmoothHideCurrentImage = true; //zobrazení příštího obrázku bude s postupným skrytím předchozího
        }

        /// <summary>
        /// Run "go through folder" based on current image
        /// </summary>
        public void GoThroughFolderON(bool startFromCurrent)
        {
            if (null != AppState.currentImageFileInfo)
            {
                timer.Stop();

                if (startFromCurrent)
                    SetGoThroughFolder(AppState.currentImageFileInfo.Directory, AppState.currentImageFileInfo, SearchOption.TopDirectoryOnly);
                else
                    SetGoThroughFolder(AppState.currentImageFileInfo.Directory, null, SearchOption.TopDirectoryOnly);

                DrawNext();
                timer.Start();
            }
        }

        /// <summary>
        /// Turn OFF "go through folder" mode
        /// </summary>
        public void GoThroughFolderOFF()
        {
            timer.Stop();
            logger.Debug("GTF OFF");
            AppState.GoThroughFolder = false;
            AppState.folderImagesFileInfoList.Clear();
            DrawNext();
            timer.Start();
        }

        public void VisitWeb()
        {
            logger.Debug("Visit web");
            timer.Stop();
            System.Diagnostics.Process.Start(AppState.Url);
            ApplicationExit();
        }

        public void ShowHelp()
        {
            logger.Debug("Show help");
            string text = "";
            text += "Keystrokes:\n\r";
            text = text + "S - show settings\n\r";
            text = text + "H - show this help\n\r";
            text = text + "N - show file name\n\r";
            text = text + "D - show image date\n\r";
            text = text + "T - show system time, number of images and duration of presentation\n\r";
            text = text + "E - show exif\n\r";
            text = text + "R - rotate right\n\r";
            text = text + "L - rotate left\n\r";
            text = text + "M - edit exif title, description and user comment\n\r";
            text = text + "C - copy current image to the " + AppState.CopyToFolder + "\n\r";
            text = text + "F - swith on/off \"go through folder\" mode (start with first image of the folder)\n\r";
            text = text + "Alt + F - swith on/off \"go through folder\" mode (start with current image)\n\r";
            text = text + "Ctrl + F - restart current folder presentation in GTF mode\n\r";
            text = text + "Left / Right arrow keys - go to previous / next image\n\r";
            text = text + "Up / Down arrow keys - speed up /down current presentation\n\r";
            text = text + "X - exclude current image (will not be shown next time)\n\r";
            text = text + "Alt + X - exclude whole current image folder (will not be shown next time)\n\r";
            text = text + "Delete - delete current image\n\r";
            text = text + "Space - pause / unpause presentation\n\r";
            text = text + "F1 - save path of the image to " + AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF1 + ApplicationState.SAVE_PATH_FILES_EXTENSION + "\n\r";
            text = text + "F2 - save path of the image to " + AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF2 + ApplicationState.SAVE_PATH_FILES_EXTENSION + "\n\r";
            text = text + "F3 - save path of the image to " + AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF3 + ApplicationState.SAVE_PATH_FILES_EXTENSION + "\n\r";
            text = text + "F4 - save path of the image to " + AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF4 + ApplicationState.SAVE_PATH_FILES_EXTENSION + "\n\r";
            text = text + "F5 - save path of the image to " + AppState.SavePathToFolder + @"\" + AppState.SavePathToFileF5 + ApplicationState.SAVE_PATH_FILES_EXTENSION + "\n\r";
            text = text + "W - go to screensaver web page " + AppState.Url;

            drawer.DrawOverlay(120);

            MessageBox.Show(text, ApplicationState.APP_NAME_WITH_VERSION + " - help", MessageBoxButtons.OK);
            //redraw composition without shadow
            if (AppState.currentImageFileInfo != null)
                drawer.DrawComposition(AppState.currentImageFileInfo, false);
        }

        public void ShowSettings()
        {
            timer.Stop();
            logger.Debug("Show settings");

            if (AppState.shownFileInfoList.Count > 0) //save to history last 10 images
            {
                List<FileInfo> toHistory = new List<FileInfo>();
                int i = 1;
                for (int j = AppState.shownFileInfoList.Count; j > 0; j--)
                {
                    if (i <= ApplicationState.LOADED_HISTORY_SIZE)
                    {
                        toHistory.Add(AppState.shownFileInfoList[j - 1]);
                        i++;
                    }
                }
                AppState.saveHistory(toHistory);
            }

            foreach (ScreenDefinition sd in drawer.ScreenDefinitions)
            {
                sd.ScreenForm.Close();
                sd.FormGraphics.Dispose();
            }

            Cursor.Show();
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.Show();
        }

        public void Next()
        {
            DrawNext();
            if (timer.Enabled)
            {
                //restart timer
                timer.Stop();
                timer.Start();
            }
        }

        public void Previous()
        {
            DrawPrevious();
            if (timer.Enabled)
            {
                //restart timer
                timer.Stop();
                timer.Start();
            }
        }

        public void Pause()
        {
            if (timer.Enabled)
            {
                logger.Debug("Pause");
                timer.Enabled = false;
            }
            else
            {
                logger.Debug("Resume");
                DrawNext();
                timer.Enabled = true;
            }
        }

        public void Delete()
        {
            if (null != AppState.currentImageFileInfo)
            {
                timer.Stop();
                drawer.DrawOverlay(120);

                FileInfo toDetele = AppState.shownFileInfoList[AppState.shownFileInfoListIndex];
                DialogResult result = MessageBox.Show("Delete file '" + toDetele.FullName + "' ? \n\r(use keyboard to select button)", "Do you really want to delete the file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    logger.Debug("Delete file '" + toDetele.FullName + "'");
                    try
                    {
                        //remove the image from lists
                        for (int i = 0; i < AppState.folderImagesFileInfoList.Count; i++)
                            if (AppState.folderImagesFileInfoList[i].FullName.Equals(toDetele.FullName))
                                AppState.folderImagesFileInfoList.Remove(AppState.toShowFileInfoList[i]);

                        for (int i = 0; i < AppState.toShowFileInfoList.Count; i++)
                            if (AppState.toShowFileInfoList[i].FullName.Equals(toDetele.FullName))
                                AppState.toShowFileInfoList.Remove(AppState.toShowFileInfoList[i]);

                        for (int i = 0; i < AppState.shownFileInfoList.Count; i++)
                            if (AppState.shownFileInfoList[i].FullName.Equals(toDetele.FullName))
                                AppState.shownFileInfoList.Remove(AppState.shownFileInfoList[i]);

                        File.Delete(toDetele.FullName);

                        DrawNext();
                    }
                    catch (Exception e2)
                    {
                        logger.Fatal("Can't delete file: '" + AppState.currentImageFileInfo.FullName + "'", e2);
                        //WindowsLogWriter.WriteLog("MainForm_KeyDown - Can't delete file: '" + AppState.currentImageFileInfo.FullName + "', exception = " + e2.Message, EventLogEntryType.Error);
                        MessageBox.Show(e2.Message, "Can't delete file!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //DrawImageToAllForms(GenerateErrorBitmap(e2.Message));
                    }
                }
                else
                {
                    if (AppState.currentImageFileInfo != null)
                        drawer.DrawComposition(AppState.currentImageFileInfo, false);
                }

                timer.Start();
            }
        }

        public void SpeedUp()
        {
            logger.Debug("Speed up (1sec)");
            //AppState.Interval--;
            //timer.Interval = AppState.Interval * 1000;
            if (timer.Interval > 1000)
                timer.Interval -= 1000;
        }

        public void SpeedDown()
        {
            logger.Debug("Speed down (1sec)");
            //AppState.Interval++;
            //timer.Interval = AppState.Interval * 1000;
                timer.Interval += 1000;
        }

        public void SaveFileName(String toFile)
        {
            if (null != AppState.currentImageFileInfo)
            {
                drawer.DrawOverlay(120);

                try
                {
                    StreamWriter sw = File.AppendText(AppState.SavePathToFolder + @"\" + toFile + ApplicationState.SAVE_PATH_FILES_EXTENSION);
                    sw.WriteLine(AppState.currentImageFileInfo.FullName);
                    sw.Flush();
                    sw.Close();
                    MessageBox.Show("Path '" + AppState.currentImageFileInfo.FullName + "' saved to '" + AppState.SavePathToFolder + @"\" + toFile + ApplicationState.SAVE_PATH_FILES_EXTENSION + "'", "Path Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    logger.Debug("Path '" + AppState.currentImageFileInfo.FullName + "' saved to '" + AppState.SavePathToFolder + @"\" + toFile + ApplicationState.SAVE_PATH_FILES_EXTENSION + "'");
                }
                catch (Exception eF1)
                {
                    logger.Fatal("Can't save path to the text file: path = '" + AppState.currentImageFileInfo.FullName + "', file = '" + AppState.SavePathToFolder + @"\" + toFile + ApplicationState.SAVE_PATH_FILES_EXTENSION + "'", eF1);
                    //WindowsLogWriter.WriteLog("MainForm_KeyDown - Can't save path to the text file: path = '" + AppState.currentImageFileInfo.FullName + "', file = '" + AppState.SavePathToFolder + @"\" + toFile + AppState.SAVE_PATH_FILES_EXTENSION + "', exception = " + eF1.Message, EventLogEntryType.Error);
                }
                //redraw composition without shadow
                if (AppState.currentImageFileInfo != null)
                    drawer.DrawComposition(AppState.currentImageFileInfo, false);
            }

        }

        /// <summary>
        /// Exclude present image
        /// </summary>
        public void ExcludeCurrentImage()
        {
            if (null != AppState.currentImageFileInfo)
            {
                timer.Stop();
                drawer.DrawOverlay(120);

                String toExclude = AppState.currentImageFileInfo.FullName;
                DialogResult result = MessageBox.Show("Exclude image '" + toExclude + "' ? \n\r(use keyboard to select button)", "Do you really want to exclude this image?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    logger.Debug("Exclude image " + toExclude);
                    //remove the image from lists
                    for (int i = 0; i < AppState.folderImagesFileInfoList.Count; i++)
                        if (AppState.folderImagesFileInfoList[i].FullName.Equals(toExclude))
                            AppState.folderImagesFileInfoList.Remove(AppState.toShowFileInfoList[i]);

                    for (int i = 0; i < AppState.toShowFileInfoList.Count; i++)
                        if (AppState.toShowFileInfoList[i].FullName.Equals(toExclude))
                            AppState.toShowFileInfoList.Remove(AppState.toShowFileInfoList[i]);

                    for (int i = 0; i < AppState.shownFileInfoList.Count; i++)
                        if (AppState.shownFileInfoList[i].FullName.Equals(toExclude))
                            AppState.shownFileInfoList.Remove(AppState.shownFileInfoList[i]);

                    List<string> dontShowImages = AppState.DontShowImages;
                    dontShowImages.Add(toExclude);
                    AppState.DontShowImages = dontShowImages;

                    DrawNext();
                }
                else
                {
                    if (AppState.currentImageFileInfo != null)
                        drawer.DrawComposition(AppState.currentImageFileInfo, false);
                }

                timer.Start();
            }
        }

        /// <summary>
        /// Exclude present folder
        /// </summary>
        public void ExcludeCurrentImageFolder()
        {
            if (null != AppState.currentImageFileInfo)
            {
                timer.Stop();
                drawer.DrawOverlay(120);

                String toExclude = AppState.currentImageFileInfo.Directory.FullName;
                DialogResult result = MessageBox.Show("Exclude directory '" + toExclude + "' ? \n\r(use keyboard to select button)", "Do you really want to exclude this directory?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    logger.Debug("Exclude folder " + toExclude);
                    List<string> dontShowFolders = AppState.DontShowFolders;
                    dontShowFolders.Add(toExclude);
                    AppState.DontShowFolders = dontShowFolders;

                    DrawNext();
                }
                else
                {
                    if (AppState.currentImageFileInfo != null)
                        drawer.DrawComposition(AppState.currentImageFileInfo, false);
                }

                timer.Start();
            }
        }

        #endregion

        /// <summary>
        /// Check for screensaver update
        /// </summary>
        public void Check4Update()
        {
            string lcUrl = AppState.Url + "/cur_version.php";
            string lcHtml = null;

            logger.Debug("Check for update on " + lcUrl);
            HttpWebRequest loHttp = (HttpWebRequest)WebRequest.Create(lcUrl);

            // *** Set properties

            loHttp.Timeout = 10000;     // 10 secs
            loHttp.UserAgent = "PhotoScreensaverPlus";

            try
            {
                // *** Retrieve request info headers
                HttpWebResponse loWebResponse = (HttpWebResponse)loHttp.GetResponse();
                Encoding enc = Encoding.GetEncoding("utf-8");
                StreamReader loResponseStream = new StreamReader(loWebResponse.GetResponseStream(), enc);

                lcHtml = loResponseStream.ReadToEnd();
                loWebResponse.Close();
                loResponseStream.Close();
            }
            catch (Exception ex1)
            {
                logger.Fatal("Cannot get result from updated server", ex1);
                //WindowsLogWriter.WriteLog("Check4Update - Cannot get result from updated server: " + ex1.Message, EventLogEntryType.Error);
                //DrawImage(GenerateErrorBitmap("Cannot get result from updated server: " + ex1.Message + "\n\nVISIT WEB PAGE " + AppState.Url + " FOR SCREENSAVER UPDATE PLEASE"), this.CreateGraphics());
            }

            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.LoadXml(lcHtml);
                XmlNodeList major = xDoc.GetElementsByTagName("major");
                XmlNodeList minor = xDoc.GetElementsByTagName("minor");
                //XmlNodeList desc = xDoc.GetElementsByTagName("description");

                int appMa = Convert.ToInt32(Application.ProductVersion.Substring(0, Application.ProductVersion.IndexOf(".")));
                int appMi = Convert.ToInt32(Application.ProductVersion.Substring(Application.ProductVersion.IndexOf(".") + 1, Application.ProductVersion.Length - (Application.ProductVersion.IndexOf(".") + 1)));
                if (appMa < Convert.ToInt32(major[0].InnerText) || appMi < Convert.ToInt32(minor[0].InnerText))
                {
                    UpdateAvailableDialog dialog = new UpdateAvailableDialog();
                    dialog.Show();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Cannot parse result: " + ex.Message + " Result is: " + lcHtml, ex);
                //WindowsLogWriter.WriteLog("Check4Update - Cannot parse result: " + ex.Message + " Result is: " + lcHtml, EventLogEntryType.Error);
                //DrawImage(GenerateErrorBitmap("Cannot parse update server result: " + ex.Message + " Result is: \n" + lcHtml + "\n\nVISIT WEB PAGE " + AppState.Url + " FOR SCREENSAVER UPDATE PLEASE" ), this.CreateGraphics());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ApplicationExit()
        {
            if (AppState.shownFileInfoList.Count > 0) //save to history last 10 images
            {
                List<FileInfo> toHistory = new List<FileInfo>();
                int i = 1;
                for (int j = AppState.shownFileInfoList.Count; j > 0; j--)
                {
                    if (i <= ApplicationState.LOADED_HISTORY_SIZE)
                    {
                        toHistory.Add(AppState.shownFileInfoList[j - 1]);
                        i++;
                    }
                }
                AppState.saveHistory(toHistory);
            }

            AppState.saveCurrentMode();

            foreach (ScreenDefinition sd in drawer.ScreenDefinitions)
            {
                sd.ScreenForm.Close();
                sd.FormGraphics.Dispose();
            }
            logger.Info("Screensaver stopped");
            Application.Exit();
        }
    }
}
