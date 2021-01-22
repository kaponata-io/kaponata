﻿// <copyright file="KubernetesProtocolTests.Watch.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Divergic.Logging.Xunit;
using k8s;
using k8s.Models;
using Kaponata.Operator.Kubernetes.Polyfill;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

namespace Kaponata.Operator.Tests.Kubernetes.Polyfill
{
    /// <summary>
    /// Tests the <see cref="KubernetesProtocol.WatchPodAsync"/> method.
    /// </summary>
    public partial class KubernetesProtocolTests
    {
        private readonly ITestOutputHelper output;
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="KubernetesProtocolTests"/> class.
        /// </summary>
        /// <param name="output">
        /// The test output helper which will be used to log to xunit.
        /// </param>
        public KubernetesProtocolTests(ITestOutputHelper output)
        {
            this.loggerFactory = LogFactory.Create(output);
            this.output = output;
        }

        /// <summary>
        /// The <see cref="KubernetesProtocol.WatchPodAsync"/> method validates the parameters passed to it.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous test.
        /// </returns>
        [Fact]
        public async Task WatchPodAsync_ValidatesArguments_Async()
        {
            var client = new KubernetesProtocol(new DummyHandler(), this.loggerFactory.CreateLogger<KubernetesProtocol>(), this.loggerFactory);
            await Assert.ThrowsAsync<ArgumentNullException>("pod", () => client.WatchPodAsync(null, (eventType, result) => Task.FromResult(WatchResult.Continue), default)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>("eventHandler", () => client.WatchPodAsync(new V1Pod(), null, default)).ConfigureAwait(false);
        }

        /// <summary>
        /// The <see cref="KubernetesProtocol.WatchPodAsync"/> method immediately exists if it receives empty content.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous test.
        /// </returns>
        [Fact]
        public async Task WatchPodAsync_EmptyContent_Completes_Async()
        {
            var handler = new DummyHandler();
            handler.Responses.Enqueue(
                new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new WatchHttpContent(
                        new StringContent(string.Empty)),
                });

            Collection<(WatchEventType, V1Pod)> events = new Collection<(WatchEventType, V1Pod)>();

            var client = new KubernetesProtocol(handler, this.loggerFactory.CreateLogger<KubernetesProtocol>(), this.loggerFactory);
            var result = await client.WatchPodAsync(
                new V1Pod()
                {
                    Metadata = new V1ObjectMeta(name: "pod", namespaceProperty: "default", resourceVersion: "1"),
                },
                (eventType, result) =>
                {
                    events.Add((eventType, result));
                    return Task.FromResult(WatchResult.Continue);
                },
                default).ConfigureAwait(false);

            Assert.Equal(WatchExitReason.ServerDisconnected, result);
            Assert.Empty(events);
            Assert.Collection(
                handler.Requests,
                r =>
                {
                    Assert.Equal(new Uri("http://localhost/api/v1/namespaces/default/pods?fieldSelector=metadata.name%3Dpod&resourceVersion=1&watch=true"), r.RequestUri);
                });
        }

        /// <summary>
        /// The <see cref="KubernetesProtocol.WatchPodAsync"/> method can be cancelled.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous test.
        /// </returns>
        [Fact]
        public async Task WatchPodAsync_CancelsIfNeeded_Async()
        {
            using (var stream = new SimplexStream())
            using (var writer = new StreamWriter(stream))
            {
                var handler = new DummyHandler();
                handler.Responses.Enqueue(
                    new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new WatchHttpContent(
                            new StreamContent(stream)),
                    });

                Collection<(WatchEventType, V1Pod)> events = new Collection<(WatchEventType, V1Pod)>();

                var client = new KubernetesProtocol(handler, this.loggerFactory.CreateLogger<KubernetesProtocol>(), this.loggerFactory);
                var tcs = new TaskCompletionSource();
                var cts = new CancellationTokenSource();

                var watchTask = client.WatchPodAsync(
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta(name: "pod", namespaceProperty: "default", resourceVersion: "1"),
                    },
                    (eventType, result) =>
                    {
                        events.Add((eventType, result));
                        tcs.SetResult();
                        return Task.FromResult(WatchResult.Continue);
                    },
                    cts.Token);

                // Send a single event and wait for the watch task to handle that. This is to make sure we enter the inner
                // loop, and the task does not cancel early.
                await writer.WriteLineAsync(
                    JsonConvert.SerializeObject(
                        new Watcher<V1Pod>.WatchEvent()
                        {
                            Type = WatchEventType.Modified,
                            Object = new V1Pod()
                            {
                                Kind = "V1Pod",
                            },
                        })).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                await tcs.Task.ConfigureAwait(false);
                Assert.True(!watchTask.IsCompleted);
                cts.Cancel();

                await Assert.ThrowsAsync<TaskCanceledException>(() => watchTask).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The <see cref="KubernetesProtocol.WatchPodAsync"/> method throws an exception when a Kubernetes
        /// error occurs.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous test.
        /// </returns>
        [Fact]
        public async Task WatchPodAsync_ThrowsExceptionIfNeeded_Async()
        {
            var handler = new DummyHandler();
            handler.Responses.Enqueue(
                new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new WatchHttpContent(
                        new StringContent(
                            JsonConvert.SerializeObject(
                                new V1WatchEvent()
                                {
                                    Type = nameof(WatchEventType.Error),
                                    ObjectProperty =
                                        new V1Status()
                                        {
                                            Kind = "Status",
                                            Message = "ErrorMessage",
                                        },
                                }))),
                });

            Collection<(WatchEventType, V1Pod)> events = new Collection<(WatchEventType, V1Pod)>();

            var client = new KubernetesProtocol(handler, this.loggerFactory.CreateLogger<KubernetesProtocol>(), this.loggerFactory);
            var cts = new CancellationTokenSource();

            var ex = await Assert.ThrowsAsync<KubernetesException>(
                () => client.WatchPodAsync(
                new V1Pod()
                {
                    Metadata = new V1ObjectMeta(name: "pod", namespaceProperty: "default", resourceVersion: "1"),
                },
                (eventType, result) =>
                {
                    events.Add((eventType, result));
                    return Task.FromResult(WatchResult.Continue);
                },
                cts.Token)).ConfigureAwait(false);
            Assert.Equal("ErrorMessage", ex.Message);
        }

        /// <summary>
        /// The <see cref="KubernetesProtocol.WatchPodAsync"/> method returns when the client
        /// returns <see cref="WatchResult.Stop"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous test.
        /// </returns>
        [Fact]
        public async Task WatchPodAsync_ClientCanStopLoop_Async()
        {
            using (var stream = new SimplexStream())
            using (var writer = new StreamWriter(stream))
            {
                var handler = new DummyHandler();
                handler.Responses.Enqueue(
                    new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new WatchHttpContent(
                            new StreamContent(stream)),
                    });

                Collection<(WatchEventType, V1Pod)> events = new Collection<(WatchEventType, V1Pod)>();

                var client = new KubernetesProtocol(handler, this.loggerFactory.CreateLogger<KubernetesProtocol>(), this.loggerFactory);
                var cts = new CancellationTokenSource();

                var watchTask = client.WatchPodAsync(
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta(name: "pod", namespaceProperty: "default", resourceVersion: "1"),
                    },
                    (eventType, result) =>
                    {
                        events.Add((eventType, result));
                        return Task.FromResult(events.Count == 1 ? WatchResult.Continue : WatchResult.Stop);
                    },
                    cts.Token);

                Assert.True(!watchTask.IsCompleted);

                await writer.WriteAsync(
                    JsonConvert.SerializeObject(
                        new V1WatchEvent()
                        {
                            Type = nameof(WatchEventType.Deleted),
                            ObjectProperty = new V1Pod()
                            {
                                Metadata = new V1ObjectMeta()
                                {
                                    NamespaceProperty = "some-namespace",
                                    Name = "some-name",
                                },
                            },
                        }));
                await writer.WriteAsync('\n').ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                Assert.True(!watchTask.IsCompleted);

                await writer.WriteAsync(
                    JsonConvert.SerializeObject(
                        new V1WatchEvent()
                        {
                            Type = nameof(WatchEventType.Deleted),
                            ObjectProperty = new V1Pod()
                            {
                                Metadata = new V1ObjectMeta()
                                {
                                    NamespaceProperty = "some-namespace2",
                                    Name = "some-name2",
                                },
                            },
                        }));
                await writer.WriteAsync('\n').ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                var result = await watchTask;
                Assert.Equal(WatchExitReason.ClientDisconnected, result);

                Assert.Collection(
                    events,
                    e =>
                    {
                        Assert.Equal(WatchEventType.Deleted, e.Item1);
                        Assert.Equal("some-namespace", e.Item2.Metadata.NamespaceProperty);
                        Assert.Equal("some-name", e.Item2.Metadata.Name);
                    },
                    e =>
                    {
                        Assert.Equal(WatchEventType.Deleted, e.Item1);
                        Assert.Equal("some-namespace2", e.Item2.Metadata.NamespaceProperty);
                        Assert.Equal("some-name2", e.Item2.Metadata.Name);
                    });
            }
        }
    }
}
