using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Briver.ProjectTasks.Utils;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Briver.ProjectTasks
{
    public class InformationVersion : Task
    {
        private const string FileName = "InformationVersion.cs";

        /// <summary>
        /// 项目文件夹
        /// </summary>
        public string ProjectDir { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// 程序集名
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// 输出消息
        /// </summary>
        [Output]
        public string OutputMessage { get; set; }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            try
            {
                var propertiesDir = Path.Combine(ProjectDir, "Properties");
                Directory.CreateDirectory(propertiesDir);

                var information = BuildInformation(ProjectDir);
                information = $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{information}\")]";

                var assembly = Assembly.GetExecutingAssembly();
                var resource = $"{this.GetType().Namespace}.ReadMe.txt";
                var outputFile = Path.Combine(propertiesDir, FileName);
                using (var stream = assembly.GetManifestResourceStream(resource))
                using (var reader = new StreamReader(stream))
                {
                    var readme = reader.ReadToEnd();
                    File.WriteAllLines(outputFile, new[] { readme, information });
                }
                this.OutputMessage = $"{ProjectName}: 已重新生成{outputFile}";
            }
            catch (Exception ex)
            {
                this.Log.LogWarningFromException(ex);
            }

            return true;
        }

        /// <summary>
        /// 生成InformationVersion的内容
        /// </summary>
        /// <param name="project">项目文件夹</param>
        /// <returns></returns>
        private string BuildInformation(string project)
        {
            var repository = project;
            while (repository != null)
            {
                var git = Path.Combine(repository, ".git");
                if (!Directory.Exists(git))
                {
                    repository = Path.GetDirectoryName(repository);
                    continue;
                }

                var time = DateTime.Now.ToString("yyyy/MM/dd");
                var name = Command.Execute("git", "symbolic-ref --short -q HEAD", repository);
                var hash = Command.Execute("git", "rev-parse HEAD", repository);
                var addr = Command.Execute("git", "remote get-url --all origin", repository);
                if (string.IsNullOrEmpty(addr))
                {
                    addr = repository.Replace(@"\", @"\\");
                }
                AmendRepository(repository, hash);

                return $"编译({time}) 分支({name}) 提交({hash}) 仓库({addr})";
            }

            throw new Exception($"未找到.git目录，无法生成版本信息");
        }

        private void AmendRepository(string repository, string hash)
        {
            using (var mutex = new Mutex(true, hash, out var created))
            {
                // 如果创建新互斥体失败，说明已经存在同名互斥体，则等待互斥体可用
                if (!created) { mutex.WaitOne(); }

                try
                {
                    var file = Path.Combine(repository, ".git", nameof(Briver));
                    if (File.Exists(file)) { return; }

                    File.WriteAllText(file, "请勿删除此文件");

                    // 在.gitignore文件中添加“InformationVersion.cs”
                    var ignore = Path.Combine(repository, ".gitignore");

                    var needRewrite = true;
                    var builder = new StringBuilder();
                    if (File.Exists(ignore))
                    {
                        foreach (var line in File.ReadAllLines(ignore))
                        {
                            if (string.Equals(line, FileName))
                            {
                                needRewrite = false;
                                break;
                            }
                            builder.AppendLine(line);
                        }
                    }
                    if (needRewrite)
                    {
                        builder.AppendLine().AppendLine(FileName);
                        File.WriteAllText(ignore, builder.ToString());
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

    }
}
