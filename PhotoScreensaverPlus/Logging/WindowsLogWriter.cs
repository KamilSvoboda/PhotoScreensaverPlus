using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security;
using PhotoScreensaverPlus.State;

namespace PhotoScreensaverPlus.Logging
{
    static class WindowsLogWriter
    {
        /// <summary>
        /// Write log to Windows system log.
        /// IT solves problem with permitions of normal user under Windows Vista
        /// see http://social.msdn.microsoft.com/forums/en-US/windowsgeneraldevelopmentissues/thread/00a043ae-9ea1-4a55-8b7c-d088a4b08f09/
        /// http://support.microsoft.com/default.aspx?scid=kb;en-us;842795
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        public static void WriteLog(string text, EventLogEntryType type)
        {
            string sourceName = Application.ProductName;
            EventLog eventLog;
            eventLog = new EventLog();
            eventLog.Log = ApplicationState.EVENT_LOG_NAME;
            eventLog.Source = sourceName;

            try //try to write log
            {
                eventLog.WriteEntry(text, type);
            }
            catch
            {
                try
                {
                    try //následující založení source pro eventlog lze založit, pouze pokud aplikaci spustil
                    {   //uživatel s právy administrátora, v opačném případě se vyhodí SecurityException
                        if(!EventLog.SourceExists(sourceName))
                            EventLog.CreateEventSource(sourceName, ApplicationState.EVENT_LOG_NAME);
                    }
                    catch(SecurityException)
                    {
                        //Níže uvedený kód vytvoří Source pro eventlog přes příkazovou řádku programově 
                        //(pokud uživatel nemá práva admin, tak požádá o heslo admina)
                        //ale bohužel založení reference na EventLogMessage.dll není funkční. Logování pak funguje, ale v každé 
                        //hlášce se píše, že tam ta DLL chybí. Šlo by si vytvořit vlastní DLL s hláškami chyb a do toho klíče
                        //vložit cestu. Je to popsané třeba zde:
                        //http://www.codeproject.com/KB/trace/messagetextlog.aspx
                        //http://justgeeks.blogspot.com/2007/10/aspnet-and-eventlog-event-id-issues_1860.html
                        //Je to ale práce, která se mi nechce dělat. Proto to tam dám natvrdo link na DLL z .NET 2.0 (který je aktuální i pro 3.5)

                        // Check whether registry key for source exists
                        string keyName = @"SYSTEM\CurrentControlSet\Services\EventLog\" + ApplicationState.EVENT_LOG_NAME + @"\" + sourceName;
                        RegistryKey rkEventSource = Registry.LocalMachine.OpenSubKey(keyName);

                        string EventLogMessegessDllPath = @" /v EventMessageFile /t REG_EXPAND_SZ /d C:\Windows\System32\PhotoScreensaverPlus.scr";
                        // Check whether keys exists
                        if(rkEventSource == null)
                        {
                            // Key doesnt exist. Create key which represents source
                            Process Proc = new Process();
                            ProcessStartInfo ProcStartInfo = new ProcessStartInfo("Reg.exe");
                            ProcStartInfo.Arguments = @"add ""HKLM\" + keyName + @"""" + EventLogMessegessDllPath;
                            ProcStartInfo.UseShellExecute = true;
                            ProcStartInfo.Verb = "runas";
                            Proc.StartInfo = ProcStartInfo;
                            Proc.Start();
                        }
                        rkEventSource.Close();
                    }
                    eventLog.WriteEntry(text, type);
                }
                catch
                {
                    Debug.Print("Can't write log: " + text);
                }
            }
        }
    }
}
