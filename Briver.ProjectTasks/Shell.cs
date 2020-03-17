using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Briver.Utils;
using Microsoft.Build.Framework;

/// <summary>
/// 执行Powershell命令
/// </summary>
public class Shell : Microsoft.Build.Utilities.Task
{
	/// <summary>
	/// 命令
	/// </summary>
	[Required]
	public string Command { get; set; }

	/// <summary>
	/// 参数
	/// </summary>
	public string Arguments { get; set; }

	/// <summary>
	/// 输出内容
	/// </summary>
	[Output]
	public string Output { get; set; }


	public override bool Execute()
	{
		if (string.IsNullOrEmpty(this.Command))
		{
			this.Log.LogError($"{nameof(Shell)}任务的“{nameof(Command)}”属性为空");
			return false;
		}

		try
		{
			var arguments = "-NonInteractive -NoProfile -NoLogo ";
			if (this.Command.EndsWith(".ps1"))//如果是文件
			{
				arguments += $@"-File ""{this.Command}"" {this.Arguments}";
			}
			else
			{
				arguments += $@"-Command ""& {{ {this.Command} {this.Arguments}}}""";
			}
			var options = new Briver.Utils.Command.Options
			{
				Command = "pwsh",
				Arguments = arguments
			};

			this.Output = Briver.Utils.Command.Execute(options);
			return true;
		}
		catch (Exception ex)
		{
			var xml = new XElement(nameof(Shell),
				new XAttribute(nameof(Command), this.Command),
				new XAttribute(nameof(Arguments), this.Arguments)
			).ToString(SaveOptions.DisableFormatting);
			this.Log.LogError($"执行任务“{xml }”发生异常：{ex.ToString()}");
			return false;
		}

	}
}
