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
	using System.Drawing;
	using System.IO;
	using System.Linq;

	using Kelp.Imaging.Filters;
	using Kelp.ResourceHandling;

	using Machine.Specifications;

	[Subject(typeof(ImageFile))]
	public class ImageFileTest
	{
		~ImageFileTest()
		{
			Utilities.ClearTemporaryDirectory();
		}

		It Should_support_registered_text_based_file_extensions = () =>
		{
			ImageFile.IsFileExtensionSupported("jpg").ShouldBeTrue();
			ImageFile.IsFileExtensionSupported("png").ShouldBeTrue();
			ImageFile.IsFileExtensionSupported("txt").ShouldBeFalse();
			ImageFile.IsFileExtensionSupported("js").ShouldBeFalse();
		};
	}

	[Subject(typeof(ImageFile)), Tags(Categories.ResourceHandling)]
	public class When_opening_a_file_with_many_querystring_parameters : ImageFileTest
	{
		private static ImageFile subject;
		private static int byteCount;
		private static string imagePath = Utilities.GetImagePath("illustration4.jpg");
		private static string queryString = "rs=410,310,0&cp=0,0,400,300&bt=1&ct=1&gm=1&mh=1&mv=1&gs=1&hsl=1,2,3&rgb=4,5,6&se=1&sx=1";

		Because bcs = () =>
		{
			subject = ImageFile.Create(Utilities.GetImagePath("illustration4.jpg"), queryString);
			byteCount = subject.Bytes.Length;
		};

		It Should_contain_brightness_filter = () => subject.Filter.Filters.Count(f => f is BrightnessMatrix).ShouldEqual(1);

		It Should_contain_contrast_filter = () => subject.Filter.Filters.Count(f => f is ContrastMatrix).ShouldEqual(1);

		It Should_contain_gamma_filter = () => subject.Filter.Filters.Count(f => f is GammaMatrix).ShouldEqual(1);

		It Should_contain_mirrorh_filter = () => subject.Filter.Filters.Count(f => f is MirrorH).ShouldEqual(1);

		It Should_contain_mirrorv_filter = () => subject.Filter.Filters.Count(f => f is MirrorV).ShouldEqual(1);

		It Should_contain_grayscape_filter = () => subject.Filter.Filters.Count(f => f is GrayscaleMatrix).ShouldEqual(1);

		It Should_contain_hsl_filter = () => subject.Filter.Filters.Count(f => f is HSLFilter).ShouldEqual(1);

		It Should_contain_color_filter = () => subject.Filter.Filters.Count(f => f is ColorBalance).ShouldEqual(1);

		It Should_contain_sepia_filter = () => subject.Filter.Filters.Count(f => f is SepiaMatrix).ShouldEqual(1);

		It Should_contain_sharpenx_filter = () => subject.Filter.Filters.Count(f => f is GaussianSharpen).ShouldEqual(1);

		It Should_generate_cached_image = () => File.Exists(subject.CachePath).ShouldBeTrue();

		It Should_resize_image_to_400x300 = () => 
		{
			var bm = new Bitmap(subject.CachePath);
			bm.Width.ShouldEqual(400);
			bm.Height.ShouldEqual(300);
			bm.Dispose();
		};
	}
}
