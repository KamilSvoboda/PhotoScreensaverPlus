using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus.State
{
    public class ApplicationState
    {
        private static ApplicationState instance;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        #region Definitions
        private static string REGISTRY_KEY = "SOFTWARE\\Kamil Svoboda\\" + Application.ProductName;
        private const string INTERVAL = "interval";
        private const string IMAGES_ROOT_FOLDERS_COUNT = "image_folders_count";
        private const string IMAGES_ROOT_FOLDER = "image_folder";
        private const string FILE_WITH_IMAGE_PATHS = "file_with_image_paths";
        private const string DONT_SHOW_FOLDER = "dont_show_folder";
        private const string DONT_SHOW_FOLDERS_COUNT = "dont_show_folders_count";
        private const string DONT_SHOW_IMAGE = "dont_show_image";
        private const string DONT_SHOW_IMAGES_COUNT = "dont_show_images_count";
        private const string COPY_TO_FOLDER = "copy_to_folder";
        private const string BACKGROUND_COLOR = "background_color";
        private const string SHOW_FILE_NAME = "show_file_name";
        private const string SHOW_DATE = "show_date";
        private const string SHOW_EXIF = "show_exif";
        private const string SHOW_TEXT_BACKGROUND = "show_text_background";
        private const string SHOW_TIME = "show_time";
        private const string ROTATE_BY_EXIF = "rotate_by_exif";
        private const string FONT_SIZE = "font_size";
        private const string RUN_IN_MODE = "run_in_mode"; //mod, ve kterém se má spořič spustit 0 = stejně jako posledně (last_mode), 1 = normal, 2 = GTF
        private const string LAST_MODE = "last_mode";
        private const string GTF_RANDOM_NEXT_FOLDER = "gtf_random";
        private const string INTERPOLATION = "interpolation";
        private const string CHECK4UPDATES = "check4updates";
        private const string SMOOTH_HIDE_OF_IMAGE = "smooth_hide_of_image";
        private const string EXIT_ONLY_WITH_ESCAPE = "exit_only_with_esc";
        private const string SAVE_PATH_TO_FOLDER = "save_path_to_folder";
        private const string SAVE_PATH_TO_FILE_F1 = "save_path_to_file_f1";
        private const string SAVE_PATH_TO_FILE_F2 = "save_path_to_file_f2";
        private const string SAVE_PATH_TO_FILE_F3 = "save_path_to_file_f3";
        private const string SAVE_PATH_TO_FILE_F4 = "save_path_to_file_f4";
        private const string SAVE_PATH_TO_FILE_F5 = "save_path_to_file_f5";
        private const string HISTORY = "history";
        private const bool DEFAULT_SHOW_FILE_NAME = false;
        private const bool DEFAULT_SHOW_DATE = false;
        private const bool DEFAULT_SHOW_EXIF = false;
        private const bool DEFAULT_SHOW_TEXT_BACKGROUND = true;
        private const bool DEFAULT_SHOW_TIME = false;
        private const bool DEFAULT_ROTATE_BY_EXIF = true;
        private const int DEFAULT_INTERVAL = 5;
        private const int MINIMAL_INTERVAL = 2;
        private const int DEFAULT_FONT_SIZE = 10;
        private const int DEFAULT_RUN_IN_MODE = 0; //defaultně spuštíme tak, jaký byl mod v posledním spuštění
        private const int DEFAULT_LAST_MODE = 1; //defaultní hodnota posledního spuštění je náhodně
        private const bool DEFAULT_GTF_RANDOM_NEXT_FOLDER = true;
        private const bool DEFAULT_INTERPOLATION = false;
        private const bool DEFAULT_CHECK4UPDATES = true;
        private const bool DEFAULT_EXIT_ONLY_WITH_ESCAPE = false;
        private const bool DEFAULT_SMOOTH_HIDE_OF_IMAGE = true;
        public const string SAVE_PATH_FILES_EXTENSION = ".txt";
        private const string DEFAULT_SAVE_PATH_TO_FILE_F1 = "toProcess";
        private const string DEFAULT_SAVE_PATH_TO_FILE_F2 = "toPrint";
        private const string DEFAULT_SAVE_PATH_TO_FILE_F3 = "toWeb";
        private const string DEFAULT_SAVE_PATH_TO_FILE_F4 = "toExhibition";
        private const string DEFAULT_SAVE_PATH_TO_FILE_F5 = "toArchive";
        private static Color DEFAULT_BACKGROUND_COLOR = Color.Black;
        public static int HISTORY_SIZE = 100;
        public static int LOADED_HISTORY_SIZE = 10;
        public static String EVENT_LOG_NAME = "Application";

        public string Url { get { return "http://pssp.svoboda.biz"; } }

        public static string APP_NAME_WITH_VERSION = Application.ProductName + " " + Application.ProductVersion;

        public DateTime startTime = System.DateTime.Now;
        private int imageNo = 0;
        public int ImageNo { get { return imageNo; } set { imageNo = value; } } //number of presented photos

        public List<DirectoryInfo> directoryInfoList = new List<DirectoryInfo>(); //seznam adresářů, které se mají promítat podle konfigurace
        public List<int> directoryInfoIndexes = new List<int>(); //seznam indexů z directoryInfoList - přes toto vybíráme nové adresáře pro promítání (postupně se vyřazují indexy z této kolekce)

        public List<FileInfo> toShowFileInfoList = new List<FileInfo>(); //seznam fotek, která "mají být" promítnuty - mají přednost před náhodným výběrem nové fotky
        public List<FileInfo> shownFileInfoList = new List<FileInfo>(); //seznam fotek, které již byly promítnuty (včetně historie)
        public int shownFileInfoListIndex = 0; //position in shown "history" - used to go previous/next image

        public List<FileInfo> folderImagesFileInfoList = new List<FileInfo>(); //seznam fotek z adresáře (pro mod GTF)
        public int FolderImagesCount { get; set; } //celkový počet fotek aktuálního adresáře

        public FileInfo currentImageFileInfo; //aktuálně promítaná fotka

        public bool forceNextImageIsNew = false; //true when next image have to be "new", disregarding to shownIndex (disregarding "shown" history). 
        //It's used when user returns in "shown" history back to previous image (using left arrow) and activates mode "walk through folder" on some selected image. 
        //Next image (right arrow or timer) should show first image from selected folder (not from history disregarding shownIndex)

        private bool smoothHide = false; //informuje, zda se má provést smooth hide předchozího obrázku
        public bool SmoothHideCurrentImage { get { return smoothHide; } set { smoothHide = value; } }

        private bool isPreviewMode = false;
        public bool IsPreviewMode { get { return isPreviewMode; } set { isPreviewMode = value; } }
        private bool isFolderMode = false; //screensaver was executed with parameter /f (show images from selected folder)
        public bool DebugMode = false;
        public bool IsFolderSlideShowMode { get { return isFolderMode; } set { isFolderMode = value; } }

        public List<string> ImagesPatterns { get { return new List<string>() { "*.jpg", "*.jpeg", "*.tif", "*.tiff", "*.png", "*.gif" }; } }
        public bool ShowFileName { get; set; } //current AppState of "ShowFileName"
        public bool ShowDate { get; set; }
        public bool RotateByExif { get; set; }
        public bool ShowExif { get; set; }
        public bool ShowTextBackground { get; set; }
        public bool ShowTime { get; set; }
        public int RunInMode { get; set; } //hostnota registrů - mod, jak se má spořič spusti (0 - stejně jako minule, 1 - náhodné fotky, 2 - GTF)
        public int LastMode { get; set; } //hodnota registrů - poslední mod, který byl v předchozím spuštění (1 - náhodně, 2 - GTF)
        public bool GoThroughFolder { get; set; } //příznak jestli je aktuálně zapnuté GTF, nebo náhodné fotky
        public bool GTFRandomNextFolder { get; set; }//příznak, že další adresář v GTF má být nalezen náhodně
        public bool BestInterpolation { get; set; }
        public bool Check4Updates { get; set; }
        public bool SmoothHidingEnabled { get; set; }
        public bool ExitOnlyWithEscape { get; set; }

        /// <summary>
        /// Persistate current value of ShowFileName to registry
        /// </summary>
        public void SaveShowFileName()
        {
            saveToRegistry(SHOW_FILE_NAME, Convert.ToString(ShowFileName));
        }

        /// <summary>
        /// Persistate current value of ShowDate to registry
        /// </summary>
        public void SaveShowDate()
        {
            saveToRegistry(SHOW_DATE, Convert.ToString(ShowDate));
        }

        /// <summary>
        /// Persistate current value of RotateByExif to registry
        /// </summary>
        public void SaveRotateByExif()
        {
            saveToRegistry(ROTATE_BY_EXIF, Convert.ToString(RotateByExif));
        }

        /// <summary>
        /// Persistate current value of ShowExif to registry
        /// </summary>
        public void SaveShowExif()
        {
            saveToRegistry(SHOW_EXIF, Convert.ToString(ShowExif));
        }

        /// <summary>
        /// Persistate current value of ShowETime to registry
        /// </summary>
        public void SaveShowTime()
        {
            saveToRegistry(SHOW_TIME, Convert.ToString(ShowTime));
        }

        /// <summary>
        /// Persistate current value of ShowTextBackground to registry
        /// </summary>
        public void SaveShowTextBackground()
        {
            saveToRegistry(SHOW_TEXT_BACKGROUND, Convert.ToString(ShowTextBackground));
        }

        /// <summary>
        /// Persistate current value of RunInMode
        /// </summary>
        public void SaveRunInMode()
        {
            saveToRegistry(RUN_IN_MODE, Convert.ToString(RunInMode));
        }

        /// <summary>
        /// Persistate current value of GTFRandom
        /// </summary>
        public void SaveGTFRandom()
        {
            saveToRegistry(GTF_RANDOM_NEXT_FOLDER, Convert.ToString(GTFRandomNextFolder));
        }


        /// <summary>
        /// Persistate current value of BestInterpolation
        /// </summary>
        public void SaveBestInterpolation()
        {
            saveToRegistry(INTERPOLATION, Convert.ToString(BestInterpolation));
        }

        /// <summary>
        /// Persistate current value of Check4Updates
        /// </summary>
        public void SaveCheck4Updates()
        {
            saveToRegistry(CHECK4UPDATES, Convert.ToString(Check4Updates));
        }

        /// <summary>
        /// Persistate smooth hide of image
        /// </summary>
        public void SaveSmoothHideOfImage()
        {
            saveToRegistry(SMOOTH_HIDE_OF_IMAGE, Convert.ToString(SmoothHidingEnabled));
        }

        /// <summary>
        /// Persistate exit only with excape
        /// </summary>
        public void SaveExitOnlyWithEscape()
        {
            saveToRegistry(EXIT_ONLY_WITH_ESCAPE, Convert.ToString(ExitOnlyWithEscape));
        }

        /// <summary>
        /// Interval between images
        /// </summary>
        public int Interval
        {
            get
            {
                string value = loadFromRegistry(INTERVAL);
                int result = DEFAULT_INTERVAL;
                try
                {
                    result = Convert.ToInt32(value);
                }
                catch (Exception e)
                {
                    logger.Fatal("Can't convert interval " + value + " to number", e);
                    result = DEFAULT_INTERVAL;
                    saveToRegistry(INTERVAL, result);
                }

                if (result < MINIMAL_INTERVAL)
                {
                    logger.Error("Loaded interval has too small value: " + result + "! Minimal value will be used " + MINIMAL_INTERVAL);
                    result = MINIMAL_INTERVAL;
                    saveToRegistry(INTERVAL, result);
                }

                return result;
            }
            set
            {
                if (value < MINIMAL_INTERVAL)
                {
                    logger.Error("Interval has to small value: " + value + "! Minimal value will be stored " + MINIMAL_INTERVAL);
                    value = MINIMAL_INTERVAL;

                }
                saveToRegistry(INTERVAL, value);
            }
        }

        List<string> _imageRootFolders = null;
        /// <summary>
        /// Kořenové adresáře, ve kterých se mají rekurzivně vyhledávat fotky
        /// </summary>
        public List<string> ImagesRootFolders
        {
            get
            {
                if (_imageRootFolders == null)
                {
                    _imageRootFolders = new List<string>();
                    string countStr = loadFromRegistry(IMAGES_ROOT_FOLDERS_COUNT);
                    try
                    {
                        if (countStr != null)
                        {
                            int count = Convert.ToInt32(countStr);
                            if (count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    var rootFolder = loadFromRegistry(IMAGES_ROOT_FOLDER + Convert.ToString(i + 1));

                                    if (rootFolder != null)
                                    {
                                        _imageRootFolders.Add(rootFolder);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't load image folders", e);
                        //WindowsLogWriter.WriteLog("LoadImagesRootFolderst - Can't load image folders " + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                    //pokud se z nějakého důvodu napodařilo načíst kořenové adresáře, tak použij MyPictures
                    if (_imageRootFolders.Count == 0)
                    {
                        logger.Warn("No root folder in settings - user's MyPictures will be used");
                        _imageRootFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                    }
                }
                return _imageRootFolders;
            }
            set
            {
                _imageRootFolders = value;
                var count = 0;
                if (value != null && value.Count > 0)
                {
                    for (int i = 0; i < _imageRootFolders.Count; i++)
                    {
                        saveToRegistry(IMAGES_ROOT_FOLDER + Convert.ToString(i + 1), _imageRootFolders[i]);
                    }
                    count = _imageRootFolders.Count;
                }
                //clear old values in the registry (next 100)
                for (int i = count + 1; i < count + 100; i++)
                {
                    removeFromRegistry(IMAGES_ROOT_FOLDER + Convert.ToString(i));
                }
                saveToRegistry(IMAGES_ROOT_FOLDERS_COUNT, count);
            }
        }

        /// <summary>
        /// Název souboru s cestami k fotkám
        /// </summary>
        public string FileWithImagePaths
        {
            get
            {
                string file = loadFromRegistry(FILE_WITH_IMAGE_PATHS);

                if (null == file)
                    file = "";

                return file;
            }
            set
            {
                if (null == value || !File.Exists(value))
                {
                    value = "";
                }
                saveToRegistry(FILE_WITH_IMAGE_PATHS, value);
            }
        }

        private List<string> _dontShowFolders = null;
        /// <summary>
        /// Adresáře, které nemají být promítány
        /// </summary>
        public List<string> DontShowFolders
        {
            get
            {
                if (_dontShowFolders == null)
                {
                    _dontShowFolders = new List<string>();
                    string countStr = loadFromRegistry(DONT_SHOW_FOLDERS_COUNT);
                    try
                    {
                        if (countStr != null)
                        {
                            int count = Convert.ToInt32(countStr);
                            if (count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    var folder = loadFromRegistry(DONT_SHOW_FOLDER + Convert.ToString(i + 1));
                                    if (folder != null)
                                    {
                                        _dontShowFolders.Add(folder);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't load image folders", e);
                        //WindowsLogWriter.WriteLog("LoadDontShowFolders - Can't load image folders " + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                return _dontShowFolders;
            }
            set
            {
                _dontShowFolders = value;
                var count = 0;
                if (value != null && value.Count > 0)
                {
                    for (int i = 0; i < _dontShowFolders.Count; i++)
                    {
                        saveToRegistry(DONT_SHOW_FOLDER + Convert.ToString(i + 1), _dontShowFolders[i]);
                    }
                    count = _dontShowFolders.Count;
                }

                //clear old values in the registry (next 100)
                for (int i = count + 1; i < count + 100; i++)
                {
                    removeFromRegistry(DONT_SHOW_FOLDER + Convert.ToString(i));
                }
                saveToRegistry(DONT_SHOW_FOLDERS_COUNT, count);
            }
        }


        private List<string> _dontShowImages = null;
        /// <summary>
        /// Seznam obrázků, které nemají být promítány
        /// </summary>
        public List<string> DontShowImages
        {
            get
            {
                if (_dontShowImages == null)
                {
                    _dontShowImages = new List<string>();
                    string countStr = loadFromRegistry(DONT_SHOW_IMAGES_COUNT);
                    try
                    {
                        if (countStr != null)
                        {
                            int count = Convert.ToInt32(countStr);
                            if (count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    string folder = loadFromRegistry(DONT_SHOW_IMAGE + Convert.ToString(i + 1));
                                    if (folder != null)
                                    {
                                        _dontShowImages.Add(folder);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't load count of images which shouldn't be shown", e);
                        //WindowsLogWriter.WriteLog("LoadDontShowImages - Can't load image" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                return _dontShowImages;
            }
            set
            {
                _dontShowImages = value;
                var count = 0;
                if (value != null && value.Count > 0)
                {
                    for (int i = 0; i < _dontShowImages.Count; i++)
                    {
                        saveToRegistry(DONT_SHOW_IMAGE + Convert.ToString(i + 1), _dontShowImages[i]);
                    }
                    count = _dontShowImages.Count;
                }
                //clear old values in the registry if any (next 100)
                for (int i = count + 1; i < count + 100; i++)
                {
                    removeFromRegistry(DONT_SHOW_IMAGE + Convert.ToString(i));
                }
                saveToRegistry(DONT_SHOW_IMAGES_COUNT, count);
            }
        }

        /// <summary>
        /// Název adresáře, kam mají být fotky kopírovány na klávesu C
        /// </summary>
        public string CopyToFolder
        {
            get
            {
                string folder = loadFromRegistry(COPY_TO_FOLDER);

                if (null == folder || !Directory.Exists(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    saveToRegistry(COPY_TO_FOLDER, folder);
                }
                return folder;
            }
            set
            {
                saveToRegistry(COPY_TO_FOLDER, value);
            }
        }

        /// <summary>
        /// Název adresáře, kam se mají ukládat soubory, obsahující uložení cesty k obrázkům
        /// </summary>
        public string SavePathToFolder
        {
            get
            {
                string folder = loadFromRegistry(SAVE_PATH_TO_FOLDER);

                if (null == folder || !Directory.Exists(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    saveToRegistry(SAVE_PATH_TO_FOLDER, folder);
                }
                return folder;
            }
            set
            {
                if (null == value || !Directory.Exists(value))
                {
                    value = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                }
                saveToRegistry(SAVE_PATH_TO_FOLDER, value);
            }
        }

        /// <summary>
        /// Název souboru, do kterého se mají ukládat názvy obrázků na klávesu F1
        /// </summary>
        public string SavePathToFileF1
        {
            get
            {
                string file = loadFromRegistry(SAVE_PATH_TO_FILE_F1);

                if (String.IsNullOrEmpty(file))
                    file = DEFAULT_SAVE_PATH_TO_FILE_F1;

                return file;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    value = DEFAULT_SAVE_PATH_TO_FILE_F1;
                if (!File.Exists(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION))
                {
                    try
                    {
                        File.CreateText(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION, e);
                        //WindowsLogWriter.WriteLog("SavePathToFileF1.Set - Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION + " :" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                saveToRegistry(SAVE_PATH_TO_FILE_F1, value);
            }
        }

        /// <summary>
        /// Název souboru, do kterého se mají ukládat názvy obrázků na klávesu F2
        /// </summary>
        public string SavePathToFileF2
        {
            get
            {
                string file = loadFromRegistry(SAVE_PATH_TO_FILE_F2);

                if (String.IsNullOrEmpty(file))
                    file = DEFAULT_SAVE_PATH_TO_FILE_F2;

                return file;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    value = DEFAULT_SAVE_PATH_TO_FILE_F2;
                if (!File.Exists(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION))
                {
                    try
                    {
                        File.CreateText(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION, e);
                        //WindowsLogWriter.WriteLog("SavePathToFileF2.Set - Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION + " :" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                saveToRegistry(SAVE_PATH_TO_FILE_F2, value);
            }
        }

        /// <summary>
        /// Název souboru, do kterého se mají ukládat názvy obrázků na klávesu F3
        /// </summary>
        public string SavePathToFileF3
        {
            get
            {
                string file = loadFromRegistry(SAVE_PATH_TO_FILE_F3);

                if (String.IsNullOrEmpty(file))
                    file = DEFAULT_SAVE_PATH_TO_FILE_F3;

                return file;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    value = DEFAULT_SAVE_PATH_TO_FILE_F3;
                if (!File.Exists(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION))
                {
                    try
                    {
                        File.CreateText(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION, e);
                        //WindowsLogWriter.WriteLog("SavePathToFileF3.Set - Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION + " :" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                saveToRegistry(SAVE_PATH_TO_FILE_F3, value);
            }
        }

        /// <summary>
        /// Název souboru, do kterého se mají ukládat názvy obrázků na klávesu F4
        /// </summary>
        public string SavePathToFileF4
        {
            get
            {
                string file = loadFromRegistry(SAVE_PATH_TO_FILE_F4);

                if (String.IsNullOrEmpty(file))
                    file = DEFAULT_SAVE_PATH_TO_FILE_F4;

                return file;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    value = DEFAULT_SAVE_PATH_TO_FILE_F4;
                if (!File.Exists(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION))
                {
                    try
                    {
                        File.CreateText(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION, e);
                        //WindowsLogWriter.WriteLog("SavePathToFileF4.Set - Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION + " :" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                saveToRegistry(SAVE_PATH_TO_FILE_F4, value);
            }
        }

        /// <summary>
        /// Název souboru, do kterého se mají ukládat názvy obrázků na klávesu F5
        /// </summary>
        public string SavePathToFileF5
        {
            get
            {
                string file = loadFromRegistry(SAVE_PATH_TO_FILE_F5);

                if (String.IsNullOrEmpty(file))
                    file = DEFAULT_SAVE_PATH_TO_FILE_F5;

                return file;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    value = DEFAULT_SAVE_PATH_TO_FILE_F5;
                if (!File.Exists(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION))
                {
                    try
                    {
                        File.CreateText(SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION, e);
                        //WindowsLogWriter.WriteLog("SavePathToFileF5.Set - Can't create text file " + SavePathToFolder + @"\" + value + SAVE_PATH_FILES_EXTENSION + " :" + e.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                saveToRegistry(SAVE_PATH_TO_FILE_F5, value);
            }
        }

        private Color? _backgroundColor = null;
        /// <summary>
        /// Barva pozadí obrázků
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                if (_backgroundColor == null)
                {
                    string color = loadFromRegistry(BACKGROUND_COLOR);
                    if (null == color)
                    {
                        logger.Warn("Settings doesn't contains background color - default color will be used and saved");
                        _backgroundColor = DEFAULT_BACKGROUND_COLOR;
                        saveToRegistry(BACKGROUND_COLOR, ((Color)_backgroundColor).ToArgb());
                    }
                    else
                    {
                        try
                        {
                            ColorConverter conv = new ColorConverter();
                            _backgroundColor = Color.FromArgb(Convert.ToInt32(color));
                        }
                        catch (Exception e)
                        {
                            logger.Fatal("Can't load stored background color - default color will be used and saved", e);
                            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
                            saveToRegistry(BACKGROUND_COLOR, ((Color)_backgroundColor).ToArgb());
                        }
                    }
                }
                return (Color)_backgroundColor;
            }
            set
            {
                _backgroundColor = value;
                saveToRegistry(BACKGROUND_COLOR, ((Color)_backgroundColor).ToArgb());
            }
        }

        private int? _fontSize;
        /// <summary>
        /// Velikost fontu na obrazovce
        /// </summary>    
        public int FontSize
        {
            get
            {
                if (_fontSize == null)
                {
                    string value = loadFromRegistry(FONT_SIZE);

                    try
                    {
                        _fontSize = Convert.ToInt32(value);
                    }
                    catch (Exception e)
                    {
                        logger.Fatal("Can't load font size - default font size will be used and saved", e);
                        _fontSize = DEFAULT_FONT_SIZE;
                        saveToRegistry(FONT_SIZE, (int)_fontSize);
                    }

                    if (_fontSize < 8)
                    {
                        logger.Error("Stored font size is too small - default font size will be used and saved");
                        _fontSize = DEFAULT_FONT_SIZE;
                        saveToRegistry(FONT_SIZE, (int)_fontSize);
                    }
                }
                return (int)_fontSize;
            }
            set
            {
                if (value < 1)
                    value = 1;
                _fontSize = value;
                saveToRegistry(FONT_SIZE, (int)_fontSize);
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        private ApplicationState()
        {
            logger.Info("Application settings loading...");
            shownFileInfoList = new List<FileInfo>();

            //initialize ShowFileName property from registry value
            string value = loadFromRegistry(SHOW_FILE_NAME);
            if (null == value)
            {
                ShowFileName = DEFAULT_SHOW_FILE_NAME;
                saveToRegistry(SHOW_FILE_NAME, Convert.ToString(ShowFileName));
            }
            else
                try
                {
                    ShowFileName = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ShowFileName = DEFAULT_SHOW_FILE_NAME;
                    saveToRegistry(SHOW_FILE_NAME, Convert.ToString(ShowFileName));
                }

            //initialize ShowDate property from registry value
            value = loadFromRegistry(SHOW_DATE);
            if (null == value)
            {
                ShowDate = DEFAULT_SHOW_DATE;
                saveToRegistry(SHOW_DATE, Convert.ToString(ShowDate));
            }
            else
                try
                {
                    ShowDate = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ShowDate = DEFAULT_SHOW_DATE;
                    saveToRegistry(SHOW_DATE, Convert.ToString(ShowDate));
                }

            //initialize RotateByExif
            value = loadFromRegistry(ROTATE_BY_EXIF);
            if (null == value)
            {
                RotateByExif = DEFAULT_ROTATE_BY_EXIF;
                saveToRegistry(ROTATE_BY_EXIF, Convert.ToString(RotateByExif));
            }
            else
                try
                {
                    RotateByExif = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    RotateByExif = DEFAULT_ROTATE_BY_EXIF;
                    saveToRegistry(ROTATE_BY_EXIF, Convert.ToString(RotateByExif));
                }

            //initialize ShowExif
            value = loadFromRegistry(SHOW_EXIF);
            if (null == value)
            {
                ShowExif = DEFAULT_SHOW_EXIF;
                saveToRegistry(SHOW_EXIF, Convert.ToString(ShowExif));
            }
            else
                try
                {
                    ShowExif = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ShowExif = DEFAULT_SHOW_EXIF;
                    saveToRegistry(SHOW_EXIF, Convert.ToString(ShowExif));
                }


            //initialize ShowTime
            value = loadFromRegistry(SHOW_TIME);
            if (null == value)
            {
                ShowTime = DEFAULT_SHOW_TIME;
                saveToRegistry(SHOW_TIME, Convert.ToString(ShowTime));
            }
            else
                try
                {
                    ShowTime = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ShowTime = DEFAULT_SHOW_TIME;
                    saveToRegistry(SHOW_TIME, Convert.ToString(ShowTime));
                }

            //initialize ShowTextBackground
            value = loadFromRegistry(SHOW_TEXT_BACKGROUND);
            if (null == value)
            {
                ShowTextBackground = DEFAULT_SHOW_TEXT_BACKGROUND;
                saveToRegistry(SHOW_TEXT_BACKGROUND, Convert.ToString(ShowTextBackground));
            }
            else
                try
                {
                    ShowTextBackground = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ShowTextBackground = DEFAULT_SHOW_TEXT_BACKGROUND;
                    saveToRegistry(SHOW_TEXT_BACKGROUND, Convert.ToString(ShowTextBackground));
                }

            //initialize Run In Mode
            value = loadFromRegistry(RUN_IN_MODE);
            if (null == value)
            {
                RunInMode = DEFAULT_RUN_IN_MODE;
                saveToRegistry(RUN_IN_MODE, Convert.ToString(RunInMode));
            }
            else
                try
                {
                    RunInMode = Convert.ToInt32(value);
                }
                catch (Exception)
                {
                    RunInMode = DEFAULT_RUN_IN_MODE;
                    saveToRegistry(RUN_IN_MODE, Convert.ToString(RunInMode));
                }

            //initialize Last Mode
            value = loadFromRegistry(LAST_MODE);
            if (null == value)
            {
                LastMode = DEFAULT_LAST_MODE;
                saveToRegistry(LAST_MODE, Convert.ToString(LastMode));
            }
            else
                try
                {
                    LastMode = Convert.ToInt32(value);
                }
                catch (Exception)
                {
                    LastMode = DEFAULT_LAST_MODE;
                    saveToRegistry(LAST_MODE, Convert.ToString(LastMode));
                }

            //pokud se má spustit GTF, nebo stejné jako posledně a to bylo GTF, nastav GTF
            if ((RunInMode == 2) || (RunInMode == 0 && LastMode == 2))
                GoThroughFolder = true;


            //initialize GTFRandomNextFolder
            value = loadFromRegistry(GTF_RANDOM_NEXT_FOLDER);
            if (null == value)
            {
                GTFRandomNextFolder = DEFAULT_GTF_RANDOM_NEXT_FOLDER;
                saveToRegistry(GTF_RANDOM_NEXT_FOLDER, Convert.ToString(GTFRandomNextFolder));
            }
            else
                try
                {
                    GTFRandomNextFolder = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    GTFRandomNextFolder = DEFAULT_GTF_RANDOM_NEXT_FOLDER;
                    saveToRegistry(GTF_RANDOM_NEXT_FOLDER, Convert.ToString(GTFRandomNextFolder));
                }

            //initialize interpolation
            value = loadFromRegistry(INTERPOLATION);
            if (null == value)
            {
                BestInterpolation = DEFAULT_INTERPOLATION;
                saveToRegistry(INTERPOLATION, Convert.ToString(BestInterpolation));
            }
            else
                try
                {
                    BestInterpolation = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    BestInterpolation = DEFAULT_INTERPOLATION;
                    saveToRegistry(INTERPOLATION, Convert.ToString(BestInterpolation));
                }

            //initialize check4updates
            value = loadFromRegistry(CHECK4UPDATES);
            if (null == value)
            {
                Check4Updates = DEFAULT_CHECK4UPDATES;
                saveToRegistry(CHECK4UPDATES, Convert.ToString(Check4Updates));
            }
            else
                try
                {
                    Check4Updates = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    Check4Updates = DEFAULT_CHECK4UPDATES;
                    saveToRegistry(CHECK4UPDATES, Convert.ToString(Check4Updates));
                }

            //initialize smooth hide of image
            value = loadFromRegistry(SMOOTH_HIDE_OF_IMAGE);
            if (null == value)
            {
                SmoothHidingEnabled = DEFAULT_SMOOTH_HIDE_OF_IMAGE;
                saveToRegistry(SMOOTH_HIDE_OF_IMAGE, Convert.ToString(SMOOTH_HIDE_OF_IMAGE));
            }
            else
                try
                {
                    SmoothHidingEnabled = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    SmoothHidingEnabled = DEFAULT_SMOOTH_HIDE_OF_IMAGE;
                    saveToRegistry(SMOOTH_HIDE_OF_IMAGE, Convert.ToString(SmoothHidingEnabled));
                }

            //initialize exitonlywithescape
            value = loadFromRegistry(EXIT_ONLY_WITH_ESCAPE);
            if (null == value)
            {
                ExitOnlyWithEscape = DEFAULT_EXIT_ONLY_WITH_ESCAPE;
                saveToRegistry(EXIT_ONLY_WITH_ESCAPE, Convert.ToString(ExitOnlyWithEscape));
            }
            else
                try
                {
                    ExitOnlyWithEscape = Convert.ToBoolean(value);
                }
                catch (Exception)
                {
                    ExitOnlyWithEscape = DEFAULT_EXIT_ONLY_WITH_ESCAPE;
                    saveToRegistry(EXIT_ONLY_WITH_ESCAPE, Convert.ToString(ExitOnlyWithEscape));
                }
        }

        /// <summary>
        /// Singleton Instance() metoda
        /// </summary>
        /// <returns></returns>
        public static ApplicationState getInstance()
        {
            if (null == instance)
                instance = new ApplicationState();
            return instance;
        }

        /// <summary>
        /// Uloží aktuální mode do registru
        /// </summary>
        internal void saveCurrentMode()
        {
            if (GoThroughFolder)
                saveToRegistry(LAST_MODE, 2);
            else
                saveToRegistry(LAST_MODE, 1);

        }
        /// <summary>
        /// Loads history of images
        /// </summary>
        /// <returns></returns>
        public List<FileInfo> loadHistory()
        {
            List<FileInfo> history = new List<FileInfo>();
            logger.Debug("History loading");
            for (int i = LOADED_HISTORY_SIZE; i > 0; i--)
            {
                string path = loadFromRegistry(HISTORY + i);
                if (!String.IsNullOrEmpty(path) && File.Exists(path))
                    history.Add(new FileInfo(path));
            }
            return history;
        }

        /// <summary>
        /// Save history to the registry
        /// </summary>
        /// <param name="history"></param>
        public void saveHistory(List<FileInfo> history)
        {
            logger.Debug("History cleaning");
            for (int i = LOADED_HISTORY_SIZE; i > 0; i--)
                removeFromRegistry(HISTORY + i);

            logger.Debug("History saving");
            for (int i = 0; i < history.Count; i++)
                saveToRegistry(HISTORY + Convert.ToString(i + 1), history[i].FullName);
        }

        /// <summary>
        /// Store in Registry
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void saveToRegistry(string name, string value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);
            key.SetValue(name, value);
            key.Close();
            logger.Debug("Stored '" + name + "' = '" + value + "'");
        }

        /// <summary>
        /// Store in Registry
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void saveToRegistry(string name, int value)
        {
            saveToRegistry(name, Convert.ToString(value));
        }

        /// <summary>
        /// Read from Registry
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string loadFromRegistry(string name)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
            string result = null;
            ; if (key != null)
            {
                result = (string)key.GetValue(name);
                key.Close();
            }
            logger.Debug("Loaded '" + name + "' = '" + result + "'");
            return result;
        }

        /// <summary>
        /// Removes key from registry
        /// </summary>
        /// <param name="name"></param>
        private void removeFromRegistry(string name)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);
            if (key != null)
            {
                foreach (String valueName in key.GetValueNames())
                    if (valueName.Equals(name))
                        key.DeleteValue(valueName);
                key.Close();
                logger.Debug("Deleted setting '" + name + "'");
            }
        }

    }
}
