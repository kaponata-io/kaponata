﻿// <copyright file="AVDictionaryHelpersTests.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Kaponata.Multimedia.FFMpeg;
using System;
using Xunit;
using NativeAVDictionary = FFmpeg.AutoGen.AVDictionary;

namespace Kaponata.Multimedia.Tests
{
    /// <summary>
    /// Tests the <see cref="AVDictionaryHelpers"/> class.
    /// </summary>
    public unsafe class AVDictionaryHelpersTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AVDictionaryHelpersTests"/> class.
        /// </summary>
        public AVDictionaryHelpersTests()
        {
            FFMpegClient.Initialize();
        }

        /// <summary>
        /// The <see cref="AVDictionaryHelpers.ToReadOnlyDictionary(NativeAVDictionary*)"/> method returns the native dictionary.
        /// </summary>
        [Fact]
        public void ToReadOnlyDictionary_ReturnsDictionary()
        {
            var nativeDictionary = new NativeAVDictionary
            {
            };
            var ptr = new IntPtr(&nativeDictionary);

            FFmpeg.AutoGen.ffmpeg.av_dict_set((NativeAVDictionary**)&ptr, "foo", "bar", 0);
            var dict = AVDictionaryHelpers.ToReadOnlyDictionary(&nativeDictionary);
            var entry = Assert.Single(dict);
            Assert.Equal("foo", entry.Key);
            Assert.Equal("bar", entry.Value);
        }
    }
}
