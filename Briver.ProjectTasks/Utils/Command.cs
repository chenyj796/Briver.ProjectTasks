using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Briver.ProjectTasks.Utils
{
    internal static class Command
    {
        /// <summary>
        /// 执行命令，并返回进程的输出
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="arguments">参数</param>
        /// <param name="directory">目录</param>
        /// <returns></returns>
        public static string Execute(string command, string arguments, string directory)
        {
            var info = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            var proc = Process.Start(info);
            proc.WaitForExit();
            return proc.StandardOutput.ReadToEnd().Trim();
        }

    }
}
