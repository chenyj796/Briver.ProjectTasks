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

			public string Template { get; set; }
			public List<Token> Tokens { get; } = new List<Token>();

			public Configuration(XElement config)
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

			public static string BuildInformation(AssemblyInformationTask task)
			{
				var file = Path.Combine(task.ProjectDir, "Properties", "Briver.ProjectTasks.xml");
				if (!File.Exists(file))
				{
					var dir = Path.GetDirectoryName(file);
					Directory.CreateDirectory(dir);//确保文件夹存在

					var source = Path.Combine(task.PackageDir, "Properties", "Briver.ProjectTasks.xml");
					File.Copy(source, file, true);
				}
				var xml = XElement.Load(file).Element(nameof(AssemblyInformationTask));
				if (xml == null)
				{
					throw new InvalidOperationException($"配置文件“{file}”中不包含{nameof(AssemblyInformationTask)}节点");
				}

				var config = new Configuration(xml);
				if (String.IsNullOrEmpty(config.Template))
				{
					throw new InvalidOperationException($"{nameof(AssemblyInformationTask)}配置节点的{nameof(Template)}属性为空");
				}

				var information = config.Template;
				foreach (var token in config.Tokens)
				{
					var content = Command.Execute(new Command.Options
					{
						Command = token.Command,
						Arguments = token.Arguments,
						Directory = task.ProjectDir,
						Timeout = token.Timeout,
					});

					if (!String.IsNullOrEmpty(content))
					{
						information = Regex.Replace(information, $@"\{{{token.Name}\}}", content, RegexOptions.IgnoreCase | RegexOptions.Singleline);
					}
					else
					{
						task.Log.LogWarning($"执行命令“{token.Command} {token.Arguments}”返回空值");
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
				information = information.Replace("\\", "\\\\").Replace("\"", "\\\"");
				information = Regex.Replace(information, "\\s+", " ", RegexOptions.Singleline);
				Log.LogMessage(MessageImportance.High, $"{this.ProjectName}生成程序集版本信息：{information}");

				information = $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{information}\")]";
				File.WriteAllText(this.AttributeFile, information, Encoding.UTF8);

				return true;
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}
			return false;
		}


	}
}
