using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using PhotoScreensaverPlus.State;
using PhotoScreensaverPlus.Forms;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus
{
    static class Program
    {
        private static Mutex applock;

        private delegate void DelegateBuildImagesList(List<string> folders); //for async calling of BuildImagesList

        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            applock = new Mutex(false, "http://pssp.svoboda.biz");

            NLogConfigFactory.BuildConfig();

            MainController mainCl = new MainController();

            //if there is not another instance of the screensaver
            if (applock.WaitOne(0, false))
            {
                if (args.Length > 0)
                {
                    if (args[0].ToLower().Trim().Substring(0, 2) == "/l") //create log source - needs administrators rights!!!
                    {
                        logger.Debug("Create log source");
                        try
                        {
                            if (!EventLog.SourceExists(Application.ProductName))
                                EventLog.CreateEventSource(Application.ProductName, ApplicationState.EVENT_LOG_NAME);
                        }
                        catch (Exception ex)
                        {
                            logger.Fatal("Can't create log source", ex);
                        }
                    }
                    else if (args[0].ToLower().Trim().Substring(0, 2) == "/s") //show
                    {
                        //run the screen saver
                        logger.Info("Screensaver started in standard mode");
                        //LogWriter.WriteLog("Preview showed", EventLogEntryType.Information);

                        mainCl.Start();
                        Application.Run();
                    }
                    else if (args[0].ToLower().Trim().Substring(0, 2) == "/p") //preview
                    {
                        //show the screen saver preview
                        logger.Info("Screensaver started in preview mode");
                        //LogWriter.WriteLog("Preview showed", EventLogEntryType.Information);

                        if (args.Length > 1)
                        {
                            var f = new MainForm(new IntPtr(long.Parse(args[1])), mainCl);
                            f.Show();
                            f.Refresh(); //nevím proč, ale musí tady být kvůli tomu aby se vykreslila ta bitmapa
                            Application.Run(f);
                            //Application.Run(); //args[1] is the handle to the preview window
                        }
                        else
                        {
                            logger.Error("Chybné parametry - chybí identifikace rodičovského okna pro preview");
                        }
                    }
                    else if (args[0].ToLower().Trim().Substring(0, 2) == "/c") //configure
                    {
                        //configure the screen saver
                        logger.Info("Screensaver configuration started");
                        //LogWriter.WriteLog("Settings showed", EventLogEntryType.Information);
                        Application.Run(new SettingsForm());
                    }
                    else if (args[0].ToLower().Trim().Substring(0, 2) == "/f") //folder slideshow mode - slideshow of directory
                    {
                        logger.Info("Screensaver started in folder slideshow mode");
                        if (args.Length > 1)
                        {
                            var dir = args[1].Trim();
                            if (Directory.Exists(dir))
                            {
                                //slideshow je vlastně mod GTF s tím předaným adresářem
                                ApplicationState state = ApplicationState.getInstance();
                                state.IsFolderSlideShowMode = true;
                                /*
                                List<string> folders = new List<string>();
                                folders.Add(dir);
                                 */
                                mainCl.SetGoThroughFolder(new DirectoryInfo(dir), null, SearchOption.AllDirectories);
                                //LogWriter.WriteLog("Slideshow of the directory '" + dir + "' started.", EventLogEntryType.Information);
                            }
                            else
                            {
                                logger.Error("Can't run slideshow of the directory: directory = " + dir + ", directory doesn't exists.");
                                //WindowsLogWriter.WriteLog("Can't run slideshow of the directory: directory = " + dir + ", directory doesn't exists.", EventLogEntryType.Error);
                            }
                        }
                        else
                        {
                            logger.Error("Can't run slideshow of the directory. Directory isn't specified!");
                            //WindowsLogWriter.WriteLog("Can't run slideshow of the directory. Directory isn't specified!", EventLogEntryType.Error);
                        }

                        mainCl.Start();
                        Application.Run();
                    }
                    else if (args[0].ToLower().Trim().Substring(0, 2) == "/d") //debug mode
                    {
                        //run the screen saver
                        logger.Info("Screensaver started in debug mode");
                        //LogWriter.WriteLog("Screensaver started in debug mode", EventLogEntryType.Information);
                        ApplicationState state = ApplicationState.getInstance();
                        state.DebugMode = true;

                        mainCl.Start();
                        Application.Run();
                    }
                    else //unknown argument was passed
                    {
                        //show the screen saver anyway
                        logger.Error("Screensaver started with unknown argument");
                        //WindowsLogWriter.WriteLog("Screensaver started with unknown argument", EventLogEntryType.Information);

                        mainCl.Start();
                        Application.Run();
                    }
                }
                else //no arguments were passed
                {
                    mainCl.Start();
                    Application.Run();
                }
                applock.ReleaseMutex();
                //MainClass.WriteLog("Screensaver stopped", EventLogEntryType.Information);
            }
            else
            {
                logger.Warn("Preventing to run second instance of the screensaver");
                //WindowsLogWriter.WriteLog("Preventing to run second instance of the screensaver", EventLogEntryType.Warning);
                Process.GetCurrentProcess().Kill();
            }
        }

    }
}
