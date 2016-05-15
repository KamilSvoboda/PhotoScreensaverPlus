using NLog;
using PhotoScreensaverPlus.Draw;
using PhotoScreensaverPlus.Logging;
using PhotoScreensaverPlus.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PhotoScreensaverPlus.FilesAndFolders
{
    class DirectoryHelper
    {
        private ApplicationState state;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public DirectoryHelper(ApplicationState state)
        {
            this.state = state;
        }

        /// <summary>
        /// Vytvoří index adresářů, které se budou procházet
        /// </summary>
        /// <param name="dirNames"></param>
        public void BuildDirectoryIndex(List<string> dirNames)
        {
            //pokud už jsou načteny adresáře a pouze došly indexy, které se mají promítat (promítli jsme všechno)
            if (state.directoryInfoList.Count > 0)
            {
                logger.Debug("Directories refreshed");
                //naplníme znovu list indexů, přes který pak vybíráme adresáře z načtených adresářů (viz. níže)
                for (int i = 0; i < state.directoryInfoList.Count; i++)
                    state.directoryInfoIndexes.Add(i);
            }
            else //pokud je potřeba načíst adresáře (typicky při prvním spuštění)
            {
                logger.Debug("Scanning started");

                if (dirNames == null || dirNames.Count == 0)
                {
                    dirNames = state.ImagesRootFolders;
                }

                try
                {
                    List<DirectoryInfo> ds = null;
                    int count = 0;
                    foreach (string dirStr in dirNames)
                    {
                        if (Directory.Exists(dirStr))
                        {
                            DirectoryInfo directory = new DirectoryInfo(dirStr);
                            if (isNotExcludedDirectory(directory) && ((directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                            {
                                logger.Debug(directory.FullName);
                                state.directoryInfoList.Add(directory);
                                ds = GetSubdirectories(directory);
                                state.directoryInfoList.AddRange(ds);
                                count += ds.Count;
                            }
                        }
                    }

                    //naplníme si list indexů, přes který pak vybíráme adresáře
                    for (int i = 0; i < state.directoryInfoList.Count; i++)
                        state.directoryInfoIndexes.Add(i);

                    logger.Debug("Scanning finished with " + count + " folders");
                }
                catch (Exception ex)
                {
                    logger.Fatal("Error while building directory list", ex);
                    //WindowsLogWriter.WriteLog("BuildDirectoryList - Error while building directory list: exception = " + ex.Message, EventLogEntryType.Error);
                    //DrawImage(GenerateErrorBitmap("Error while building image list"), this.CreateGraphics());
                }
            }
        }

        /// <summary>
        /// Rekurzivně sestaví hierarchii adresářů
        /// </summary>
        /// <param name="parentDi"></param>
        /// <returns></returns>
        private List<DirectoryInfo> GetSubdirectories(DirectoryInfo parentDi)
        {
            List<DirectoryInfo> dis = new List<DirectoryInfo>();
            foreach (DirectoryInfo directory in parentDi.GetDirectories())
            {
                if (isNotExcludedDirectory(directory) && ((directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                {
                    dis.Add(directory);
                    dis.AddRange(GetSubdirectories(directory));
                }
                Application.DoEvents(); //aby bylo možné v prostřed načítání z disku ten spořič shodit myší
            }
            return dis;
        }

        /// <summary>
        /// Sestaví seznam obrázků z vybraného adresáře
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="patterns"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public List<FileInfo> getImagesInfosFromDirectory(DirectoryInfo dir, List<string> patterns, SearchOption option)
        {
            List<FileInfo> list = new List<FileInfo>();
            try
            {
                FileInfo[] images;
                foreach (string pattern in patterns)
                {
                    images = dir.GetFiles(pattern);
                    foreach (FileInfo image in images)
                    {
                        Image img = Image.FromFile(image.FullName);
                        if (img.Height >= state.MinDimension & img.Width >= state.MinDimension)
                            list.Add(image);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Fatal("Error while building directory list in directory: '" + dir.FullName + "'", ex);
                //WindowsLogWriter.WriteLog("getImagesInfosFromDirectory - Error while building image list in directory: '" + dir.FullName + "', exception = " + ex.Message, EventLogEntryType.Error);
                return list;
            }

            //Manual searching of subdirectories give a better images order in result list
            if (option == SearchOption.AllDirectories)
            {
                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    list.AddRange(getImagesInfosFromDirectory(d, patterns, option));
                    Application.DoEvents(); //aby bylo možné v prostřed načítání z disku ten spořič shodit myší
                }
            }

            return list;
        }

        /// <summary>
        /// Builds new image list from passed text file
        /// </summary>
        /// <param name="fileName">Name of file containing paths to images</param>
        public void BuildImageListFromFile(String fileName)
        {
            if (!(fileName == null || fileName.Trim().Length == 0 || !File.Exists(fileName)))
            {
                //AppState.toShowFileInfoList.Clear();
                AddImagesToShowFileInfoListFromFile(fileName);
            }
        }

        /// <summary>
        /// Přidá obrázky do seznamu "toShowFileInfoList" - ten se používá pro obrázky, které se mají přednostně promítnout, než se začnou náhodně vybírat fotky z adresářů
        /// Typicky se tam dávají fotky ze souboru, který je v konfiguraci
        /// </summary>
        /// <param name="fileName"></param>
        public void AddImagesToShowFileInfoListFromFile(String fileName)
        {
            String line;
            try
            {
                if (!(fileName == null || fileName.Trim().Length == 0 || !File.Exists(fileName)))
                {
                    StreamReader file = new StreamReader(fileName);

                    while ((line = file.ReadLine()) != null)
                    {
                        if (File.Exists(line))
                        {
                            state.toShowFileInfoList.Add(new FileInfo(line));
                            logger.Debug("Ze souboru přidána cesta: " + line);
                        }
                    }

                    file.Close();
                    file.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Error while building image list", ex);
                //WindowsLogWriter.WriteLog("AddImagesToImagesFileInfoListFromFile - Error while building image list: exception = " + ex.Message, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Provede náhodný výběr adresáře
        /// </summary>
        /// <returns></returns>
        public  DirectoryInfo getRandomDirectory(Random random)
        {
            logger.Debug("Looking for new random folder");
            if (state.directoryInfoIndexes.Count > 0)
            {
                //logger.Debug("Looking for random folder");
                logger.Debug(state.directoryInfoIndexes.Count + " folders remain");
                DirectoryInfo dir = null;
                while (state.directoryInfoIndexes.Count > 0)
                {
                    //náhodně vybereme adresář
                    int i = state.directoryInfoIndexes[random.Next(state.directoryInfoIndexes.Count)]; //náhodně vybereme index z listu
                    dir = state.directoryInfoList[i];
                    logger.Debug(i + " folder selected: '" + dir.FullName + "'");
                    //jeho index vyhodíme, aby se neopakoval (Z každého adresáře se zobrazí jen jedna fotka. Když se postupně vyčerpají všechny adresáře, načtou se znovu a zase se z každýho načítá jedna náhodná fotka
                    state.directoryInfoIndexes.Remove(i);
                    //logger.Debug("Looking for random folder finished: " + dir.FullName);
                    if (getImagesInfosFromDirectory(dir, state.ImagesPatterns, SearchOption.TopDirectoryOnly).Count > 0)
                        break;
                    logger.Debug("No images found!");
                }
                return dir;
            }
            return null;
        }

        /// <summary>
        /// Vrátí true, když není soubor excludovaný
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool isNotExcludedFile(FileInfo file)
        {
            foreach (String f in state.DontShowImages)
            {
                //porovnání case insensitive
                if (string.Compare(f, file.FullName, true) == 0)
                {
                    logger.Debug(file.FullName + " excluded");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Vrátí true, pokud to je adresář, který se může promítat (není mezi excluded)
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool isNotExcludedDirectory(DirectoryInfo dir)
        {
            foreach (String d in state.DontShowFolders)
            {
                if (string.Compare(d, dir.FullName, true) == 0)
                {
                    //logger.Debug(dir.FullName + " excluded");
                    return false;
                }
            }

            if (dir.Parent != null)
                return isNotExcludedDirectory(dir.Parent);
            else
                return true;
        }
    }
}
