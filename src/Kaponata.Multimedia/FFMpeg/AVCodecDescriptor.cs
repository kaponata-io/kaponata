﻿// <copyright file="AVCodecDescriptor.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using FFmpeg.AutoGen;
using NativeAVCodecDescriptor = FFmpeg.AutoGen.AVCodecDescriptor;

namespace Kaponata.Multimedia.FFMpeg
{
    /// <summary>
    /// Describes an instance of an audio or video codec.
    /// </summary>
    /// <seealso href="https://ffmpeg.org/doxygen/2.5/structAVCodecDescriptor.html"/>
    public unsafe class AVCodecDescriptor
    {
        private readonly NativeAVCodecDescriptor* native;

        /// <summary>
        /// Initializes a new instance of the <see cref="AVCodecDescriptor"/> class.
        /// </summary>
        /// <param name="native">
        /// The underlying native instance.
        /// </param>
        public AVCodecDescriptor(NativeAVCodecDescriptor* native)
        {
            this.native = native;
        }

        /// <summary>
        /// Gets the <see cref="AVCodecID"/> of the codec.
        /// </summary>
        public AVCodecID Id
        {
            get { return native->id; }
        }

        /// <summary>
        /// Gets the long name of the codec.
        /// </summary>
        public string LongName
        {
            get { return new string((sbyte*)this.native->long_name); }
        }

        /// <summary>
        /// Gets the name of the codec.
        /// </summary>
        public string Name
        {
            get { return new string((sbyte*)this.native->name); }
        }

        /// <summary>
        /// Gets additional properties which describe the codec.
        /// </summary>
        public AVCodecProps Props
        {
            get { return (AVCodecProps)this.native->props; }
        }

        /// <summary>
        /// Gets the type of the codec.
        /// </summary>
        public AVMediaType Type
        {
            get { return this.native->type; }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
