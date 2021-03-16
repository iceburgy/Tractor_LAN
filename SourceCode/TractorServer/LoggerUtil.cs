using log4net;
using log4net.Appender;
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
        public static ILog Setup(string logFilePath, string logName)
        {
            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = logFilePath;
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 10;
            roller.MaximumFileSize = "10MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();

            ILog log = LogManager.GetLogger(logName);
            var l = (Logger)log.Logger;
            l.AddAppender(roller);

            l.Level = l.Hierarchy.LevelMap["Debug"];
            l.Repository.Configured = true;
            return log;
        }
    }
}