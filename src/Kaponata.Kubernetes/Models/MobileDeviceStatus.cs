﻿// <copyright file="MobileDeviceStatus.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Newtonsoft.Json;

#nullable disable

namespace Kaponata.Kubernetes.Models
{
    /// <summary>
    /// Describes the status of a <see cref="MobileDevice"/> object.
    /// </summary>
    public class MobileDeviceStatus
    {
        /// <summary>
        /// Gets or sets, when available, the name of a service or IP address of a pod which hosts a VNC server
        /// which allows users to remotely control this device.
        /// </summary>
        [JsonProperty(PropertyName = "vncHost")]
        public string VncHost { get; set; }

        /// <summary>
        /// Gets or sets, when available, a password which can be used to connect to the VNC server at
        /// <see cref="VncHost"/>.
        /// </summary>
        [JsonProperty(PropertyName = "vncPassword")]
        public string VncPassword { get; set; }
    }
}
