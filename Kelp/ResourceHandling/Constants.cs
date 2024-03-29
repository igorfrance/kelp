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
	/// <summary>
	/// Defines code resource types.
	/// </summary>
	public enum ResourceType
	{
		/// <summary>
		/// Indicates that the value hasn't been defined.
		/// </summary>
		Undefined = 0,

		/// <summary>
		/// Specifies a <c>JavaScript</c> resource.
		/// </summary>
		JavaScript = 1,

		/// <summary>
		/// Specifies a <c>CSS</c> resource.
		/// </summary>
		CSS = 2,

		/// <summary>
		/// Specifies a <c>LESS CSS</c> resource.
		/// </summary>
		LessCSS,

		/// <summary>
		/// Specifies an image resource.
		/// </summary>
		Image,

		/// <summary>
		/// Specifies an icon resource.
		/// </summary>
		Icon,

		/// <summary>
		/// Specifies a document resource.
		/// </summary>
		Document,
	}

	/// <summary>
	/// Defines the special characters significant for code processing.
	/// </summary>
	internal static class Chars
	{
		public const char Slash = '/';
		public const char Backslash = '\\';
		public const char Space = ' ';
		public const char Tab = '\t';
		public const char Star = '*';
		public const char NewLine = '\n';
		public const char CarriageReturn = '\r';
		public const char Quote = '"';
		public const char Apos = '\'';
		public const char Eof = (char) 0;
	}
}
