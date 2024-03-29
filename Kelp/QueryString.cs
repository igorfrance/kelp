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
namespace Kelp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Web;
	using System.Xml;

	/// <summary>
	/// Provides a <see cref="NameValueCollection"/> variation that provides additional support for working with query strings.
	/// </summary>
	public class QueryString : NameValueCollection, IXmlConvertible
	{
		private const char DefaultKeyValueSeparator = '=';
		private static readonly char[] DefaultPairSeparators = { '&' };

		private readonly char[] pairSeparators = DefaultPairSeparators;
		private readonly char keyValueSeparator = DefaultKeyValueSeparator;

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString"/> class.
		/// </summary>
		public QueryString()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString" /> class, using the specified <paramref name="pairSeparators"/>
		/// and optional <paramref name="keyValueSeparator"/>.
		/// </summary>
		/// <param name="pairSeparators">The chars that can be used as key/value pair separators.</param>
		/// <param name="keyValueSeparator">The character that separates the key from the value.</param>
		public QueryString(char[] pairSeparators, char keyValueSeparator = DefaultKeyValueSeparator)
		{
			if (pairSeparators != null && pairSeparators.Length != 0)
				this.pairSeparators = pairSeparators;

			this.keyValueSeparator = keyValueSeparator;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString" /> class, using the specified <paramref name="pairSeparator"/>
		/// and optional <paramref name="keyValueSeparator"/>.
		/// </summary>
		/// <param name="pairSeparator">The char to use as key/value pair separator.</param>
		/// <param name="keyValueSeparator">The character that separates the key from the value.</param>
		public QueryString(char pairSeparator, char keyValueSeparator = DefaultKeyValueSeparator)
		{
			this.pairSeparators = new[] { pairSeparator };
			this.keyValueSeparator = keyValueSeparator;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString"/> class, copying non-empty entries 
		/// from the specified <paramref name="collection"/> into this instance.
		/// </summary>
		/// <param name="collection">The <see cref="NameValueCollection"/> to copy to the new 
		/// <see cref="QueryString"/> instance.</param>
		public QueryString(NameValueCollection collection)
		{
			if (collection == null)
				return;

			foreach (string key in collection)
			{
				if (string.IsNullOrEmpty(key))
					continue;

				this.Add(key, collection[key]);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString"/> class, using the specified cookie collection.
		/// </summary>
		/// <param name="collection">The <see cref="HttpCookieCollection"/> to copy to the new <see cref="QueryString"/> instance.</param>
		public QueryString(HttpCookieCollection collection)
		{
			if (collection == null)
				return;

			foreach (string key in collection)
			{
				if (string.IsNullOrEmpty(key))
					continue;

				this.Add(key, collection.Get(key).Value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString"/> class, and populates it with
		/// values parsed the specified with <paramref name="queryString"/>.
		/// </summary>
		/// <param name="queryString">The query string that contains the name/value pairs to use.</param>
		public QueryString(string queryString)
		{
			if (!string.IsNullOrEmpty(queryString))
				this.Parse(queryString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString" /> class, and populates it with
		/// values parsed the specified <paramref name="queryString" />.
		/// </summary>
		/// <param name="queryString">The query string that contains the name/value pairs to use.</param>
		/// <param name="splitChars">The characters to use to split the key/value pairs.</param>
		public QueryString(string queryString, char[] splitChars)
			: this(splitChars)
		{
			if (!string.IsNullOrEmpty(queryString))
				this.Parse(queryString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryString" /> class, and populates it with
		/// values parsed the specified <paramref name="queryString" />.
		/// </summary>
		/// <param name="queryString">The query string that contains the name/value pairs to use.</param>
		/// <param name="splitChar">The character to use to split the key/value pairs.</param>
		public QueryString(string queryString, char splitChar)
			: this(splitChar)
		{
			if (!string.IsNullOrEmpty(queryString))
				this.Parse(queryString);
		}

		/// <summary>
		/// Converts any characters invalid for an xml node name with an underscore character.
		/// </summary>
		/// <param name="name">Original node name.</param>
		/// <returns>A valid node name</returns>
		public static string ValidName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			string validName = Regex.Replace(name, @"[^\w\.\-]", "_");
			if (Regex.Match(validName.Substring(0, 1), @"[a-zA-Z_\-]").Success != true)
				validName = "_" + validName;

			return validName;
		}

		/// <summary>
		/// Parses the specified query string into this instance and returns this instance.
		/// </summary>
		/// <param name="queryString">The query string to parse.</param>
		/// <param name="pairSplitChars">The character(s) to use to split the pairs.</param>
		/// <param name="keyValueSplitChar">The character(s) to use to split keys and values.</param>
		/// <returns>The current instance.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="queryString" /> argument is <c>null</c>.</exception>
		/// <example><c>
		/// var coll = new <see cref="NameValueCollection" />();
		/// coll.ParseQueryString("a=12&amp;c=56&amp;color=red");
		/// </c></example>
		public QueryString Parse(string queryString, char[] pairSplitChars = null, char? keyValueSplitChar = null)
		{
			char[] pairSplitCharsLocal = pairSplitChars == null || pairSplitChars.Length == 0 ? this.pairSeparators : pairSplitChars;
			char kvSplitCharLocal = keyValueSplitChar == null ? this.keyValueSeparator : keyValueSplitChar.Value;

			this.Clear();

			if (queryString.IndexOf('?') == 0)
				queryString = queryString.Substring(1);

			string[] values = queryString.Split(pairSplitCharsLocal, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in values)
			{
				if (t == string.Empty)
					continue;

				int index = t.IndexOf(kvSplitCharLocal);
				if (index != -1)
				{
					string name = t.Substring(0, index);
					string value = t.Substring(index + 1);
					if (name != string.Empty)
						this.Add(name, value);
				}
				else
				{
					this.Add(t, null);
				}
			}

			return this;
		}

		/// <inheritdoc/>
		public void Parse(XmlElement element)
		{
			this.Clear();
			foreach (XmlAttribute attr in element.Attributes)
			{
				this[attr.LocalName] = attr.Value;
			}
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <returns>A new <see cref="QueryString"/> instance with identical data as this one.</returns>
		public QueryString Clone()
		{
			return new QueryString(this);
		}

		/// <summary>
		/// Merges the specified query into the current instance, overwriting any duplicates and returns this instance.
		/// </summary>
		/// <param name="query">The query whose values to merge into current instance.</param>
		/// <returns>The current instance.</returns>
		public QueryString Merge(NameValueCollection query)
		{
			return this.Merge(query, false);
		}

		/// <summary>
		/// Merges the specified query into the current instance, optionally keeping the duplicates from the original query and returns this instance.
		/// </summary>
		/// <param name="query">The query whose values to merge into current instance.</param>
		/// <param name="keepOriginalValues">If <c>true</c>, values from current query with keys that exist in 
		/// <paramref name="query"/> will be preserved. </param>
		/// <returns>The current instance.</returns>
		public QueryString Merge(NameValueCollection query, bool keepOriginalValues)
		{
			if (query != null)
			{
				foreach (string key in query.Keys)
				{
					if (this[key] != null && keepOriginalValues)
						continue;

					this[key] = query[key];
				}
			}

			return this;
		}

		/// <summary>
		/// Copies the entries in the specified <see cref="NameValueCollection"/> to the current instance and returns it.
		/// </summary>
		/// <param name="collection">The <see cref="NameValueCollection"/> to copy to the current <see cref="NameValueCollection"/>.</param>
		/// <returns>The current instance.</returns>
		public new QueryString Add(NameValueCollection collection)
		{
			base.Add(collection);
			return this;
		}

		/// <summary>
		/// Adds an entry with the specified name and value to the current instance and returns it.
		/// </summary>
		/// <param name="key">The <see cref="String"/> key of the entry to add. If the key is a <c>null</c>, no action will be done.</param>
		/// <param name="value">The <see cref="String"/> value of the entry to add. If the key is a <c>null</c>, the value added will be an empty string.</param>
		/// <returns>The current instance.</returns>
		public new QueryString Add(string key, string value)
		{
			this[key] = value;
			return this;
		}

		/// <summary>
		/// Sets the specified key/value pair and returns this instance.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns>The current instance.</returns>
		public new QueryString Set(string key, string value)
		{
			return this.Set(key, (object) value);
		}

		/// <summary>
		/// Sets the specified key/value pair and returns this instance.
		/// </summary>
		/// <param name="key">The key to set.</param>
		/// <param name="value">The value to set.</param>
		/// <returns>The current instance.</returns>
		public QueryString Set(string key, object value)
		{
			if (value == null)
				value = string.Empty;

			base.Set(key, value.ToString());
			return this;
		}

		/// <summary>
		/// Removes the specified key and returns this instance.
		/// </summary>
		/// <param name="key">The key to set.</param>
		/// <returns>The current instance.</returns>
		public new QueryString Remove(string key)
		{
			base.Remove(key);
			return this;
		}

		/// <summary>
		/// Returns <c>true</c> if the current query has a value with the specified <paramref name="key"/> and the value matches the 
		/// specified regular <paramref name="expression"/>.
		/// </summary>
		/// <param name="key">The key of the value to check.</param>
		/// <param name="expression">The regular expression to use for checking the value.</param>
		/// <returns>
		/// <c>true</c> if the specified value is valid; otherwise, <c>false</c>.
		/// </returns>
		public bool HasValid(string key, string expression)
		{
			if (this[key] == null)
				return false;

			if (Regex.Match(this[key], expression, RegexOptions.IgnoreCase).Success)
				return true;

			return false;
		}

		/// <summary>
		/// Determines whether the current <see cref="QueryString"/> has the specified <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>
		/// <c>true</c> if the current <see cref="QueryString"/> has the specified <paramref name="key"/>; otherwise, <c>false</c>.
		/// </returns>
		public bool Has(string key)
		{
			if (string.IsNullOrEmpty(key))
				return false;

			return this.Keys.Cast<string>().Count(t => t == key) != 0;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Boolean"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as a <see cref="Boolean"/></returns>
		public bool GetBoolean(string key)
		{
			bool result = false;
			string value = this[key];
			if (value != null)
				bool.TryParse(value, out result);

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Byte"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as a <see cref="Byte"/></returns>
		public byte GetByte(string key)
		{
			return this.GetByte(key, 0);
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Byte"/>, optionally setting it to the default value.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Byte"/>.</param>
		/// <returns>
		/// The specified value as a <see cref="Byte"/>
		/// </returns>
		public byte GetByte(string key, byte defaultValue)
		{
			if (this[key] == null)
				return defaultValue;

			byte result;
			if (!byte.TryParse(this[key], out result))
				return defaultValue;

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Byte"/>, optionally setting it to the default value, and ensuring that it falls within the specified range.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Byte"/>.</param>
		/// <param name="minValue">The minimum allowed value of the range.</param>
		/// <param name="maxValue">The maximum allowed value of the range.</param>
		/// <returns>
		/// The specified value as a <see cref="Byte"/>
		/// </returns>
		public byte GetByte(string key, byte defaultValue, byte minValue, byte maxValue)
		{
			byte result = this.GetByte(key, defaultValue);
			if (result < minValue) result = minValue;
			if (result > maxValue) result = maxValue;
			return result;
		}

		/// <summary>
		/// Gets the specified value as an <see cref="Int32"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as an <see cref="Int32"/></returns>
		public int GetInt(string key)
		{
			return this.GetInt(key, 0);
		}

		/// <summary>
		/// Gets the specified value as an <see cref="Int32"/>, optionally setting it to the default value.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Int32"/>.</param>
		/// <returns>
		/// The specified value as an <see cref="Int32"/>
		/// </returns>
		public int GetInt(string key, int defaultValue)
		{
			return (int) this.GetLong(key, defaultValue);
		}

		/// <summary>
		/// Gets the specified value as an <see cref="Int32"/>, optionally setting it to the default value, and ensuring that it falls within the specified range.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Int32"/>.</param>
		/// <param name="minValue">The minimum allowed value of the range.</param>
		/// <param name="maxValue">The maximum allowed value of the range.</param>
		/// <returns>
		/// The specified value as an <see cref="Int32"/>
		/// </returns>
		public int GetInt(string key, int defaultValue, int minValue, int maxValue)
		{
			int result = this.GetInt(key, defaultValue);
			if (result < minValue)
				result = minValue;
			if (result > maxValue)
				result = maxValue;

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Int64"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as a <see cref="Int64"/></returns>
		public long GetLong(string key)
		{
			return this.GetLong(key, 0);
		}

		/// <summary>
		/// Gets the specified value as an <see cref="Int64"/>, optionally setting it to the default value.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Int64"/>.</param>
		/// <returns>The specified value as an <see cref="Int64"/></returns>
		public long GetLong(string key, long defaultValue)
		{
			if (this[key] == null)
				return defaultValue;

			long result;
			if (!long.TryParse(this[key], out result))
				return defaultValue;

			return result;
		}

		/// <summary>
		/// Gets the specified value as an <see cref="Int64"/>, optionally setting it to the default value, and ensuring that it falls within the specified range.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c> or could not be converted to <see cref="Int64"/>.</param>
		/// <param name="minValue">The minimum allowed value of the range.</param>
		/// <param name="maxValue">The maximum allowed value of the range.</param>
		/// <returns>
		/// The specified value as an <see cref="Int64"/>
		/// </returns>
		public long GetLong(string key, long defaultValue, long minValue, long maxValue)
		{
			long result = this.GetLong(key, defaultValue);
			if (result < minValue)
				result = minValue;
			if (result > maxValue)
				result = maxValue;

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="Decimal"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as an <see cref="Decimal"/></returns>
		public decimal GetDecimal(string key)
		{
			decimal result = 0;
			if (this[key] != null)
				decimal.TryParse(this[key], out result);

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="float"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as an <see cref="float"/></returns>
		public float GetFloat(string key)
		{
			float result = 0;
			if (this[key] != null)
				float.TryParse(this[key], out result);

			return result;
		}

		/// <summary>
		/// Gets the specified value as a <see cref="String"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as an <see cref="String"/></returns>
		public string GetString(string key)
		{
			return this[key];
		}

		/// <summary>
		/// Gets the specified value as a <see cref="String"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c>.</param>
		/// <returns>The specified value as a <see cref="String"/></returns>
		public string GetString(string key, string defaultValue)
		{
			return this.GetString(key, defaultValue, false);
		}

		/// <summary>
		/// Gets the specified value as a <see cref="String"/>.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="defaultValue">The default value to return if the specified key is <c>null</c>.</param>
		/// <param name="emptyIsValid">If set to <c>true</c> an empty string with the specified key will be considered valid 
		/// and be returned instead of the default value.</param>
		/// <returns>
		/// The specified value as a <see cref="String"/>
		/// </returns>
		public string GetString(string key, string defaultValue, bool emptyIsValid)
		{
			string value = this.GetString(key);
			if (value == null)
				return defaultValue;

			if (value == string.Empty)
				return emptyIsValid ? value : defaultValue;

			return value;
		}

		/// <summary>
		/// Gets the specified value as a list of strings.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The specified value as a list of strings.</returns>
		public List<string> GetList(string key)
		{
			return this.GetList(key, null);
		}

		/// <summary>
		/// Gets the specified value as a list of strings, optionally checked against the specified expression.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="expression">Optional expression to use for checking the list elements.</param>
		/// <returns>The specified value as a list of strings.</returns>
		/// <remarks>
		/// The value with the specified <paramref name="key"/> is split on comma (','), and each of the values is then (optionally)
		/// checked against the specified <paramref name="expression"/>. If the value is checked it will only be added to the resulting list if
		/// if matches the <paramref name="expression"/>.
		/// </remarks>
		public List<string> GetList(string key, string expression)
		{
			Regex test = null;
			if (!string.IsNullOrEmpty(expression))
				test = new Regex(expression);

			List<string> list = new List<string>();
			string value = this.GetString(key);
			if (value != null)
			{
				string[] values = value.Split(',');
				list.AddRange(values.Where(t => test == null || test.Match(t).Success));
			}

			return list;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.ToString(string.Empty);
		}

		/// <summary>
		/// Returns a <see cref="String" /> that contains this collection's name/value pairs packed as a URL query string and prepended
		/// with the specified <paramref name="symbolToPrepend" />.
		/// </summary>
		/// <param name="symbolToPrepend">The symbol (typically '?' or '#' to prepend the query string with. For no prefix, pass
		/// an empty string.</param>
		/// <param name="keyValueSeparator">The key/value separator.</param>
		/// <param name="pairSeparator">The pair separator.</param>
		/// <returns>A <see cref="String" /> that contains this collection's name/value pairs packed as a URL query string.</returns>
		public string ToString(string symbolToPrepend, string pairSeparator = "&", string keyValueSeparator = "=")
		{
			string[] pairs = new string[this.Count];

			for (int i = 0; i < this.Count; i++)
			{
				string key = this.GetKey(i);
				string value = this[i];
				if (value == null)
					pairs[i] = key;
				else
					pairs[i] = string.Concat(key, keyValueSeparator, value);
			}

			string result = string.Join(pairSeparator.ToString(CultureInfo.InvariantCulture), pairs);
			if (result.Length != 0)
				return symbolToPrepend + result;

			return result;
		}

		/// <summary>
		/// Returns an <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes.
		/// </summary>
		/// <param name="document">The <see cref="XmlDocument"/> to use for creating the nodes.</param>
		/// <returns>An <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes.</returns>
		public XmlElement ToXml(XmlDocument document)
		{
			return this.ToXml(document, "query");
		}

		/// <summary>
		/// Returns an <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes, where the element's
		/// node name will be the specified <paramref name="nodeName"/>.
		/// </summary>
		/// <param name="document">The <see cref="XmlDocument"/> to use for creating the nodes.</param>
		/// <param name="nodeName">The name of the <see cref="XmlElement"/> to create.</param>
		/// <returns>An <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes.</returns>
		public XmlElement ToXml(XmlDocument document, string nodeName)
		{
			XmlElement xmlNode = document.CreateElement(nodeName);
			foreach (string name in this)
			{
				if (name == null)
					xmlNode.SetAttribute("value", this.Get(name));
				else
				{
					string attrName = ValidName(name);
					if (attrName != string.Empty)
						xmlNode.SetAttribute(attrName, this.Get(name));
				}
			}

			return xmlNode;
		}

		/// <summary>
		/// Returns an <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes, where the element's
		/// node name will be the specified with <paramref name="qualifiedName"/>, and it's namespace with <paramref name="nodeNamespace"/>.
		/// </summary>
		/// <param name="document">The <see cref="XmlDocument"/> to use for creating the nodes.</param>
		/// <param name="qualifiedName">The qualified name of the <see cref="XmlElement"/> to create.</param>
		/// <param name="nodeNamespace">The XML namespace of the element to create.</param>
		/// <returns>An <see cref="XmlElement"/> that contains this collection's name/value pairs set as attributes.</returns>
		public XmlElement ToXml(XmlDocument document, string qualifiedName, string nodeNamespace)
		{
			XmlElement xmlNode = document.CreateElement(qualifiedName, nodeNamespace);
			foreach (string name in this)
			{
				if (name == null)
					continue;

				string attrName = ValidName(name);
				if (attrName != string.Empty)
					xmlNode.SetAttribute(attrName, this.Get(name));
			}

			return xmlNode;
		}
	}
}
