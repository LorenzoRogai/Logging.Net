using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoggingNet;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create a new logging with our custom format
            Logging MyLog = new Logging("[%loglevel][%dd/%MM/%yy][%hh:%mm:%ss]%{cyan}[%class.%method] > %{red}%text");
            //the MinimumLogLevel to display the line
            MyLog.MinimumLogLevel = LogLevel.Debug;
            //Name of a file where logs will be stored
            MyLog.SetLogFile("mylog.txt");
            //Mysql Query Format
            string QueryFormat = "insert into mylog values ('%loglevel','%class','%method','%text')";
            //Mysql Connection parameters
            MyLog.SetLogDatabase("127.0.0.1", 3306, "root", "123456", "test", QueryFormat);
            MyLog.WriteLine("log", LogLevel.Debug);
            MyLog.WriteLine("information", LogLevel.Information);
            MyLog.WriteLine("warning", LogLevel.Warning);
            MyLog.WriteLine("error", LogLevel.Error);
            MyLog.WriteLine("fatal", LogLevel.Fatal);
            //Change Success LogLevel Default Color
            MyLog.ChangeLogLevelColor(LogLevel.Success, ConsoleColor.DarkGreen); 
            MyLog.WriteLine("success", LogLevel.Success);

            Logging ExceptionLog = new Logging("[%dd/%MM/%yy][%hh:%mm:%ss][%class.%method] > %text");
            ExceptionLog.MinimumLogLevel = LogLevel.Error;
            ExceptionLog.SetLogFile("exceptionlog.txt");
            string QueryFormat2 = "insert into exception_log values ('%timestamp','%class','%method','%text')";
            ExceptionLog.SetLogDatabase("127.0.0.1", 3306, "root", "123456", "test", QueryFormat2);
            ExceptionLog.WriteLine("Exception: ...", LogLevel.Error);         
           
            Console.ReadLine();
        }
    }
}
