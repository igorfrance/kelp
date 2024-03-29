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
namespace Kelp.Extensions
{
	using System;
	using System.Collections.Specialized;
	using System.Diagnostics.Contracts;
	using System.Text;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Defines extension methods for strings.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Matches curly brace surrounded placeholder texts, such as <c>{example}</c>.
		/// </summary>
		public static readonly Regex SubstitutionExpression = new Regex(@"\{([^}{]+)\}", RegexOptions.Compiled);
		private static readonly Regex whitespaceExpr = new Regex(@"[\s\t\r\n]+", RegexOptions.Compiled);

		/// <summary>
		/// Formats the current string instance using the static string.Format method.
		/// </summary>
		/// <param name="subject">The string being formatted.</param>
		/// <param name="replacements">The substitution values.</param>
		/// <returns>Formatted <paramref name="subject"/>. 
		/// </returns>
		public static string F(this string subject, params object[] replacements)
		{
			if (string.IsNullOrWhiteSpace(subject) || replacements == null)
				return subject;

			return string.Format(subject, replacements);
		}

		/// <summary>
		/// Returns a value indicating whether the specified <paramref name="value"/> occurs within this string.
		/// </summary>
		/// <param name="subject">The string being extended.</param>
		/// <param name="value">The value to look for.</param>
		/// <param name="caseSensitive">If true, the check is being performed case-insensitive.</param>
		/// <returns><c>true</c> if the <paramref name="value"/> parameter occurs within this string, or if value is 
		/// the empty string (""); otherwise, <c>false</c>. 
		/// </returns>
		public static bool Contains(this string subject, string value, bool caseSensitive)
		{
			if (subject == null || value == null)
				return false;

			if (value == string.Empty)
				return true;

			if (caseSensitive)
				return subject.ToLower().Contains(value.ToLower());

			return subject.Contains(value);
		}

		/// <summary>
		/// Returns <c>true</c> if the string contains any one of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool ContainsAnyOf(this string subject, params string[] values)
		{
			return subject.ContainsAnyOf(false, values);
		}

		/// <summary>
		/// Returns <c>true</c> if the string contains any one of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="caseInSensitive">If this argument is <c>true</c>, the search will ignore case.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool ContainsAnyOf(this string subject, bool caseInSensitive, params string[] values)
		{
			string compare = !caseInSensitive ? subject : subject.ToLower();
			foreach (string test in values)
			{
				if (compare.Contains(!caseInSensitive ? test : test.ToLower()))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns <c>true</c> if the string contains all of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool ContainsAllOf(this string subject, params string[] values)
		{
			return subject.ContainsAllOf(false, values);
		}

		/// <summary>
		/// Returns <c>true</c> if the string contains all of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="caseInSensitive">If this argument is <c>true</c>, the search will ignore case.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool ContainsAllOf(this string subject, bool caseInSensitive, params string[] values)
		{
			string compare = !caseInSensitive ? subject : subject.ToLower();
			bool result = true;
			foreach (string test in values)
			{
				if (!compare.Contains(!caseInSensitive ? test : test.ToLower()))
				{
					result = false;
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the number of occurrences of value <paramref name="text"/> within the current <paramref name="subject"/>.
		/// </summary>
		/// <param name="subject">The string to be searched</param>
		/// <param name="text">The value to search for</param>
		/// <returns>The number of occurrences of value <paramref name="text"/> within the current <paramref name="subject"/>.</returns>
		public static int CountOf(this string subject, string text)
		{
			if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(text))
				return 0;

			int count = 0;
			int index = -1;
			while ((index = subject.IndexOf(text, index + 1, StringComparison.InvariantCultureIgnoreCase)) != -1)
				count++;

			return count;
		}

		/// <summary>
		/// Returns <c>true</c> if the string equals any one of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool EqualsAnyOf(this string subject, params string[] values)
		{
			return subject.EqualsAnyOf(false, values);
		}

		/// <summary>
		/// Returns <c>true</c> if the string equals any one of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="caseInSensitive">If this argument is <c>true</c>, the search will ignore case.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool EqualsAnyOf(this string subject, bool caseInSensitive, params string[] values)
		{
			if (string.IsNullOrEmpty(subject))
				return false;

			string compare = !caseInSensitive ? subject : subject.ToLower();
			foreach (string test in values)
			{
				if (compare == (!caseInSensitive ? test : test.ToLower()))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Escapes all characters in <paramref name="subject"/> that may be treated as meta characters (such as '\.^$*+?(){['),
		/// so that it can be interpreted as-is in regular expressions. 
		/// </summary>
		/// <param name="subject">The subject to escape.</param>
		/// <returns>The escaped version of <paramref name="subject"/>.</returns>
		public static string EscapeMeta(this string subject)
		{
			if (subject == null)
				return null;

			return Regex.Replace(subject, @"([\\\.\^\$\*\+\?\(\)\[\{])", @"\$1");
		}

		/// <summary>
		/// Searches the specified <paramref name="subject"/> for the first occurrence of the specified regular 
		/// <paramref name="expression"/>.
		/// </summary>
		/// <param name="subject">The string to search for a match.</param>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <returns>
		/// An object that contains information about the match.
		/// </returns>
		public static Match Match(this string subject, string expression)
		{
			return Match(subject, expression, RegexOptions.None);
		}

		/// <summary>
		/// Searches the specified <paramref name="subject"/> for the first occurrence of the specified regular
		/// <paramref name="expression"/>.
		/// </summary>
		/// <param name="subject">The string to search for a match.</param>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
		/// <returns>
		/// An object that contains information about the match.
		/// </returns>
		public static Match Match(this string subject, string expression, RegexOptions options)
		{
			if (subject == null || string.IsNullOrEmpty(expression))
				return null;

			return Regex.Match(subject, expression);
		}

		/// <summary>
		/// A shortcut for <c>(x, y) => string.IsNullOrWhiteSpace(x) ? y : x;</c>
		/// </summary>
		/// <param name="subject">The string to use if not null or whitespace.</param>
		/// <param name="alternative">The string to use if <paramref name="subject"/> is null or whitespace.</param>
		/// <returns>string.IsNullOrWhiteSpace(x) ? y : x</returns>
		public static string Or(this string subject, string alternative)
		{
			return string.IsNullOrWhiteSpace(subject) ? alternative : subject;
		}

		/// <summary>
		/// Replaces the string matching the specified regular <paramref name="expression"/> string with the specified 
		/// <paramref name="replacement"/> string.
		/// </summary>
		/// <param name="subject">The string to replace.</param>
		/// <param name="expression">The pattern to initialize the regular expression with.</param>
		/// <param name="replacement">The replacement value string.</param>
		/// <returns>The original string with all substrings matching <paramref name="expression"/> replaced with
		/// the specified <paramref name="replacement"/> string.</returns>
		public static string ReplaceAll(this string subject, string expression, string replacement)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(expression));
			Contract.Requires<ArgumentNullException>(replacement != null);

			if (string.IsNullOrEmpty(subject))
				return subject;

			return ReplaceAll(subject, new Regex(expression), replacement);
		}

		/// <summary>
		/// Replaces the string matching the specified regular <paramref name="expression"/> string using the specified 
		/// <paramref name="replacement"/> function to further process the match.
		/// </summary>
		/// <param name="subject">The string to replace.</param>
		/// <param name="expression">The pattern to initialize the regular expression with.</param>
		/// <param name="replacement">The replacement function.</param>
		/// <returns>The original string with all substrings matching <paramref name="expression"/> replaced using the specified 
		/// <paramref name="replacement"/> function to further process the match.</returns>
		public static string ReplaceAll(this string subject, string expression, MatchEvaluator replacement)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(expression));
			Contract.Requires<ArgumentNullException>(replacement != null);

			if (string.IsNullOrEmpty(subject))
				return subject;

			return ReplaceAll(subject, new Regex(expression), replacement);
		}

		/// <summary>
		/// Replaces the string matching the specified regular <paramref name="expression"/> with the specified 
		/// <paramref name="replacement"/> string.
		/// </summary>
		/// <param name="subject">The string to replace.</param>
		/// <param name="expression">The regular expression to use.</param>
		/// <param name="replacement">The replacement value string.</param>
		/// <returns>The original string with all substrings matching <paramref name="expression"/> replaced with
		/// the specified <paramref name="replacement"/> string.</returns>
		public static string ReplaceAll(this string subject, Regex expression, string replacement)
		{
			Contract.Requires<ArgumentNullException>(expression != null);
			Contract.Requires<ArgumentNullException>(replacement != null);

			if (string.IsNullOrEmpty(subject))
				return subject;

			return expression.Replace(subject, replacement);
		}

		/// <summary>
		/// Replaces the string matching the specified regular <paramref name="expression"/> using the specified 
		/// <paramref name="replacement"/> function to further process the match.
		/// </summary>
		/// <param name="subject">The string to replace.</param>
		/// <param name="expression">The regular expression to use.</param>
		/// <param name="replacement">The replacement function.</param>
		/// <returns>The original string with all substrings matching <paramref name="expression"/> replaced using the specified 
		/// <paramref name="replacement"/> function to further process the match.</returns>
		public static string ReplaceAll(this string subject, Regex expression, MatchEvaluator replacement)
		{
			Contract.Requires<ArgumentNullException>(expression != null);
			Contract.Requires<ArgumentNullException>(replacement != null);

			if (string.IsNullOrEmpty(subject))
				return subject;

			return expression.Replace(subject, replacement);
		}

		/// <summary>
		/// Repeats the specified <paramref name="subject"/> the specified <paramref name="count"/> of times.
		/// </summary>
		/// <param name="subject">The subject to repeat.</param>
		/// <param name="count">The number of times to repeat the subject.</param>
		/// <returns>the specified <paramref name="subject"/> repeated the specified <paramref name="count"/> of times.</returns>
		public static string Repeat(this string subject, int count)
		{
			Contract.Requires<ArgumentNullException>(count > 0);

			if (string.IsNullOrEmpty(subject))
				return subject;

			if (count == 1)
				return subject;

			StringBuilder result = new StringBuilder(subject);
			for (int i = 1; i < count; i++)
				result.Append(subject);

			return result.ToString();
		}

		/// <summary>
		/// Reverses the characters of the string.
		/// </summary>
		/// <param name="subject">The original string.</param>
		/// <returns>The reversed string</returns>
		public static string Rev(this string subject)
		{
			if (subject == null)
				return null;

			// this was posted by petebob as well 
			char[] array = subject.ToCharArray();
			Array.Reverse(array);
			return new String(array);
		}

		/// <summary>
		/// Searches the specified <paramref name="subject"/> for placeholders such as <code>{name}</code> 
		/// and replaces them with values with matching keys in the specified <paramref name="values"/>.
		/// </summary>
		/// <param name="subject">The string to substitute.</param>
		/// <param name="values">The substitution values.</param>
		/// <returns>The substituted <paramref name="subject"/>.</returns>
		public static string Substitute(this string subject, NameValueCollection values)
		{
			if (string.IsNullOrWhiteSpace(subject))
				return subject;

			return SubstitutionExpression.Replace(subject, (m) => values[m.Groups[1].Value] ?? m.Groups[0].Value);
		}

		/// <summary>
		/// Searches the specified <paramref name="subject"/> for placeholders such as <code>{name}</code> 
		/// and replaces them with values with matching keys in the <see cref="NameValueCollection"/> parsed from the specified
		/// string <paramref name="values"/>.
		/// </summary>
		/// <param name="subject">The string to substitute.</param>
		/// <param name="values">The substitution values packed as a string.</param>
		/// <returns>The substituted <paramref name="subject"/>.</returns>
		public static string Substitute(this string subject, string values)
		{
			return Substitute(subject, new QueryString(values));
		}

		/// <summary>
		/// Sanitizes the specified subject of any unnecessary whitespace.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The sanitized subject</returns>
		/// <remarks>The unnecessary whitespace are any tabs, line breaks or multiple consecutive spaces.</remarks>
		public static string Sanitize(this string subject)
		{
			if (string.IsNullOrEmpty(subject))
				return subject;

			return whitespaceExpr.Replace(subject, " ").Trim();
		}

		/// <summary>
		/// Returns the specified <paramref name="instance"/> with the first letter converted to upper case.
		/// </summary>
		/// <param name="instance">The value to process.</param>
		/// <returns>The original string, with the first letter converted to upper case.</returns>
		public static string ToUpperCaseFirst(this string instance)
		{
			if (string.IsNullOrEmpty(instance))
				return instance;

			string temp = instance.Substring(0, 1);
			return temp.ToUpper() + instance.Substring(1);
		}
	}
}
