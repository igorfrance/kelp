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
	using System.Diagnostics.Contracts;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Kelp.Extensions;
	using Kelp.Http;
	using Kelp.Imaging.Filters;
	using log4net;

	/// <summary>
	/// Represents an image file resource.
	/// </summary>
	public abstract class ImageFile
	{
		/// <summary>
		/// The <see cref="QueryFilter"/> associated with the current <see cref="ImageFile"/>.
		/// </summary>
		protected QueryFilter filter;

		/// <summary>
		/// The <see cref="QueryString"/> associated with the current <see cref="ImageFile"/>.
		/// </summary>
		protected QueryString parameters;

		private const int MaxPathLength = 260;
		
		private static readonly ILog log = LogManager.GetLogger(typeof(ImageFile).FullName);
		private static readonly List<string> extensions;
		private readonly string absolutePath;
		private byte[] imageBytes;
		private MemoryStream stream;


		static ImageFile()
		{
			extensions = new List<string>();
			var types = from t in Assembly.GetExecutingAssembly().GetTypes()
						where t.IsClass && !t.IsAbstract
						select t;

			foreach (Type type in types)
			{
				var attribs = type.GetCustomAttributes(typeof(ResourceFileAttribute), false);
				if (attribs.Length != 0)
				{
					var resourceAttrib = (ResourceFileAttribute) attribs[0];
					if (resourceAttrib.ContentType.ContainsAnyOf("image"))
						extensions.AddRange(resourceAttrib.Extensions);
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImageFile"/> class.
		/// </summary>
		/// <param name="absolutePath">The absolute path of the image.</param>
		protected ImageFile(string absolutePath)
		{
			this.absolutePath = absolutePath;
			this.UseCache = true;
		}

		/// <summary>
		/// Gets the contents of this image as a byte stream.
		/// </summary>
		public byte[] Bytes
		{
			get
			{
				return this.imageBytes ?? (this.imageBytes = this.Load());
			}
		}

		/// <summary>
		/// Gets the path to the temporary directory in which to save the cached versions of the image.
		/// </summary>
		public string TemporaryDirectory { get; private set; }

		/// <summary>
		/// Gets the query filter associated with this image.
		/// </summary>
		public QueryFilter Filter
		{
			get
			{
				return filter;
			}
		}

		/// <summary>
		/// Gets the physical file name of the processed and cached version of this image file instance.
		/// </summary>
		public string CacheName
		{
			get
			{
				return string.Concat(absolutePath, filter.Query.ToString("?"))
						.Replace('/', '_')
						.Replace('\\', '_')
						.Replace(':', '_')
						.Replace('?', '_')
						.Replace('>', '_')
						.Replace('<', '_');
			}
		}

		/// <summary>
		/// Gets the physical file path of the processed and cached version of this image file instance.
		/// </summary>
		public string CachePath
		{
			get
			{
				if (this.TemporaryDirectory == null)
					return null;

				var fullPath = Path.Combine(this.TemporaryDirectory, this.CacheName);
				if (fullPath.Length < MaxPathLength)
					return fullPath;

				var cacheName = this.CacheName;
				var tempDir = this.TemporaryDirectory;

				do
				{
					cacheName = cacheName.Substring(1);
					fullPath = Path.Combine(tempDir, cacheName);
				}
				while (fullPath.Length >= MaxPathLength);

				return fullPath;
			}
		}

		/// <summary>
		/// Gets the the content type associated with this image file instance.
		/// </summary>
		public string ContentType
		{
			get
			{
				var extension = Path.GetExtension(absolutePath).Replace(".", string.Empty).ToLower();
				if (extension == "jpg")
					extension = "jpeg";

				return "image/" + extension;
			}
		}

		/// <summary>
		/// Gets the modification date of this image file instance.
		/// </summary>
		public DateTime LastModified
		{
			get
			{
				DateTime dateSource = Kelp.Util.GetDateLastModified(absolutePath);
				if (File.Exists(CachePath))
				{
					DateTime dateCache = Kelp.Util.GetDateLastModified(CachePath);
					if (dateCache > dateSource)
						return dateCache;
				}

				return dateSource;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use cache with this image file instance
		/// </summary>
		public bool UseCache { get; set; }

		/// <summary>
		/// Gets the absolute path of this image.
		/// </summary>
		public string AbsolutePath
		{
			get
			{
				return this.absolutePath;
			}
		}

		/// <summary>
		/// Gets the stream of bytes from this image.
		/// </summary>
		public Stream Stream
		{
			get
			{
				return stream ?? new MemoryStream(this.Bytes);
			}
		}

		/// <summary>
		/// Gets an E-tag for the file represented with this instance.
		/// </summary>
		public string ETag
		{
			get
			{
				return Util.GetETag(this.absolutePath, this.LastModified);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to skip processing the image
		/// </summary>
		protected virtual bool SkipProcessing
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the codec info to use when saving this image file instance.
		/// </summary>
		protected abstract ImageCodecInfo CodecInfo { get; }

		/// <summary>
		/// Gets the encode parameters to use when saving this image file instance.
		/// </summary>
		protected abstract EncoderParameters CodecParameters { get; }

		/// <summary>
		/// Creates <see cref="ImageFile"/> instances matching the specified absolute path by extension..
		/// </summary>
		/// <param name="absolutePath">The absolute path of the image.</param>
		/// <param name="parameters">The query string parameters of the <see cref="QueryFilter"/> associated 
		/// with the <see cref="ImageFile"/> that will be created.</param>
		/// <param name="temporaryDirectory">The temporary directory in which to save the caches.</param>
		/// <returns>A new <see cref="ImageFile"/> instance matching the specified absolute path by extension.</returns>
		public static ImageFile Create(string absolutePath, string parameters = null, string temporaryDirectory = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(absolutePath));

			return Create(absolutePath, new QueryString(parameters ?? string.Empty), temporaryDirectory);
		}

		/// <summary>
		/// Creates <see cref="ImageFile"/> instances matching the specified absolute path by extension..
		/// </summary>
		/// <param name="absolutePath">The absolute path of the image.</param>
		/// <param name="parameters">The query string parameters of the <see cref="QueryFilter"/> associated 
		/// with the <see cref="ImageFile"/> that will be created.</param>
		/// <param name="temporaryDirectory">The temporary directory in which to save the caches.</param>
		/// <returns>A new <see cref="ImageFile"/> instance matching the specified absolute path by extension.</returns>
		public static ImageFile Create(string absolutePath, QueryString parameters, string temporaryDirectory = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(absolutePath));
			Contract.Requires<ArgumentNullException>(parameters != null);

			ImageFile instance;
			if (absolutePath.ToLower().EndsWith("png"))
				instance = new PngFile(absolutePath);
			else if (absolutePath.ToLower().EndsWith("gif"))
				instance = new GifFile(absolutePath);
			else
				instance = new JpegFile(absolutePath);

			instance.TemporaryDirectory = Kelp.Util.MapPath(temporaryDirectory ?? Configuration.Current.TemporaryDirectory);
			instance.filter = new QueryFilter(parameters);
			instance.parameters = parameters;
			return instance;
		}

		internal static bool IsFileExtensionSupported(string extension)
		{
			return extensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase);
		}

		private byte[] Load()
		{
			bool useCache = false;
			byte[] imageData;
			stream = null;

			DateTime dateSource = Kelp.Util.GetDateLastModified(absolutePath);

			if (this.UseCache && File.Exists(CachePath) && Kelp.Util.GetDateLastModified(CachePath) > dateSource)
				useCache = true;

			if (useCache)
			{
				return File.ReadAllBytes(this.CachePath);
			}

			var img = Image.FromFile(absolutePath);
			var dimensions = new FrameDimension(img.FrameDimensionsList[0]);
			var frameCount = img.GetFrameCount(dimensions);
			img.Dispose();

			//// If frame count is greater than 1 it's an animated gif, and we don't want to process those
			if (frameCount > 1)
			{
				imageData = File.ReadAllBytes(absolutePath);
			}
			else
			{
				using (Bitmap inputImage = new Bitmap(absolutePath))
				using (Bitmap outputImage = filter.Apply(inputImage))
				{
					MemoryStream outputStream = new MemoryStream();
					outputImage.Save(outputStream, CodecInfo, CodecParameters);
					imageData = outputStream.GetBuffer();
				}
			}

			if (this.UseCache)
			{
				try
				{
					var cacheDir = Path.GetDirectoryName(CachePath);
					if (!Directory.Exists(cacheDir))
						Directory.CreateDirectory(cacheDir);

					File.WriteAllBytes(CachePath, imageData);
				}
				catch (Exception ex)
				{
					log.ErrorFormat("Failed to cache the image to {0}: {1}", CachePath, ex.Message);
					throw;
				}
			}

			return imageData;
		}
	}
}
