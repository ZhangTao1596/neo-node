// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using System;
using System.IO;
using System.Text;
using static System.IO.Path;

namespace Neo.CLI
{
    partial class MainService
    {
        private static readonly ConsoleColorSet DebugColor = new ConsoleColorSet(ConsoleColor.Cyan);
        private static readonly ConsoleColorSet InfoColor = new ConsoleColorSet(ConsoleColor.White);
        private static readonly ConsoleColorSet WarningColor = new ConsoleColorSet(ConsoleColor.Yellow);
        private static readonly ConsoleColorSet ErrorColor = new ConsoleColorSet(ConsoleColor.Red);
        private static readonly ConsoleColorSet FatalColor = new ConsoleColorSet(ConsoleColor.Red);

        private readonly object syncRoot = new();

        private void Initialize_Logger()
        {
            Utility.Logging += OnLog;
        }

        private void Dispose_Logger()
        {
            Utility.Logging -= OnLog;
        }

        private static void GetErrorLogs(StringBuilder sb, Exception ex)
        {
            sb.AppendLine(ex.GetType().ToString());
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            if (ex is AggregateException ex2)
            {
                foreach (Exception inner in ex2.InnerExceptions)
                {
                    sb.AppendLine();
                    GetErrorLogs(sb, inner);
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine();
                GetErrorLogs(sb, ex.InnerException);
            }
        }

        private void OnLog(string source, LogLevel level, object message)
        {
            if (!Settings.Default.Logger.Active)
                return;

            if (message is Exception ex)
            {
                var sb = new StringBuilder();
                GetErrorLogs(sb, ex);
                message = sb.ToString();
            }

            lock (syncRoot)
            {
                DateTime now = DateTime.Now;
                var log = $"[{now.TimeOfDay:hh\\:mm\\:ss\\.fff}] {message}";

                if (Settings.Default.Logger.ConsoleOutput)
                {
                    var currentColor = new ConsoleColorSet();

                    switch (level)
                    {
                        case LogLevel.Debug: DebugColor.Apply(); break;
                        case LogLevel.Error: ErrorColor.Apply(); break;
                        case LogLevel.Fatal: FatalColor.Apply(); break;
                        case LogLevel.Info: InfoColor.Apply(); break;
                        case LogLevel.Warning: WarningColor.Apply(); break;
                    }

                    Console.WriteLine(log);
                    currentColor.Apply();
                }

                if (!string.IsNullOrEmpty(Settings.Default.Logger.Path))
                {
                    StringBuilder sb = new StringBuilder(source);
                    foreach (char c in GetInvalidFileNameChars())
                        sb.Replace(c, '-');
                    var path = Combine(Settings.Default.Logger.Path, sb.ToString());
                    Directory.CreateDirectory(path);
                    path = Combine(path, $"{now:yyyy-MM-dd}.log");
                    try
                    {
                        File.AppendAllLines(path, new[] { $"[{level}]{log}" });
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Error writing the log file: " + path);
                    }
                }
            }
        }
    }
}
