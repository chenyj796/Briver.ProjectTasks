using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Briver.Utils;
using Microsoft.Build.Framework;

namespace Briver
{
    public class AssemblyInformationTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// NuGet包所在的文件夹
        /// </summary>
        public string PackageDir { get; set; }

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
        /// 目标程序集文件的全路径
        /// </summary>
        public string AssemblyFile { get; set; }

        /// <summary>
        /// 生成的包含程序集版本信息的文件
        /// </summary>
        public string AttributeFile { get; set; }

        /// <summary>
        /// 是否写入默认的日志
        /// </summary>
        public bool Logging { get; set; } = true;


        private class Configuration
        {
            public class Token
            {
                public string Name { get; set; }
                public string Command { get; set; }
                public string Arguments { get; set; }
                public int? Timeout { get; set; }

                public Token(XElement config)
                {
                    this.Name = (string)config.Attribute(nameof(Name));
                    this.Command = (string)config.Attribute(nameof(Command));
                    this.Arguments = (string)config.Attribute(nameof(Arguments));
                    this.Timeout = (int?)config.Attribute(nameof(Timeout));
                }

                public bool IsValid()
                {
                    return !String.IsNullOrEmpty(this.Name) && !String.IsNullOrEmpty(this.Command);
                }
            }

            public class Element
            {
                public string Template { get; set; }
                public List<Token> Tokens { get; } = new List<Token>();

                public Element(XElement config)
                {
                    this.Template = (string)config.Attribute(nameof(Template));

                    foreach (var item in config.Elements(nameof(Token)))
                    {
                        var token = new Token(item);
                        if (token.IsValid())
                        {
                            this.Tokens.Add(token);
                        }
                    }
                }

            }

            public static string BuildInformation(AssemblyInformationTask task)
            {
                var file = Path.Combine(task.ProjectDir, "Properties", "Briver.ProjectTasks.xml");
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException($"未找到配置文件{file}");
                }
                var config = XElement.Load(file).Element(nameof(AssemblyInformationTask));

                var element = new Element(config);
                if (String.IsNullOrEmpty(element.Template))
                {
                }

                var information = element.Template;
                foreach (var token in element.Tokens)
                {
                    var content = Command.Execute(new Command.Options
                    {
                        Command = token.Command,
                        Arguments = token.Arguments,
                        Directory = task.ProjectDir
                    });

                    if (!String.IsNullOrEmpty(content))
                    {
                        information = Regex.Replace(information, $@"\{{{token.Name}\}}", content, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    }
                }

                return information;
            }
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            try
            {
                var information = Configuration.BuildInformation(this);
                if (Logging)
                {
                    Log.LogMessage(MessageImportance.Normal, $"{this.ProjectName}生成程序集版本信息：{information}");
                }

                var attribute = information.Replace("\\", "\\\\").Replace("\"", "\\\"");
                attribute = Regex.Replace(attribute, "\\s+", " ", RegexOptions.Singleline);
                attribute = $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{attribute}\")]";
                File.WriteAllText(this.AttributeFile, attribute, Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                if (Logging)
                {
                    Log.LogWarningFromException(ex);
                }
                else { throw; }
            }
            return false;
        }


    }
}
