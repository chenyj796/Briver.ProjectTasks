using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Briver.Utils
{
    public static class Command
    {
        public class Options
        {
            public string Command { get; set; }
            public string Arguments { get; set; }
            public string Directory { get; set; }
            public int? Timeout { get; set; }
        }
        private class FailedException : Exception
        {
            public FailedException(string message) : base(message)
            {
            }
        }
        /// <summary>
        /// 执行命令，并返回进程的输出
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="arguments">参数</param>
        /// <param name="directory">目录</param>
        /// <returns></returns>
        public static string Execute(Options options)
        {
            var info = new ProcessStartInfo
            {
                FileName = options.Command,
                Arguments = options.Arguments,
                WorkingDirectory = options.Directory,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            var proc = new Process();
            proc.StartInfo = info;
            proc.EnableRaisingEvents = true;
            proc.ErrorDataReceived += (o, e) =>
            {
                throw new FailedException(e.Data);
            };

            try
            {
                proc.Start();

                var timeout = -1;
                if (options.Timeout.HasValue)
                {
                    timeout = (int)options.Timeout.Value;
                }
                proc.WaitForExit(timeout);

                return proc.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception ex)
            {
                throw new FailedException(ex.Message);
            }
        }

    }
}
