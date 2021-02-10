﻿// <copyright file="FFMpegClient.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using NativeAVCodec = FFmpeg.AutoGen.AVCodec;
using NativeAVCodecDescriptor = FFmpeg.AutoGen.AVCodecDescriptor;

namespace Kaponata.Multimedia.FFMpeg
{
    /// <summary>
    /// Implements FFmpeg methods.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element must begin with upper-case letter", Justification = "Native function names.")]
    public partial class FFMpegClient
    {
        /// <summary>
        /// Lists the available codecs IDs. There can be multiple codecs (e.g. for decoding or encoding, or with and without hardware acceleration)
        /// for a given codec ID (e.g. H264). Use <see cref="GetAvailableCodecs"/> to get an extensive list.
        /// </summary>
        /// <returns>
        /// A list of available codecs IDs.
        /// </returns>
        public unsafe List<AVCodecDescriptor> GetAvailableCodecIDs()
        {
            List<AVCodecDescriptor> codecs = new List<AVCodecDescriptor>();
            NativeAVCodecDescriptor* descriptor = null;

            while (true)
            {
                descriptor = ffmpeg.avcodec_descriptor_next(descriptor);

                if (descriptor == null)
                {
                    break;
                }

                codecs.Add(new AVCodecDescriptor(descriptor));
            }

            return codecs;
        }

        /// <summary>
        /// Lists all available codecs. This list can include multiple codecs for the same codec ID.
        /// </summary>
        /// <returns>
        /// A list of all available codecs.
        /// </returns>
        public unsafe List<AVCodec> GetAvailableCodecs()
        {
            var codecs = new List<AVCodec>();

            void* iter = null;
            NativeAVCodec* codec;

            while ((codec = ffmpeg.av_codec_iterate(&iter)) != null)
            {
                var handle = new AVCodecHandle(codec);
                codecs.Add(new AVCodec(handle));
            }

            return codecs;
        }

        /// <summary>
        /// Find a registered decoder with a matching codec ID.
        /// </summary>
        /// <param name="id">
        /// AVCodecID of the requested decoder.
        /// </param>
        /// <returns>
        /// The <see cref="AVCodec"/> mathcing the <see cref="AVCodecID"/>.
        /// </returns>
        public unsafe AVCodec avcodec_find_decoder(AVCodecID id)
        {
            var handle = new AVCodecHandle(ffmpeg.avcodec_find_decoder(id));
            return new AVCodec(handle);
        }
    }
}
