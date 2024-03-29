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
namespace Kelp.Test
{
	// ReSharper disable InconsistentNaming
	// ReSharper disable UnusedMember.Global
	// ReSharper disable UnusedMember.Local

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Machine.Specifications;

	[Subject(typeof(Util)), Tags(Categories.Utilities)]
	public class When_splitting_arguments
	{
		private static IEnumerable<string> arguments;

		Because of = () => arguments = Util.SplitArguments(',',
			@"',', a, 45mn, 'z', 12, 'Hello, world!', ""Quoted arg"", 'Igor\'s World', ""Igor's \""World\"""", 'He said: \'Hello\''");

		It Should_produce_the_correct_number_of_parameters = () => arguments.Count().ShouldEqual(10);
		It Should_handle_starting_quotations = () => arguments.ElementAt(0).ShouldEqual(",");
		It Should_handle_quotation_delimited_parameter = () => arguments.ElementAt(6).ShouldEqual("Quoted arg");
		It Should_handle_apostrophe_delimited_parameter = () => arguments.ElementAt(3).ShouldEqual("z");
		It Should_handle_embedded_separator_as_string = () => arguments.ElementAt(5).ShouldEqual("Hello, world!");
		It Should_handle_escaped_quotation_as_string = () => arguments.ElementAt(7).ShouldEqual("Igor's World");
		It Should_handle_escaped_apostrophe_as_string = () => arguments.ElementAt(8).ShouldEqual("Igor's \"World\"");
		It Should_ignore_unnecessary_escapes = () => arguments.ElementAt(9).ShouldEqual("He said: 'Hello'");
	}

	[Subject(typeof(Util)), Tags(Categories.Utilities)]
	public class When_getting_assembly_date
	{
		private static DateTime assemblyDate;
		private static Action<object> log = o => Console.WriteLine(o == null ? "null" : o.ToString());

		Because of = () =>
			assemblyDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

		It Should_return_null_for_null_assembly = () =>
			Util.GetAssemblyDate(null).ShouldEqual(null);

		It Should_return_proper_date_for_executing_assembly = () =>
			Util.GetAssemblyDate(Assembly.GetExecutingAssembly()).Value.Ticks.ShouldEqual(assemblyDate.Ticks);
	}
}
