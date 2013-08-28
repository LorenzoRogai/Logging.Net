using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace LoggingNet
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Fatal,
        Success
    }

    public class LogLevelInfo
    {
        public LogLevel LogLevel;
        public ConsoleColor LogLevelColor;
        public LogLevelInfo(LogLevel LogLevel, ConsoleColor LogLevelColor)
        {
            this.LogLevel = LogLevel;
            this.LogLevelColor = LogLevelColor;
        }
    }

    public class Logging
    {
        //the MinimumLogLevel to display the line
        public LogLevel MinimumLogLevel;         
        //List of LogLevelInfo, contains LogLevel and its color
        private List<LogLevelInfo> LogLevelInfos = new List<LogLevelInfo>();
        //Log format
        private string Format;
        //Mysql Query format
        private string LogQueryFormat;
        //Log file stream
        private StreamWriter LogFileStream;
        //Database connection
        private DatabaseManager DatabaseManager;
        //Log format

        /// <summary>
        /// New Logging
        /// </summary>
        /// <param name="Format">Log format</param>
        public Logging(string Format)
        {          
            this.Format = Format;
            //Add ConsoleColor to LogLevel
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Trace, ConsoleColor.Gray));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Debug, ConsoleColor.Gray));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Information, ConsoleColor.Cyan));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Warning, ConsoleColor.Yellow));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Error, ConsoleColor.Red));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Fatal, ConsoleColor.DarkRed));
            LogLevelInfos.Add(new LogLevelInfo(LogLevel.Success, ConsoleColor.Green));
        }

        /// <summary>
        /// Name of a file where logs will be stored
        /// </summary>
        /// <param name="FileName">File name</param>
        public void SetLogFile(string FileName)
        {
            LogFileStream = new StreamWriter(FileName, true);
            LogFileStream.AutoFlush = true;
        }

        /// <summary>
        /// Connect to database
        /// </summary>
        /// <param name="IP">Database ip</param>
        /// <param name="Port">Database port</param>
        /// <param name="DB_Username">Database username</param>
        /// <param name="DB_Password">Database password</param>
        /// <param name="DB_Name">Database name</param>
        /// <param name="LogQueryFormat">Log query format</param>
        public void SetLogDatabase(string IP, uint Port, string DB_Username, string DB_Password, string DB_Name, string LogQueryFormat)
        {
            DatabaseServer dbServer = new DatabaseServer(IP, Port, DB_Username, DB_Password);
            Database db = new Database(DB_Name, 5, 300);
            DatabaseManager = new DatabaseManager(dbServer, db);
            DatabaseManager.GetClient();
            this.LogQueryFormat = LogQueryFormat;
        }

        /// <summary>
        /// Change default log level color
        /// </summary>
        /// <param name="LogLevel">Log level</param>     
        /// <param name="NewColor">The new color</param>
        public void ChangeLogLevelColor(LogLevel LogLevel, ConsoleColor NewColor)
        {
            GetLogLevelInfoByLogLevel(LogLevel).LogLevelColor = NewColor;
        }

        /// <summary>
        /// Format and write the line
        /// </summary>
        /// <param name="Text">Text to write</param>     
        /// <param name="LogLevel">Log level</param>
        public void WriteLine(string Text, LogLevel LogLevel)
        {
            //Skip line if MinimumLogLevel is higher than the given LogLevel 
            if (LogLevel >= MinimumLogLevel)
            {               
                //Temp variable of the log format
                string FinalString = Format;
                //Set the given LogLevel console color
                Console.ForegroundColor = GetLogLevelInfoByLogLevel(LogLevel).LogLevelColor;
          
                //Replace all values
                FinalString = ReplaceValues(FinalString, Text, LogLevel);
          
                //Set ConsoleColor by color pattern
                while (FinalString.IndexOf("%{") > 0)
                {
                    int startIndex = FinalString.IndexOf("%{");
                    int endIndex = FinalString.IndexOf("}");
                    Console.Write(FinalString.Substring(0, startIndex));
                    if (LogFileStream != null)
                    {
                        LogFileStream.Write(FinalString.Substring(0, startIndex));
                    }
                    string ColorName = FinalString.Substring(startIndex + 2, endIndex - (startIndex + 2));
                    ConsoleColor ConsoleColor;
                    if (Enum.TryParse(ColorName, true, out ConsoleColor))
                    {
                        Console.ForegroundColor = ConsoleColor;
                    }
                    FinalString = FinalString.Substring(endIndex + 1);
                }

                Console.WriteLine(FinalString);
                if (LogFileStream != null)
                {
                    //Write Log to file
                    LogFileStream.WriteLine(FinalString);
                    LogFileStream.Flush();
                }
                if (DatabaseManager != null)
                {
                    //Write Log to database
                    string FinalQuery = LogQueryFormat;
                    FinalQuery = ReplaceValues(FinalQuery, Text, LogLevel);
                    DatabaseManager.GetClient().ExecuteQuery(FinalQuery);
                }
            }
        }
        
        private string ReplaceValues(string String, string Text, LogLevel LogLevel)
        {            
            StackFrame _SF = new StackTrace().GetFrame(2);
            //Current Hour
            String = String.Replace("%hh", DateTime.Now.ToString("hh"));
            //Minute            
            String = String.Replace("%mm", DateTime.Now.ToString("mm"));
            //Second
            String = String.Replace("%ss", DateTime.Now.ToString("ss"));
            //Month
            String = String.Replace("%MM", DateTime.Now.ToString("MM"));
            //Day
            String = String.Replace("%dd", DateTime.Now.ToString("dd"));
            //Year
            String = String.Replace("%yy", DateTime.Now.ToString("yy"));
            //Class name where WriteLine has been called
            String = String.Replace("%class", _SF.GetMethod().ReflectedType.Name);
            //method name where WriteLine has been called
            String = String.Replace("%method", _SF.GetMethod().Name);
            //Text
            String = String.Replace("%text", Text);
            //LogLevel name
            String = String.Replace("%loglevel", LogLevel.ToString());
            //User login name
            String = String.Replace("%username", Environment.UserName);
            //Current timestamp
            String = String.Replace("%timestamp", Stopwatch.GetTimestamp().ToString());
            return String;
        }

        /// <summary>
        /// Return LogLevelInfo by LogLevel
        /// </summary>
        /// <param name="LogLevel">Log level</param>     
        private LogLevelInfo GetLogLevelInfoByLogLevel(LogLevel LogLevel)
        {
            foreach (LogLevelInfo LogLevelInfo in LogLevelInfos)
            {
                if (LogLevelInfo.LogLevel == LogLevel)
                {
                    return LogLevelInfo;
                }
            }
            return null;
        }
    }
}
