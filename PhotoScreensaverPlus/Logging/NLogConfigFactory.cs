using NLog;
using NLog.Config;
using NLog.Targets;
using System;

namespace PhotoScreensaverPlus.Logging
{
    /// <summary>
    /// Class building NLog configuration
    /// </summary>
    class NLogConfigFactory
    {
        public static void BuildConfig()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.FileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/PhotoScreensaverPlus/logs/${shortdate}.log";
            fileTarget.Layout = @"${date:format=HH\:mm\:ss.fff} ${uppercase:${level}} ${callsite:className=false:fileName=true:includeSourcePath=false:methodName=true} ${message}";
            fileTarget.ArchiveFileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/PhotoScreensaverPlus/logs/{#}.txt";
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Date;
            fileTarget.ArchiveDateFormat = "yyyyMMdd";
            fileTarget.MaxArchiveFiles = 30;

            LoggingRule rule1 = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule1);

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            //consoleTarget.Layout = @"${longdate} ${logger} ${message}";
            consoleTarget.Layout = @"${date:format=HH\:mm\:ss.fff} ${message}";

            LoggingRule rule2 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule2);

            LogManager.Configuration = config;

            //// Example usage
            //Logger logger = LogManager.GetLogger("*");
            //logger.Trace("trace log message");
        }
    }
}
