using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TractorServer
{
    public class LoggerUtil
    {
        public static ILog Setup(string logName, string logFilePath)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Threshold = Level.Debug;

            // Configure logger
            var loggerRaw = hierarchy.LoggerFactory.CreateLogger(hierarchy, logName);
            loggerRaw.Hierarchy = hierarchy;
            loggerRaw.AddAppender(CreateFileAppender(logName, logFilePath));
            loggerRaw.Repository.Configured = true;
            loggerRaw.Level = Level.Debug;

            ILog logger = new LogImpl(loggerRaw);
            return logger;
        }

        private static IAppender CreateFileAppender(string name, string fileName)
        {
            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date %level %logger: %message%newline";
            patternLayout.ActivateOptions();

            RollingFileAppender appender = new RollingFileAppender();
            appender.Name = name;
            appender.File = fileName;
            appender.AppendToFile = true;
            appender.MaxSizeRollBackups = 10;
            appender.RollingStyle = RollingFileAppender.RollingMode.Size;
            appender.MaximumFileSize = "2MB";
            appender.Layout = patternLayout;
            appender.LockingModel = new FileAppender.MinimalLock();
            appender.StaticLogFileName = true;
            appender.ActivateOptions();
            return appender;
        }
    }
}