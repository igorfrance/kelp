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
namespace Kelp.ResourceHandling
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.Contracts;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Security.Cryptography;
	using System.Text;
	using System.Text.RegularExpressions;

	using Kelp.Extensions;
	using Kelp.Http;

	using log4net;

	/// <summary>
	/// Represents a code file that may include other code files.
	/// </summary>
	public abstract class CodeFile
	{
		/// <summary>
		/// A reference to the including (parent) <see cref="CodeFile"/> class, if this file 
		/// was included from another.
		/// </summary>
		protected CodeFile Parent { get; private set; }
		
		/// <summary>
		/// The file's fully processed source code.
		/// </summary>
		protected StringBuilder content = new StringBuilder();

		/// <summary>
		/// The file's unprocessed source code.
		/// </summary>
		protected StringBuilder rawContent = new StringBuilder();

		private string temporaryDirectory;

		private static readonly ILog log = LogManager.GetLogger(typeof(CodeFile).FullName);
		private static readonly Regex instructionExpression = new Regex(@"^\s*/\*#\s*(?'name'\w+):\s*(?'value'.*?) \*/");
		private static List<string> fileExtensions;

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeFile"/> class.
		/// </summary>
		protected CodeFile()
		{
			this.References = new OrderedDictionary<string, string>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeFile"/> class, using the specified <paramref name="configuration"/>
		/// and <paramref name="parent"/>.
		/// </summary>
		/// <param name="configuration">The processing configuration for this file.</param>
		/// <param name="parent">The script processor creating this instance (used when processing includes)</param>
		protected CodeFile(FileTypeConfiguration configuration, CodeFile parent = null)
			: this()
		{
			this.Parent = parent;
			this.Configuration = configuration;
			this.CachedConfigurationSettings = string.Empty;
			this.Parent = parent;
		}

		/// <summary>
		/// Gets the absolute path of this code file.
		/// </summary>
		/// <exception cref="ArgumentNullException">If the supplied value is <c>null</c> or empty.</exception>
		/// <exception cref="FileNotFoundException">If the specified value could not be resolved to an existing file.</exception>
		public string AbsolutePath { get; private set; }

		/// <summary>
		/// Gets the relative path of this code file.
		/// </summary>
		public string RelativePath { get; private set; }

		/// <summary>
		/// Gets the list of files that this <see cref="CodeFile"/> is depending on
		/// </summary>
		public List<string> Dependencies
		{
			get
			{
				return this.References.Keys.ToList();
			}
		}

		/// <summary>
		/// Gets the last modified Date and Time of this <see cref="CodeFile"/>
		/// </summary>
		/// <remarks>
		/// This is done by getting the latest modification time from all files this code file depends on.
		/// </remarks> 
		public DateTime LastModified
		{
			get
			{
				if (File.Exists(this.CacheName))
					return Util.GetDateLastModified(this.CacheName);

				return Util.GetDateLastModified(this.Dependencies);
			}
		}

		/// <summary>
		/// Gets the fully processed content of this file.
		/// </summary>
		public string Content
		{
			get
			{
				return content.ToString();
			}

			set
			{
				this.content = new StringBuilder(value ?? string.Empty);
			}
		}

		/// <summary>
		/// Gets the raw, unprocessed content of this file.
		/// </summary>
		public string RawContent
		{
			get
			{
				return rawContent.ToString();
			}
		}

		/// <summary>
		/// Gets the dictionary of files that this code file references.
		/// </summary>
		public OrderedDictionary<string, string> References
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets an E-tag for the file represented with this instance.
		/// </summary>
		public string ETag
		{
			get
			{
				return GetETag(this.RelativePath, this.LastModified);
			}
		}

		/// <summary>
		/// Gets the full name of the temporary file where the processed version of this file will be saved.
		/// </summary>
		/// <remarks>
		/// This is an absolute path.
		/// </remarks>
		internal string CacheName
		{
			get
			{
				if (this.Configuration == null)
					return null;

				string fileName = this.AbsolutePath.ReplaceAll(@"[\\/:]", "_");
				return Path.Combine(this.TemporaryDirectory, fileName);
			}
		}

		/// <summary>
		/// Gets or sets the content-type of this code file.
		/// </summary>
		internal string ContentType { get; set; }

		internal ResourceType ResourceType { get; set; }

		/// <summary>
		/// Gets the configuration associated with this <see refe="CodeFile"/>'s file type.
		/// </summary>
		public FileTypeConfiguration Configuration { get; protected set; }

		/// <summary>
		/// The directory to use for caching
		/// </summary>
		protected string TemporaryDirectory
		{
			get
			{
				return temporaryDirectory ?? this.Configuration.TemporaryDirectory;
			}
		}

		internal string CachedConfigurationSettings { get; private set; }

		/// <summary>
		/// Gets a value indicating whether minification is enabled for this code file.
		/// </summary>
		protected virtual bool MinificationEnabled
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Returns an instance of <see cref="CodeFile" /> that matches the specified <typeparamref name="T" />
		/// </summary>
		/// <typeparam name="T">The specific type of the code file to create</typeparam>
		/// <param name="absolutePath">Optional physical path of the file to load into created <see cref="CodeFile" />.</param>
		/// <param name="relativePath">Optional relative path of the file to load.</param>
		/// <param name="parent">Optional parent <see cref="CodeFile" /> that is including this <see cref="CodeFile" /></param>
		/// <param name="configuration">Optional configuration object.</param>
		/// <returns>The code file matching the specified resource type, optionally loaded with contents from <paramref name="absolutePath" />.</returns>
		public static T Create<T>(string absolutePath, string relativePath = null, CodeFile parent = null, FileTypeConfiguration configuration = null) where T : CodeFile
		{
			CodeFile result = (CodeFile) typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });

			if (parent != null)
			{
				result.Parent = parent;
				if (parent.temporaryDirectory != null)
					result.temporaryDirectory = parent.temporaryDirectory;
			}

			if (configuration != null)
			{
				result.Configuration = configuration;
			}

			if (!string.IsNullOrEmpty(absolutePath))
				result.Load(absolutePath, relativePath);

			return result as T;
		}

		/// <summary>
		/// Returns an instance of <see cref="CodeFile" />, loaded with content from <paramref name="absolutePath"/>.
		/// </summary>
		/// <param name="absolutePath">The physical path of the file to load into created <see cref="CodeFile" />.</param>
		/// <param name="relativePath">Optional relative path of the file to load.</param>
		/// <param name="parent">Optional parent <see cref="CodeFile"/> that is including this <see cref="CodeFile"/></param>
		/// <returns>A code file appropriate for the specified <paramref name="absolutePath"/>.
		/// If the extension of the specified <paramref name="absolutePath"/> is <c>css</c>, the resulting value will be a 
		/// new <see cref="CssFile"/>. In all other cases the resulting value is a <see cref="ScriptFile"/>.</returns>
		public static CodeFile Create(string absolutePath, string relativePath = null, CodeFile parent = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(absolutePath));

			string extension = Path.GetExtension(absolutePath).Trim('.');
			if (extension.EndsWith("css"))
				return CodeFile.Create<CssFile>(absolutePath, relativePath, parent);
			if (extension.EndsWith("less"))
				return CodeFile.Create<LessCssFile>(absolutePath, relativePath, parent);

			return CodeFile.Create<ScriptFile>(absolutePath, relativePath, parent);
		}

		/// <summary>
		/// Creates the specified absolute path.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="configuration">The configuration.</param>
		/// <returns>CodeFile.</returns>
		public static T Create<T>(FileTypeConfiguration configuration) where T : CodeFile
		{
			CodeFile result = Create<T>();
			result.Configuration = configuration;
			return result as T;
		}

		/// <summary>
		/// Creates the specified absolute path.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns>CodeFile.</returns>
		public static T Create<T>() where T : CodeFile
		{
			CodeFile result = (CodeFile) typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
			return result as T;
		}

		/// <summary>
		/// Returns an instance of <see cref="CodeFile" />, loaded with content from <paramref name="absolutePath"/>.
		/// </summary>
		/// <param name="absolutePath">The physical path of the file to load into created <see cref="CodeFile" />.</param>
		/// <param name="relativePath">The relative path of the file to load.</param>
		/// <param name="temporaryDirectory">The temporary directory to use for caching</param>
		/// <returns>A code file appropriate for the specified <paramref name="absolutePath"/>.
		/// If the extension of the specified <paramref name="absolutePath"/> is <c>css</c>, the resulting value will be a 
		/// new <see cref="CssFile"/>. In all other cases the resulting value is a <see cref="ScriptFile"/>.</returns>
		public static CodeFile Create(string absolutePath, string relativePath, string temporaryDirectory)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(absolutePath));
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(relativePath));
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(temporaryDirectory));

			CodeFile result = CodeFile.Create(absolutePath, relativePath);
			result.temporaryDirectory = temporaryDirectory;
			return result;
		}

		/// <summary>
		/// Gets an E-tag for the specified <paramref name="fileName"/> and <paramref name="lastModified"/> date.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="lastModified">The last modified date of the file.</param>
		/// <returns>The E-Tag that matches the specified <paramref name="fileName"/> and <paramref name="lastModified"/> date.</returns>
		public static string GetETag(string fileName, DateTime lastModified)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(fileName));

			Encoder stringEncoder = Encoding.UTF8.GetEncoder();
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

			string fileString = fileName + lastModified +
				Assembly.GetExecutingAssembly().GetName().Version;

			// get string bytes
			byte[] bytes = new byte[stringEncoder.GetByteCount(fileString.ToCharArray(), 0, fileString.Length, true)];
			stringEncoder.GetBytes(fileString.ToCharArray(), 0, fileString.Length, bytes, 0, true);

			return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty).ToLower();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.RelativePath;
		}

		/// <summary>
		/// Parses and the content and references of the specified file to this code file.
		/// </summary>
		/// <param name="file">The file to add.</param>
		public void AddFile(string file)
		{
			var inner = CodeFile.Create(file);
			this.content.Append(inner.Content);
			this.rawContent.Append(inner.RawContent);
			foreach (string absolutePath in inner.References.Keys)
				this.AddReference(absolutePath, inner.References[absolutePath]);
		}

		/// <summary>
		/// Determines whether the specified path exists in the list of files already included.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns>
		/// <c>true</c> if the specified path exists in the list of files already included; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If the specified <c>path</c> is <c>null</c>.</exception>
		internal bool IsPathInIncludeChain(string path)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

			if (this.Parent == null)
				return false;

			bool contains = false;
			CodeFile ancestor = this;
			while (ancestor != null)
			{
				contains = ancestor.Dependencies.Any(value => 
					value.Equals(path, StringComparison.InvariantCultureIgnoreCase));

				ancestor = ancestor.Parent;
			}

			return contains;
		}

		/// <summary>
		/// Performs file-type specific pre-processing of the source code.
		/// </summary>
		/// <param name="sourceCode">The source code.</param>
		/// <param name="relativePath">The relative path of the current parent parse context, if any.</param>
		/// <returns>Preprocessed source code.</returns>
		protected virtual string PreProcess(string sourceCode, string relativePath)
		{
			return sourceCode;
		}

		/// <summary>
		/// Performs file-type specific post-processing of the source code
		/// </summary>
		/// <param name="sourceCode">The source code.</param>
		/// <returns>Preprocessed source code.</returns>
		protected virtual string PostProcess(string sourceCode)
		{
			return sourceCode;
		}

		/// <summary>
		/// Loads the file, either from cache or from its <see cref="AbsolutePath"/>.
		/// </summary>
		public virtual void Load(string absolutePath, string relativePath = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(absolutePath));
			Contract.Requires<FileNotFoundException>(File.Exists(absolutePath));

			this.AbsolutePath = absolutePath;
			this.RelativePath = relativePath ?? Path.GetFileName(absolutePath);
			if (this.Parent != null)
			{
				this.RelativePath = Path.Combine(Path.GetDirectoryName(Parent.RelativePath), this.RelativePath);
			}

			var cacheEntry = new CacheEntry(this.CacheName);
			ParseResult result;

			if (this.NeedsRefresh(cacheEntry))
			{
				var sourceCode = File.ReadAllText(absolutePath, Encoding.UTF8);
				result = this.Parse(sourceCode);
			}
			else
			{
				result = this.Parse(cacheEntry.Content);
			}

			this.content = new StringBuilder(result.Content.ToString());
			this.rawContent = new StringBuilder(result.RawContent);

			this.References.Clear();
			foreach (KeyValuePair<string, string> reference in result.References)
				this.AddReference(reference.Key, reference.Value);

			this.References.Add(this.AbsolutePath, this.RelativePath);

			this.content = new StringBuilder(this.PostProcess(this.Content));
			this.CacheToTemporaryDirectory();
		}

		/// <summary>
		/// Scans through the specified source code and processes it line by line.
		/// </summary>
		/// <param name="sourceCode">The source code to parse.</param>
		/// <param name="container">Optional parse container (for recursive calls).</param>
		/// <param name="absolutePath">The absolute path of the parsed file (for recursive calls).</param>
		/// <param name="relativePath">The relative path of the parsed file (for recursive calls).</param>
		/// <returns>The object that contains the result of parsing the source code.</returns>
		/// <exception cref="System.InvalidOperationException">The script cannot include itself.</exception>
		protected virtual ParseResult Parse(string sourceCode, ParseResult container = null, string absolutePath = null, string relativePath = null)
		{
			Contract.Requires<ArgumentNullException>(sourceCode != null);

			sourceCode = this.PreProcess(sourceCode, relativePath);
			absolutePath = absolutePath ?? this.AbsolutePath;
			relativePath = relativePath ?? this.RelativePath;

			ParseResult result = new ParseResult(sourceCode, container, relativePath);
			string[] lines = sourceCode.Replace("\r", string.Empty).Split(new[] { '\n' });

			foreach (string line in lines)
			{
				Match match;
				if ((match = instructionExpression.Match(line)).Success)
				{
					string name = match.Groups["name"].Value;
					string value = match.Groups["value"].Value;

					switch (name.ToLower())
					{
						case "reference":
							string[] parts = value.Split('|');
							if (parts.Length == 2)
								result.AddReference(parts[0].Trim(), parts[1].Trim());

							break;

						case "configuration":
							result.Configuration = value;
							break;

						case "include":
							var includePath = value;
							if (!Path.IsPathRooted(value))
							{
								var directory = Path.GetDirectoryName(absolutePath);
								includePath = Path.Combine(directory, Regex.Replace(value, @"\?.*$", string.Empty));
							}

							if (File.Exists(includePath))
							{
								if (includePath.Equals(absolutePath, StringComparison.InvariantCultureIgnoreCase))
								{
									log.FatalFormat("The script cannot include itself. The script is: {0}({1})", value, includePath);
									throw new InvalidOperationException("The script cannot include itself.");
								}

								var includeContent = File.ReadAllText(includePath, Encoding.UTF8);
								var inner = this.Parse(includeContent, result, includePath, value);
								result.Content.Append(inner.Content);

								foreach (var absPath in inner.References.Keys)
									result.AddReference(absPath, inner.References[absPath]);

								result.AddReference(includePath, value);
							}
							else
							{
								result.Content.AppendLine(line + "/* File not found */");
							}

							break;

						default:
							log.WarnFormat("Unrecognized processing instruction '{0}' encountered.", name);
							break;
					}
				}
				else
					result.Content.AppendLine(line);
			}

			return result;
		}

		private bool NeedsRefresh(CacheEntry cacheEntry)
		{
			if (!cacheEntry.Exists)
				return true;

			var parsed = this.Parse(cacheEntry.Content);
			foreach (string referencePath in parsed.References.Keys)
			{
				if (!File.Exists(referencePath))
					return true;

				var referenceModified = Util.GetDateLastModified(referencePath);
				if (referenceModified > cacheEntry.LastModified)
					return true;
			}

			return false;
		}

		private void AddReference(string absolutePath, string relativePath)
		{
			string referenceKey = absolutePath.ToLower().Replace("/", "\\").Replace("\\\\", "\\");
			if (this.References.ContainsKey(referenceKey))
				return;

			this.References.Add(referenceKey, relativePath);
		}

		private void CacheToTemporaryDirectory()
		{
			if (this.CacheName == null)
				return;

			if (!Directory.Exists(this.TemporaryDirectory))
			{
				try
				{
					Directory.CreateDirectory(this.TemporaryDirectory);
				}
				catch (Exception ex)
				{
					log.ErrorFormat("Could not create temporary directory '{0}': {1}", this.TemporaryDirectory, ex.Message);
				}
			}

			try
			{
				StringBuilder persistContent = new StringBuilder();
				persistContent.AppendLine(string.Format("/*# Configuration: {0} */", this.Configuration));
				foreach (string includePath in this.References.Keys)
					persistContent.AppendLine(string.Format("/*# Reference: {0} | {1} */", includePath, this.References[includePath]));

				persistContent.AppendLine();
				persistContent.Append(this.content);

				File.WriteAllText(this.CacheName, persistContent.ToString(), Encoding.UTF8);
				log.DebugFormat("Saved the temporary contents of '{0}' to '{1}'.", this.AbsolutePath, this.CacheName);
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Could not save the temporary file '{0}': {1}", this.CacheName, ex.Message);
			}
		}

		/// <summary>
		/// Contains all information about the file that was parsed
		/// </summary>
		protected class ParseResult
		{
			/// <summary>
			/// String builder around the parsed content.
			/// </summary>
			public StringBuilder Content = new StringBuilder();

			/// <summary>
			/// The raw content of the parsed file
			/// </summary>
			public string RawContent;

			/// <summary>
			/// The relative path
			/// </summary>
			public string RelativePath;

			/// <summary>
			/// Dictionary of absolute-path/relative-path names of files referenced by parsed file
			/// </summary>
			public OrderedDictionary<string, string> References = new OrderedDictionary<string, string>();

			/// <summary>
			/// Configuration settings, if parsed from content.
			/// </summary>
			public string Configuration;

			/// <summary>
			/// The code file that created this object.
			/// </summary>
			public ParseResult Container;

			/// <summary>
			/// Initializes a new instance of the <see cref="ParseResult" /> class.
			/// </summary>
			/// <param name="rawContent">The raw content of the resource..</param>
			/// <param name="container">Optional container object that represents content that included the content represented
			/// by this <see cref="ParseResult" /> instance.</param>
			/// <param name="relativePath">The relative path of the .</param>
			public ParseResult(string rawContent, ParseResult container, string relativePath)
			{
				this.Container = container;
				this.RawContent = rawContent;
				this.RelativePath = relativePath ?? string.Empty;
			}

			/// <summary>
			/// Adds a reference to the specified <paramref name="absolutePath" /> and <paramref name="relativePath" />.
			/// </summary>
			/// <param name="absolutePath">The absolute path.</param>
			/// <param name="relativePath">The relative path.</param>
			public void AddReference(string absolutePath, string relativePath = null)
			{
				relativePath = relativePath ?? Path.GetFileName(absolutePath);
				if (IsPathInIncludeChain(absolutePath))
				{
					log.FatalFormat("Including the referenced path would cause recursion due to it being present at a level higher above. The script is: {0}({1})", relativePath, absolutePath);
					throw new InvalidOperationException("Including the referenced path would cause recursion due to it being present at a level higher above.");
				}

				this.References.Add(absolutePath, relativePath);
			}

			/// <summary>
			/// Determines whether the specified path exists in the list of files already included.
			/// </summary>
			/// <param name="path">The path to check.</param>
			/// <returns>
			/// <c>true</c> if the specified path exists in the list of files already included; otherwise, <c>false</c>.
			/// </returns>
			/// <exception cref="ArgumentNullException">If the specified <c>path</c> is <c>null</c>.</exception>
			internal bool IsPathInIncludeChain(string path)
			{
				Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

				bool contains = false;
				ParseResult container = this;
				while (container != null)
				{
					contains = this.References.Keys.Any(value =>
						value.Equals(path, StringComparison.InvariantCultureIgnoreCase));

					container = container.Container;
				}

				return contains;
			}


			/// <summary>
			/// Gets a value indicatind whether the specified <paramref name="file"/> is referenced by this object.
			/// </summary>
			/// <param name="file">The path of the file to check.</param>
			/// <returns><c>true</c> if the specified <paramref name="file"/> is referenced by this object, <c>false</c> 
			/// otherwise</returns>
			public bool ReferencesFile(string file)
			{
				return this.References.ContainsKey((file ?? string.Empty).ToLower());
			}
		}


		/// <summary>
		/// Represents a previsously cached, fully processed version of a <see cref="CodeFile"/>.
		/// </summary>
		protected class CacheEntry
		{
			/// <summary>
			/// True if a cache entry exists
			/// </summary>
			public bool Exists;

			/// <summary>
			/// The last modification date of the cache entry
			/// </summary>
			public DateTime LastModified;

			/// <summary>
			/// The content of the cache entry
			/// </summary>
			public string Content;

			/// <summary>
			/// The path of the cache entry
			/// </summary>
			public string Path;

			/// <summary>
			/// Initializes a new instance of the <see cref="CacheEntry"/> class.
			/// </summary>
			/// <param name="path">The path of the cache entry.</param>
			public CacheEntry(string path)
			{
				this.Path = path;

				if (File.Exists(path))
				{
					this.Exists = true;
					this.Content = File.ReadAllText(path, Encoding.UTF8);
					this.LastModified = Util.GetDateLastModified(path);
				}
			}
		}

		internal static bool IsFileExtensionSupported(string extension)
		{
			if (fileExtensions == null)
			{
				fileExtensions = new List<string>();
				var types = from t in Assembly.GetExecutingAssembly().GetTypes()
							where t.IsClass && !t.IsAbstract
							select t;

				foreach (Type type in types)
				{
					var attribs = type.GetCustomAttributes(typeof(ResourceFileAttribute), false);
					if (attribs.Length != 0)
					{
						var resourceAttrib = (ResourceFileAttribute) attribs[0];
						if (resourceAttrib.ContentType.ContainsAnyOf("text", "application"))
						{
							fileExtensions.AddRange(resourceAttrib.Extensions);
						}
					}
				}
			}

			return fileExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase);
		}
	}
}
