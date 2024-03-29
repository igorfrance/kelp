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
	using System.Diagnostics.Contracts;
	using System.IO;
	using System.Web;

	using log4net;

	using Util = Kelp.Http.Util;

	/// <summary>
	/// Implements an <see cref="IHttpHandler"/> for handling image, script and CSS requests.
	/// </summary>
	public class ResourceHandler : IHttpHandler
	{
		/// <summary>
		/// The name of the query string property that instructs <see cref="ResourceHandler"/> to return 
		/// only the raw content, without processing the includes.
		/// </summary>
		/// <example><c>?noprocess=1</c></example>
		public const string SkipProcessingKey = "noprocess";
		
		/// <summary>
		/// The value of the query string property (defined with <see cref="SkipProcessingKey"/>) 
		/// that instructs <see cref="ResourceHandler"/> to return only the raw content, without 
		/// processing the includes.
		/// </summary>
		/// <example><c>?noprocess=1</c></example>
		public const string SkipProcessingValue = "1";

		/// <summary>
		/// Specifies the maximum time difference (in seconds) between the current and the cached files that
		/// will still be allowed for the files to still be considered equal.
		/// </summary>
		public const int MaxDifferenceCachedDate = 2;

		private const byte AttemptCount = 5;
		private static readonly ILog log = LogManager.GetLogger(typeof(ResourceHandler).FullName);

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
		/// </summary>
		/// <value></value>
		/// <returns><c>true</c> if the <see cref="IHttpHandler"/> instance is reusable; otherwise, <c>false</c>.
		/// </returns>
		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

		internal static string TemporaryDirectory { get; private set; }

		/// <summary>
		/// Enables processing of HTTP Web requests by a custom HTTP handler that implements the <see cref="IHttpHandler"/> interface.
		/// </summary>
		/// <param name="context">An <see cref="HttpContext"/> object that provides references to the intrinsic server objects (for example, 
		/// Request, Response, Session, and Server) used to service HTTP requests.</param>
		public void ProcessRequest(HttpContext context)
		{
			string absolutePath = context.Request.PhysicalPath;
			string extension = Path.GetExtension(absolutePath).Trim('.');
			HttpContextWrapper wrapped = new HttpContextWrapper(context);

			if (File.Exists(absolutePath))
			{
				byte currentAttempt = 0;
				while (true)
				{
					try
					{
						if (CodeFile.IsFileExtensionSupported(extension))
							ResourceHandler.ProcessCodeFileRequest(wrapped);
						else if (ImageFile.IsFileExtensionSupported(extension))
							ResourceHandler.ProcessImageFileRequest(wrapped);
						else
						{
							log.WarnFormat("The file extension {0} is not recognized as either a code or an image file", extension);
							ResourceHandler.SendContent(wrapped, absolutePath);
						}

						break;
					}
					catch (System.IO.IOException)
					{
						if (++currentAttempt > AttemptCount)
							throw;
					}
				}
			}
			else
			{
				Util.SendFileNotFound(wrapped);
			}
		}

		/// <summary>
		/// Processes requests for file types handled by <see cref="CodeFile"/>.
		/// </summary>
		/// <param name="context">An <see cref="HttpContext"/> object that provides references to the intrinsic server objects (for example,
		/// Request, Response, Session, and Server) used to service HTTP requests.</param>
		/// <exception cref="ArgumentNullException"><c>context</c> or <c>mapPath</c> is <c>null</c>.</exception>
		/// <remarks>
		/// The logic of getting and serving a resource file, in a nutshell is similar to:
		/// <list>
		/// <item>1) Get the resource item</item>
		/// <item>2) Is this a previously cached request? (YES: goto 3; NO: goto 5)</item>
		/// <item>3) Is the resource newer than the cached date? (YES: goto 5; NO: goto 4)</item>
		/// <item>4) Send 304 headers back to the client</item>
		/// <item>5) Send content down the wire</item>
		/// </list>
		/// </remarks>
		private static void ProcessCodeFileRequest(HttpContextBase context)
		{
			Contract.Requires<ArgumentNullException>(context != null);

			// 1
			CodeFile file = CodeFile.Create(context.Request.PhysicalPath, context.Request.Path, ResourceHandler.GetTemporaryDirectory(context));

			// 2 & 3
			if (!Util.IsNoCacheRequest(context) && Util.IsCachedRequest(context) && !Util.IsFileUpdatedSinceCached(context, file.LastModified))
			{
				// 4
				Util.SendNotModified(context);
				return;
			}

			// 5
			ResourceHandler.SendContent(context, file);
		}

		/// <summary>
		/// Processes requests for file types handled by <see cref="ImageFile"/>.
		/// </summary>
		/// <param name="context">An <see cref="HttpContext"/> object that provides references to the intrinsic server objects (for example,
		/// Request, Response, Session, and Server) used to service HTTP requests.</param>
		/// <exception cref="ArgumentNullException"><c>context</c> or <c>mapPath</c> is <c>null</c>.</exception>
		private static void ProcessImageFileRequest(HttpContextBase context)
		{
			Contract.Requires<ArgumentNullException>(context != null);

			// 1
			ImageFile file = ImageFile.Create(
				context.Request.PhysicalPath, new QueryString(context.Request.QueryString), ResourceHandler.GetTemporaryDirectory(context));

			// 2 & 3
			if (!Util.IsNoCacheRequest(context) && Util.IsCachedRequest(context) && !Util.IsFileUpdatedSinceCached(context, file.LastModified))
			{
				// 4
				Util.SendNotModified(context);
				return;
			}

			// 5
			ResourceHandler.SendContent(context, file);
		}

		private static void SendContent(HttpContextBase context, string filename)
		{
			context.Response.ContentType = Util.GetMimeType(filename);
			context.Response.BinaryWrite(File.ReadAllBytes(filename));
		}

		/// <summary>
		/// Sends the contents of the file down the response.
		/// </summary>
		/// <param name="context">The context under which this code is executing.</param>
		/// <param name="file">The file to send.</param>
		private static void SendContent(HttpContextBase context, CodeFile file)
		{
			bool rawContent = context.Request.QueryString[SkipProcessingKey] == SkipProcessingValue;
			string content = rawContent ? file.RawContent : file.Content;

			context.Response.ContentType = file.ContentType;
			context.Response.Cache.SetCacheability(HttpCacheability.Public);
			context.Response.Cache.SetLastModified(file.LastModified);
			context.Response.Cache.SetETag(file.ETag);
			context.Response.Write(content);
		}

		/// <summary>
		/// Sends the contents of the file down the response.
		/// </summary>
		/// <param name="context">The context under which this code is executing.</param>
		/// <param name="file">The file to send.</param>
		private static void SendContent(HttpContextBase context, ImageFile file)
		{
			if (Util.IsNoCacheRequest(context))
				file.UseCache = false;

			context.Response.ContentType = file.ContentType;
			context.Response.Cache.SetCacheability(HttpCacheability.Public);
			context.Response.Cache.SetLastModified(file.LastModified);
			context.Response.BinaryWrite(file.Bytes);
		}

		private static string GetTemporaryDirectory(HttpContextBase context)
		{
			if (ResourceHandler.TemporaryDirectory != null)
				return ResourceHandler.TemporaryDirectory;

			string path = Configuration.Current.TemporaryDirectory;
			if (Path.IsPathRooted(path) && !path.StartsWith("/"))
				ResourceHandler.TemporaryDirectory = path;
			else
			{
				if (!path.StartsWith("/") && !path.StartsWith("~/"))
					path = "~/" + path;

				ResourceHandler.TemporaryDirectory = context.Server.MapPath(path);
			}

			return ResourceHandler.TemporaryDirectory;
		}
	}
}