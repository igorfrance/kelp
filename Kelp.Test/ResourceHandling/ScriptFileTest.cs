﻿// <auto-generated>Marked as auto-generated so StyleCop will ignore BDD style tests</auto-generated>
/**
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
namespace Kelp.Test.ResourceHandling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Threading;

	using Kelp.ResourceHandling;
	using Machine.Specifications;

	using Microsoft.Ajax.Utilities;

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_getting_an_including_file : CodeFileTest
	{
		private static string content;
		private static ScriptFile subject;

		private Establish context = () =>
		{
			subject = CodeFile.Create<ScriptFile>(Utilities.GetScriptPath("script1.js"));
		};

		private It Should_contain_included_file1 = () => subject.Content.ShouldContain("Test.ExampleClass1");
		private It Should_contain_included_file2 = () => subject.Content.ShouldContain("Test.ExampleClass2");
		private It Should_contain_included_file3 = () => subject.Content.ShouldContain("Test.ExampleClass3");
		private It Should_save_the_resulting_file_to_cache = () => File.Exists(subject.CacheName).ShouldBeTrue();
		private It Should_include_files_in_proper_order = () =>
		{
			IEnumerable<string> keys = subject.References.Keys;
			keys.ElementAt(0).ShouldContain("include1");
			keys.ElementAt(1).ShouldContain("include3");
			keys.ElementAt(2).ShouldContain("include2");
			keys.ElementAt(3).ShouldContain("script1");
		};

		private It Should_preserve_the_correct_include_after_files_change = () =>
		{
			IEnumerable<string> keys = subject.References.Keys;
			keys.ElementAt(0).ShouldContain("include1");
			keys.ElementAt(1).ShouldContain("include3");
			keys.ElementAt(2).ShouldContain("include2");
			keys.ElementAt(3).ShouldContain("script1");

			string testString = string.Format("// NOW: {0}\r\n", DateTime.Now.Ticks);
			string scriptPath = Utilities.GetScriptPath("include2.js");

			Thread.Sleep(10);
			File.AppendAllText(scriptPath, testString);

			ScriptFile other = CodeFile.Create<ScriptFile>( 
				new ScriptFileConfiguration 
				{ 
					Settings = new CodeSettings { MinifyCode = false }
				});

			other.Load(Utilities.GetScriptPath("script1.js"));
			other.Content.ShouldContain(testString);

			IEnumerable<string> keys2 = other.References.Keys;
			keys2.ElementAt(0).ShouldContain("include1");
			keys2.ElementAt(1).ShouldContain("include3");
			keys2.ElementAt(2).ShouldContain("include2");
			keys2.ElementAt(3).ShouldContain("script1");
		};

	}

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_getting_a_file_with_includes_itself : CodeFileTest
	{
		private static string content;

		private It Should_throw_an_InvalidOperationException = () => Catch.Exception(delegate {
			ScriptFile subject = (ScriptFile)
			CodeFile.Create(Utilities.GetScriptPath("script3.js"), "script3.js");
			string content = subject.Content;
		})
		.ShouldBeAssignableTo<InvalidOperationException>();
	}

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_minifying_script_files : CodeFileTest
	{
		private static ScriptFile subject = (ScriptFile) CodeFile.Create(
			Utilities.GetScriptPath("compression1.js"), "compression.js");

		private It Multiline_strings_should_be_concatenated = () =>
			subject.Minify(Utilities.GetScriptContents("multiline-string1.js")).ShouldNotContain("\n");

		private It Multiline_strings_with_backslash_syntax_should_not_result_in_errors = () =>
		{
			var minified = subject.Minify(Utilities.GetScriptContents("multiline-string2.js"));
			minified.ShouldNotContain("\n");
		};

		private It Should_remove_whitespace_from_expressions = () =>
			subject.Minify(Utilities.GetScriptContents("formatting3.js")).ShouldContain("8/12+4");

		private It Should_not_remove_whitespace_from_regex = () =>
			subject.Minify(Utilities.GetScriptContents("formatting3.js")).ShouldContain("/12  87/");

		private It Should_not_remove_whitespace_from_regex_function_call = () =>
			subject.Minify(Utilities.GetScriptContents("formatting3.js")).ShouldContain("callFx(/6 \\//)");

		private It Should_not_remove_whitespace_from_negated_regex_expression = () =>
			subject.Minify(Utilities.GetScriptContents("formatting3.js")).ShouldContain("MSIE (5");
	}

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_getting_a_file_that_exists_in_cache_already : CodeFileTest
	{
		private static string scriptName = "script1.js";
		private static string scriptPath = Utilities.GetScriptPath(scriptName);
		private static string contents;
		private static ScriptFile subject;

		private Establish ctx = () =>
		{
			subject = (ScriptFile) CodeFile.Create(scriptPath, scriptName);
			contents = subject.Content;
		};

		private It Should_use_cached_file_as_long_an_it_is_newer_than_original = () =>
		{
			// this will ensure the cached file is newer than original
			using (StreamWriter writer = File.AppendText(subject.CacheName))
			{
				writer.WriteLine("Cached line");
				writer.Close();
			}

			subject = (ScriptFile) CodeFile.Create(scriptPath, scriptName);
			subject.Content.ShouldContain("Cached line");
		};

		private It Should_use_original_instead_of_cache_file_if_original_was_changed = () =>
		{
			File.SetLastWriteTime(scriptPath, DateTime.Now);
			File.SetLastWriteTime(subject.CacheName, DateTime.Now.AddDays(-1));
			
			Thread.Sleep(1000);
			CodeFile.Create(scriptPath).Content.ShouldNotContain("Cached line");
		};
	}

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_requesting_a_minified_js_file_with_whitespace : CodeFileTest
	{
		private const string ScriptName = "formatting1.js";
		private static readonly string scriptPath = Utilities.GetScriptPath(ScriptName);
		private static string contents;
		private static ScriptFile proc;

		private Establish ctx = () =>
		{
			proc = CodeFile.Create<ScriptFile>(scriptPath);
		};

		private It Should_remove_whitespace_and_comments = () =>
			proc.Content.ShouldEqual("var rAnimate={isBusy:!1,Move:{}}");
	}

	[Subject(typeof(ScriptFile)), Tags(Categories.ResourceHandling)]
	public class When_requesting_a_non_minified_empty_js_file : CodeFileTest
	{
		private const string ScriptName = "empty.js";
		private static readonly string scriptPath = Utilities.GetScriptPath(ScriptName);
		private static string contents;
		private static ScriptFile proc;

		private Establish ctx = () =>
		{
			proc = new ScriptFile();
			proc.Load(scriptPath, ScriptName);
		};

		private Because of = () => contents = proc.Content;

		private It Should_not_be_null_or_throw_an_error = () => contents.ShouldNotBeNull();
	}

	[Subject(typeof(CodeFile)), Tags(Categories.ResourceHandling)]
	public class When_appending_file_using_its_api : CodeFileTest
	{
		private static ScriptFile proc;
		private const string ScriptName = "empty.js";
		private static readonly string scriptPath = Utilities.GetScriptPath(ScriptName);

		private Establish ctx = () =>
		{
			proc = new ScriptFile();
			proc.Load(scriptPath, ScriptName);
			proc.AddFile(Utilities.GetScriptPath("simple1.js"));
			proc.AddFile(Utilities.GetScriptPath("simple2.js"));
		};

		private It Should_contain_content_of_included_files = () =>
		{
			proc.Content.ShouldContain("var x");
			proc.Content.ShouldContain("Math.max");
		};
	}
}
