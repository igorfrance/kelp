﻿/**
 * Copyright 2012 Igor France
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace Kelp.App
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	using Kelp.Extensions;
	using Kelp.ResourceHandling;

	using log4net;
	using log4net.Appender;
	using log4net.Core;
	using CA = log4net.Appender.ColoredConsoleAppender;

	/// <summary>
	/// Provides an application that offers kelp functionality outside of a web application.
	/// </summary>
	internal static class Program
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Program).FullName);
		private static readonly ColoredConsoleAppender appender = new ColoredConsoleAppender();

		static Program()
		{
			appender.AddMapping(new CA.LevelColors { Level = Level.Error, ForeColor = CA.Colors.Red | CA.Colors.HighIntensity });
			appender.AddMapping(new CA.LevelColors { Level = Level.Debug, ForeColor = CA.Colors.White });
			appender.AddMapping(new CA.LevelColors { Level = Level.Warn, ForeColor = CA.Colors.Yellow });
			appender.AddMapping(new CA.LevelColors { Level = Level.Info, ForeColor = CA.Colors.Green });
			appender.AddMapping(new CA.LevelColors { Level = Level.Fatal, ForeColor = CA.Colors.White, BackColor = CA.Colors.Red | CA.Colors.HighIntensity });
			appender.Layout = new log4net.Layout.PatternLayout("%message%newline");
			appender.Name = "CCA";
			appender.ActivateOptions();
			appender.Threshold = Level.Off;

			var repository = LogManager.GetRepository() as log4net.Repository.Hierarchy.Hierarchy;
			repository.Root.AddAppender(appender);
			repository.Configured = true;
		}

		internal static string Name
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Name.ToLower();
			}
		}

		[STAThread]
		internal static int Main(string[] args)
		{
			var arguments = new Arguments(args);
			appender.Threshold = arguments.Logging;

			if (arguments.ShowHelp)
			{
				Console.Write(arguments.Help == "options" ? GetOptions() : GetUsage());
			}
			else if (!arguments.Valid)
			{
				Console.Write(GetUsage());
			}
			else
			{
				if (arguments.ProcessType == ResourceType.Undefined)
				{
					Console.WriteLine("The processing type could not be determined based on the file name extensions.");
					Console.WriteLine("Make sure that all file paths specify file name extensions, and that they are compatible.\n");
					Console.WriteLine(GetUsage());
				}
				else
				{
					try
					{
						switch (arguments.ProcessType)
						{
							case ResourceType.Image:
								ProcessImageFile(arguments);
								break;

							case ResourceType.CSS:
							case ResourceType.JavaScript:
								ProcessCodeFile(arguments);
								break;
						}

						return 0;
					}
					catch (Exception ex)
					{
						log.Error("Failed to process input file(s)", ex);
					}
				}
			}

			return 1;
		}

		private static void ProcessCodeFile(Arguments arguments)
		{
			FileTypeConfiguration settings;

			if (arguments.ProcessType == ResourceType.CSS || arguments.ProcessType == ResourceType.LessCSS)
				settings = new CssFileConfiguration(arguments.Settings);
			else
				settings = new ScriptFileConfiguration(arguments.Settings);

			CodeFile file = CodeFile.CreateFromResourceType(arguments.ProcessType, settings);
			file.CachingEnabled = false;

			arguments.Files.Each((Action<string>) file.AddFile);

			if (!string.IsNullOrEmpty(arguments.Target))
			{
				var ex = EnsureDirectoryExists(arguments.Target);
				if (ex == null)
				{
					File.WriteAllText(arguments.Target, file.Content, Encoding.UTF8);
				}
				else
				{
					log.Error(string.Format("Error creating output directory for path {0}", arguments.Target), ex);
				}
			}
			else
			{
				Console.Write(file.Content);
			}
		}

		private static void ProcessImageFile(Arguments arguments)
		{
			if (string.IsNullOrEmpty(arguments.Target))
			{
				log.ErrorFormat("To process an image, please specify the target file");
				return;
			}

			var imagePath = Util.MapPath(arguments.Files[0]);
			var targetPath = Util.MapPath(arguments.Target);
			ImageFile file = ImageFile.Create(imagePath, arguments.Settings);

			using (Bitmap outputImage = new Bitmap(file.Stream))
			{
				outputImage.Save(targetPath);
			}
		}

		private static Exception EnsureDirectoryExists(string targetPath)
		{
			if (string.IsNullOrEmpty(targetPath))
				return null;

			string directory = Path.GetDirectoryName(targetPath);
			try
			{
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		private static string GetUsage()
		{
			StringBuilder result = new StringBuilder();
			result.AppendLine();
			result.AppendLine("Merges and/or compresses scripts and stylesheets.");
			result.AppendFormat("Usage: {0} -source:<path> -target:<path> [-settings:<settings>]\n\n", Program.Name);
			result.AppendLine("  -source:<path>        File or files that should be merged and/or compressed. ");
			result.AppendLine("                        If multiple paths are used, separate them with a semicolon ';'.");
			result.AppendLine("                        If a path contains spaces, make sure the path is quoted.");
			result.AppendLine("  -target:<path>        The file to which to save the result. With script and style files");
			result.AppendLine("                        this argument is optional and if omitted the result will be written");
			result.AppendLine("                        back to console.");
			result.AppendLine("  -settings:<settings>  Optional processing settings. The settings should be combined into");
			result.AppendLine("                        a single value using '=' joined name/value pairs, separated by an ");
			result.AppendLine("                        explamation mark '!'; for instance:");
			result.AppendLine("                              -settings:minifycode=true!outputmode=singleline");
			result.AppendLine("                        The options that can be used here depend on type of processing that is");
			result.AppendLine("                        being done; image, script or css processing. Options are not case-sensitive.");
			result.AppendLine("  -log:<level>          Specifies the amount of logging information to send to console. Possible");
			result.AppendLine("                        values are DEBUG|INFO|WARN|ERROR|FATAL|OFF. The default value is OFF.");
			result.AppendLine("  -help:options         Displays extended information about available processing options");

			return result.ToString();
		}

		private static string GetOptions()
		{
			StringBuilder result = new StringBuilder();
			result.AppendLine("Script processing options are: ");
			result.AppendLine("    MinifyCode=true|false (default:false)");
			result.AppendLine("    CollapseToLiteral=true|false (default:true)");
			result.AppendLine("    EvalLiteralExpressions=true|false (default:true)");
			result.AppendLine("    MacSafariQuirks=true|false (default:true)");
			result.AppendLine("    ManualRenamesProperties=true|false (default:true)");
			result.AppendLine("    PreserveFunctionNames=true|false (default:false)");
			result.AppendLine("    PreserveImportantComments=true|false (default:false)");
			result.AppendLine("    RemoveFunctionExpressionNames=true|false (default:true)");
			result.AppendLine("    RemoveUnneededCode=true|false (default:true)");
			result.AppendLine("    StripDebugStatements=true|false (default:true)");
			result.AppendLine("    InlineSafeStrings=true|false (default:true)");
			result.AppendLine("    StrictMode=true|false (default:false)");
			result.AppendLine("    EvalTreatment=Ignore|MakeAllSafe|MakeImmediateSafe (default:Ignore)");
			result.AppendLine("    LocalRenaming=CrunchAll|KeepAll|KeepLocalizationVars (default:CrunchAll)");
			result.AppendLine("    OutputMode=SingleLine|MultipleLines (default:SingleLine)");
			result.AppendLine("\nCSS processing options are: ");
			result.AppendLine("    MinifyExpressions=true|false (default:false)");
			result.AppendLine("    TermSemicolons=true|false (default:true)");
			result.AppendLine("    ColorNames=Hex|Major|Strict (default:Strict)");
			result.AppendLine("    CommentMode=All|Hacks|Important|None (default:Important)");
			result.AppendLine("\nImage processing options are: ");
			result.AppendLine("    resize=int,int");
			result.AppendLine("    crop=int,int,int,int");
			result.AppendLine("    brightness=byte");
			result.AppendLine("    contrast=byte");
			result.AppendLine("    gamma=byte");
			result.AppendLine("    rgb=byte,byte,byte");
			result.AppendLine("    hsl=byte,byte,byte");
			result.AppendLine("    sepia=0|1");
			result.AppendLine("    grayscale=0|1");
			result.AppendLine("    mirrorh=0|1");
			result.AppendLine("    mirrorv=0|1");
			result.AppendLine("    sharpen=0|1");
			result.AppendLine("    sharpenx=byte");

			return result.ToString();
		}

		private static string PrependWorkingDirectoryToPath(string childPath)
		{
			if (Path.IsPathRooted(childPath))
				return childPath;

			return Path.Combine(Environment.CurrentDirectory, childPath);
		}

		private class Arguments
		{
			private static readonly string[] imageExtensions = { "gif", "png", "bmp", "jpg", "jpeg" };
			private static readonly string[] scriptExtensions = { "js" };
			private static readonly string[] cssExtensions = { "css" };
			private static readonly string[] lessExtensions = { "less" };

			private readonly bool? valid;

			public Arguments(IEnumerable<string> args)
			{
				this.Files = new List<string>();
				this.Settings = new QueryString();
				this.ProcessType = ResourceType.Undefined;
				this.Logging = Level.Off;

				foreach (var argument in args)
				{
					if (argument == "/?" || argument.EndsWith("-help"))
					{
						this.ShowHelp = true;
						continue;
					}

					if (!argument.StartsWith("-") || !argument.Contains(":"))
						continue;

					string name = argument.Substring(1, argument.IndexOf(":", System.StringComparison.Ordinal) - 1);
					string value = argument.Substring(argument.IndexOf(":", System.StringComparison.Ordinal) + 1);

					if (name == "settings")
					{
						this.Settings = new QueryString(value, '!');
					}
					else if (name == "target")
					{
						string targetPath = Program.PrependWorkingDirectoryToPath(value.Trim('\'', '"'));
						FileInfo fileInfo = new FileInfo(targetPath);
						this.Target = Program.PrependWorkingDirectoryToPath(fileInfo.FullName);
					}
					else if (name == "source")
					{
						IEnumerable<string> sources = Util.SplitArguments(';', value);
						this.Files = new List<string>();
						foreach (string source in sources)
						{
							string sourcePath = Program.PrependWorkingDirectoryToPath(source);
							FileInfo fileInfo = new FileInfo(sourcePath);
							if (fileInfo.FullName.Equals(this.Target, StringComparison.InvariantCultureIgnoreCase))
							{
								log.ErrorFormat("Input file can't be the same as the output path");
								this.valid = false;
								return;
							}

							this.Files.Add(fileInfo.FullName);
						}
					}
					else if (name == "help")
					{
						this.ShowHelp = true;
						this.Help = value;
					}
					else if (name == "log")
					{
						switch (value.ToLower())
						{
							case "debug":
								this.Logging = Level.Debug;
								break;
							case "info":
								this.Logging = Level.Info;
								break;
							case "warn":
								this.Logging = Level.Warn;
								break;
							case "error":
								this.Logging = Level.Error;
								break;
							case "fatal":
								this.Logging = Level.Fatal;
								break;
						}
					}
					else
					{
						log.ErrorFormat("Unrecognized argument: '{0}'", name);
					}
				}

				foreach (string file in Files)
				{
					var ext = Path.GetExtension(file).ToLower().Trim('.');
					var type = ResourceType.Undefined;
					if (imageExtensions.Contains(ext))
						type = ResourceType.Image;
					if (scriptExtensions.Contains(ext))
						type = ResourceType.JavaScript;
					if (cssExtensions.Contains(ext))
						type = ResourceType.CSS;
					if (lessExtensions.Contains(ext))
						type = ResourceType.LessCSS;

					if (this.ProcessType != type && this.ProcessType != ResourceType.Undefined && type != ResourceType.Undefined)
					{
						log.ErrorFormat("The files used are not of the same type.");
						this.ProcessType = ResourceType.Undefined;
						break;
					}

					this.ProcessType = type;
				}
			}

			public bool Valid
			{
				get
				{
					return valid ?? Files.Count != 0;
				}
			}

			public bool ShowHelp { get; private set; }

			public List<string> Files { get; private set; }

			public QueryString Settings { get; private set; }

			public string Target { get; private set; }

			public string Help { get; private set; }

			public Level Logging { get; private set; }

			public ResourceType ProcessType { get; private set; }
		}
	}
}
