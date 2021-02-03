﻿// <copyright file="ChildOperatorIntegrationTests.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using k8s.Models;
using Kaponata.Operator.Kubernetes;
using Kaponata.Operator.Kubernetes.Polyfill;
using Kaponata.Operator.Models;
using Kaponata.Operator.Operators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kaponata.Operator.Tests.Operators
{
    /// <summary>
    /// Integration tests for the <see cref="ChildOperator{TParent, TChild}"/> class.
    /// </summary>
    public class ChildOperatorIntegrationTests
    {
        private readonly IHost host;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildOperatorIntegrationTests"/> class.
        /// </summary>
        /// <param name="output">
        /// A <see cref="ITestOutputHelper"/> which captures test output.
        /// </param>
        public ChildOperatorIntegrationTests(ITestOutputHelper output)
        {
            var builder = new HostBuilder();
            builder.ConfigureServices(
                (services) =>
                {
                    services.AddKubernetes();
                    services.AddLogging(
                        (loggingBuilder) =>
                        {
                            loggingBuilder.AddXunit(output);
                        });
                });

            this.host = builder.Build();
        }

        /// <summary>
        /// The <see cref="ChildOperator{TParent, TChild}"/> creates a new child object for items which it monitors, but does not act
        /// on items which do not match the label or field selectors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [Trait("TestCategory", "IntegrationTest")]
        public async Task Session_CreatesAndDeletesPod_Async()
        {
            const string name = "session-createsanddeletespod-async";
            Debug.Assert(FormatName(nameof(this.Session_CreatesAndDeletesPod_Async)) == name, "Use naming conventions");

            var configuration = new ChildOperatorConfiguration(name)
            {
                ParentLabelSelector = LabelSelector.Create<WebDriverSession>(
                    s => s.Metadata.Labels[Annotations.ManagedBy] == name
                    && s.Metadata.Labels[Annotations.AutomationName] == Annotations.AutomationNames.Fake),
            };

            var podCreated = new TaskCompletionSource<V1Pod>();
            var podDeleted = new TaskCompletionSource<V1Pod>();

            var kubernetes = this.host.Services.GetRequiredService<KubernetesClient>();
            var podClient = kubernetes.GetClient<V1Pod>();
            var podWatcher =
                podClient.WatchAsync(
                    "default",
                    fieldSelector: null,
                    labelSelector: $"{Annotations.ManagedBy}={name}",
                    null,
                    (eventType, pod) =>
                    {
                        switch (eventType)
                        {
                            case k8s.WatchEventType.Added:
                                podCreated.TrySetResult(pod);
                                break;

                            case k8s.WatchEventType.Deleted:
                                podDeleted.TrySetResult(pod);
                                return Task.FromResult(WatchResult.Stop);
                        }

                        return Task.FromResult(WatchResult.Continue);
                    },
                    default);

            var sessionClient = kubernetes.GetClient<WebDriverSession>();

            // Delete all objects which may have been created by this test.
            await Task.WhenAll(
                sessionClient.TryDeleteAsync("default", $"{name}-empty", TimeSpan.FromMinutes(1), default),
                sessionClient.TryDeleteAsync("default", $"{name}-fake", TimeSpan.FromMinutes(1), default),
                podClient.TryDeleteAsync("default", $"{name}-empty", TimeSpan.FromMinutes(1), default),
                podClient.TryDeleteAsync("default", $"{name}-fake", TimeSpan.FromMinutes(1), default)).ConfigureAwait(false);

            using (var @operator = new ChildOperator<WebDriverSession, V1Pod>(
                kubernetes,
                configuration,
                (mobileDevice, pod) =>
                {
                    pod.Spec = new V1PodSpec()
                    {
                        Containers = new V1Container[]
                        {
                            new V1Container()
                            {
                                Name = "fake-driver",
                                Image = "quay.io/kaponata/fake-driver:2.0.1",
                            },
                        },
                    };
                },
                this.host.Services.GetRequiredService<ILogger<ChildOperator<WebDriverSession, V1Pod>>>()))
            {
                // Start the operator
                await @operator.StartAsync(default).ConfigureAwait(false);

                // Create two new sessions, one which uses the fake automation and another which uses dummy automation.
                // A pod should be created for the fake session, but not the dummy session
                var emptySession = await sessionClient.CreateAsync(
                    new WebDriverSession()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Name = $"{name}-empty",
                            NamespaceProperty = "default",
                            Labels = new Dictionary<string, string>()
                            {
                                { Annotations.ManagedBy, name },
                                { Annotations.AutomationName, "dummy" },
                            },
                        },
                    },
                    default).ConfigureAwait(false);

                var fakeSession = await sessionClient.CreateAsync(
                    new WebDriverSession()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Name = $"{name}-fake",
                            NamespaceProperty = "default",
                            Labels = new Dictionary<string, string>()
                            {
                                { Annotations.ManagedBy, name },
                                { Annotations.AutomationName, Annotations.AutomationNames.Fake },
                            },
                        },
                    },
                    default).ConfigureAwait(false);

                var createdPod = await podCreated.Task.ConfigureAwait(false);
                Assert.Equal($"{name}-fake", createdPod.Metadata.Name);

                // Deleting the sessions should result in the associated pod being deleted, too.
                await sessionClient.DeleteAsync(emptySession, new V1DeleteOptions(propagationPolicy: "Foreground"), TimeSpan.FromMinutes(1), default).ConfigureAwait(false);
                await sessionClient.DeleteAsync(fakeSession, new V1DeleteOptions(propagationPolicy: "Foreground"), TimeSpan.FromMinutes(1), default).ConfigureAwait(false);
                var deletedPod = await podDeleted.Task.ConfigureAwait(false);
                Assert.Equal($"{name}-fake", deletedPod.Metadata.Name);

                // Stop the operator.
                await @operator.StopAsync(default).ConfigureAwait(false);
            }
        }

        private static string FormatNameCamelCase(string value)
        {
            return value.Replace("_", "-");
        }

        private static string FormatName(string value)
        {
            return value.ToLower().Replace("_", "-");
        }
    }
}