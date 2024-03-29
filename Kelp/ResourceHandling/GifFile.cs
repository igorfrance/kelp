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
	using System.Drawing.Imaging;

	using Kelp.Imaging;

	/// <summary>
	/// Represents a GIF <see cref="ImageFile"/>
	/// </summary>
	[ResourceFile(ResourceType.Image, "image/gif", "gif", "gif")]
	public class GifFile : ImageFile
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GifFile"/> class.
		/// </summary>
		/// <param name="absolutePath">The absolute path of the image.</param>
		internal GifFile(string absolutePath)
			: base(absolutePath)
		{
		}

		/// <inheritdoc/>
		protected override ImageCodecInfo CodecInfo
		{
			get
			{
				return ImageHelper.GetCodecForType(ImageFormat.Gif);
			}
		}

		/// <inheritdoc/>
		protected override EncoderParameters CodecParameters
		{
			get
			{
				EncoderParameters encoderParams = new EncoderParameters(1);
				encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8);
				return encoderParams;
			}
		}
	}
}
