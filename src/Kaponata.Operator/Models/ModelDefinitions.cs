﻿// <copyright file="ModelDefinitions.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using k8s;
using k8s.Models;
using System.IO;

namespace Kaponata.Operator.Models
{
    /// <summary>
    /// Provides access to the <see cref="V1CustomResourceDefinition"/> used by Kaponata.
    /// </summary>
    public static class ModelDefinitions
    {
        /// <summary>
        /// Gets the <see cref="V1CustomResourceDefinition"/> for the MobileDevice type.
        /// </summary>
        public static V1CustomResourceDefinition MobileDevice
        {
            get
            {
                using (Stream stream = typeof(ModelDefinitions).Assembly.GetManifestResourceStream("Kaponata.Operator.Models.MobileDevice.yaml"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return Yaml.LoadFromString<V1CustomResourceDefinition>(reader.ReadToEnd());
                }
            }
        }
    }
}