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
namespace Kelp.Test.Extensions
{
	using System;

	using Kelp.Extensions;
	using Machine.Specifications;

	[Subject(typeof(DateTimeExtensions)), Tags(Categories.Core)]
	public class When_using_the_datetime_extensions
	{
		private static DateTime x;

		private Establish ctx = () =>
		{
			x = new DateTime(2000, 1, 1);
		};

		private It Should_correctly_add_two_days = () => 
			x.Offset("+2d").Day.ShouldEqual(3);

		private It Should_correctly_add_two_years = () => 
			x.Offset("+2y").Year.ShouldEqual(2002);

		private It Should_correctly_substract_four_months = () => 
			x.Offset("-4M").Month.ShouldEqual(9);

		private It Should_correctly_substract_four_months_and_change_the_year = () => 
			x.Offset("-4M").Year.ShouldEqual(1999);

		private It Should_correctly_add_1000_years = () => 
			x.Offset("1000y").Year.ShouldEqual(3000);
	}
}