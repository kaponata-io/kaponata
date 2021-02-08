﻿// <copyright file="FakeOperators.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using k8s.Models;
using Kaponata.Kubernetes;
using Kaponata.Kubernetes.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kaponata.Operator.Operators
{
    /// <summary>
    /// Contains operators for provisioning <see cref="WebDriverSession"/> instances which use the Fake driver.
    /// </summary>
    public static class FakeOperators
    {
        private const string ImageName = "quay.io/kaponata/fake-driver:2.0.1";
        private const int Port = 4774;

        /// <summary>
        /// Builds an operator which provides a Fake driver pod for each <see cref="WebDriverSession"/> which uses
        /// the Fake driver.
        /// </summary>
        /// <param name="host">
        /// A service host from which to host services.
        /// </param>
        /// <returns>
        /// A configured operator.
        /// </returns>
        public static ChildOperatorBuilder<WebDriverSession, V1Pod> BuildPodOperator(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var kubernetes = host.Services.GetRequiredService<KubernetesClient>();
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("FakeOperator");

            return new ChildOperatorBuilder(host)
                .CreateOperator("WebDriverSession-FakeDriver-PodOperator")
                .Watches<WebDriverSession>()
                .WithLabels(s => s.Metadata.Labels[Annotations.AutomationName] == Annotations.AutomationNames.Fake)
                .Creates<V1Pod>(
                    (session, pod) =>
                    {
                        pod.EnsureMetadata().EnsureLabels();
                        pod.Metadata.Labels.Add(Annotations.SessionName, session.Metadata.Name);

                        pod.Spec = new V1PodSpec()
                        {
                            Containers = new V1Container[]
                            {
                                new V1Container()
                                {
                                    Image = ImageName,
                                    Name = "appium-fake-driver",
                                    Ports = new V1ContainerPort[]
                                    {
                                        new V1ContainerPort()
                                        {
                                             ContainerPort = Port,
                                             Name = "http",
                                        },
                                    },
                                    ReadinessProbe = new V1Probe()
                                    {
                                        HttpGet = new V1HTTPGetAction()
                                        {
                                            Path = "/wd/hub/status",
                                            Port = $"{Port}",
                                        },
                                    },
                                },
                            },
                        };
                    })
                .PostsFeedback(
                    async (context, cancellationToken) =>
                    {
                        var session = context.Parent;
                        var pod = context.Child;

                        JsonPatchDocument<WebDriverSession> patch;

                        if (session?.Spec?.Capabilities == null)
                        {
                            // This is an invalid session; we need at least desired capabilities.
                            logger.LogWarning("Session {session} is missing desired capabilities.", session?.Metadata?.Name);
                            patch = null;
                        }
                        else if (session.Status?.SessionId != null)
                        {
                            // Do nothing if the session already exists.
                            logger.LogDebug("Session {session} already has a session ID.", session?.Metadata?.Name);
                            patch = null;
                        }
                        else if (pod?.Status?.Phase != "Running" || !pod.Status.ContainerStatuses.All(c => c.Ready))
                        {
                            // Do nothing if the pod is not yet ready
                            logger.LogInformation("Not creating a session for session {session} because pod {pod} is not ready yet.", session?.Metadata?.Name, pod?.Metadata?.Name);
                            patch = null;
                        }
                        else
                        {
                            var requestedCapabilities = JsonConvert.DeserializeObject(context.Parent.Spec.Capabilities);
                            var request = JsonConvert.SerializeObject(
                                new
                                {
                                    capabilities = requestedCapabilities,
                                });

                            var content = new StringContent(request, Encoding.UTF8, "application/json");

                            using (var httpClient = kubernetes.CreatePodHttpClient(context.Child, 4774))
                            using (var remoteResult = await httpClient.PostAsync("wd/hub/session/", content, cancellationToken).ConfigureAwait(false))
                            {
                                var sessionJson = await remoteResult.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                var sessionObject = JObject.Parse(sessionJson);
                                var sessionValue = (JObject)sessionObject.GetValue("value");

                                patch = new JsonPatchDocument<WebDriverSession>();

                                if (context.Parent.Status == null)
                                {
                                    patch.Add(s => s.Status, new WebDriverSessionStatus());
                                }

                                // Check whether we should store this as a Kubernetes object.
                                if (sessionValue.TryGetValue("sessionId", out var sessionId))
                                {
                                    patch.Add(s => s.Status.SessionId, sessionId.Value<string>());
                                    patch.Add(s => s.Status.SessionReady, true);
                                }

                                if (sessionValue.TryGetValue("capabilities", out var capabilities))
                                {
                                    patch.Add(s => s.Status.Capabilities, capabilities.ToString(Formatting.None));
                                }

                                if (sessionValue.TryGetValue("error", out var error))
                                {
                                    patch.Add(s => s.Status.Error, error.Value<string>());
                                }

                                if (sessionValue.TryGetValue("message", out var message))
                                {
                                    patch.Add(s => s.Status.Message, message.Value<string>());
                                }

                                if (sessionValue.TryGetValue("stacktrace", out var stackTrace))
                                {
                                    patch.Add(s => s.Status.StackTrace, stackTrace.Value<string>());
                                }

                                if (sessionValue.TryGetValue("data", out var data))
                                {
                                    patch.Add(s => s.Status.Data, data.ToString(Formatting.None));
                                }

                                return patch;
                            }
                        }

                        return null;
                    });
        }

        /// <summary>
        /// Builds an operator which provisions ingress rules for <see cref="WebDriverSession"/> objects which
        /// use the Fake driver.
        /// </summary>
        /// <param name="host">
        /// A service host from which to host services.
        /// </param>
        /// <returns>
        /// A configured operator.
        /// </returns>
        public static ChildOperatorBuilder<WebDriverSession, V1Ingress> BuildIngressOperator(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var kubernetes = host.Services.GetRequiredService<KubernetesClient>();
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("FakeOperator");

            return new ChildOperatorBuilder(host)
                .CreateOperator("WebDriverSession-IngressOperator")
                .Watches<WebDriverSession>()
                .Where(s => s.Status?.SessionId != null)
                .Creates<V1Ingress>(
                (session, ingress) =>
                {
                    // No need to validate Status.SessionId != null, that's handled by the Where clause above.
                    ingress.Spec = new V1IngressSpec()
                    {
                        Rules = new V1IngressRule[]
                        {
                            new V1IngressRule()
                            {
                                Http = new V1HTTPIngressRuleValue()
                                {
                                    Paths = new V1HTTPIngressPath[]
                                    {
                                        new V1HTTPIngressPath()
                                        {
                                            Path = $"/wd/hub/session/{session.Status.SessionId}/",
                                            PathType = "Prefix",
                                            Backend = new V1IngressBackend()
                                            {
                                                Service = new V1IngressServiceBackend()
                                                {
                                                    Name = session.Metadata.Name,
                                                    Port = new V1ServiceBackendPort()
                                                    {
                                                        Number = 4774,
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    };
                })
                .PostsFeedback((context, cancellationToken) =>
                {
                    JsonPatchDocument<WebDriverSession> patch = null;

                    var session = context.Parent;
                    var ingress = context.Child;

                    if (ingress != null && !session.Status.IngressReady)
                    {
                        patch = new JsonPatchDocument<WebDriverSession>();
                        patch.Add(s => s.Status.IngressReady, true);
                    }

                    return Task.FromResult(patch);
                });
        }

        /// <summary>
        /// Builds an operator which provisions services for <see cref="WebDriverSession"/> objects which
        /// use the Fake driver.
        /// </summary>
        /// <param name="host">
        /// A service host from which to host services.
        /// </param>
        /// <returns>
        /// A configured operator.
        /// </returns>
        public static ChildOperatorBuilder<WebDriverSession, V1Service> BuildServiceOperator(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var kubernetes = host.Services.GetRequiredService<KubernetesClient>();
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("WebDriverSession-ServiceOperator");

            return new ChildOperatorBuilder(host)
                .CreateOperator("WebDriverSession-ServiceOperator")
                .Watches<WebDriverSession>()
                .Where(s => s.Status?.SessionId != null)
                .Creates<V1Service>(
                (session, service) =>
                {
                    service.Spec = new V1ServiceSpec()
                    {
                        Selector = new Dictionary<string, string>()
                        {
                            { Annotations.SessionName, session.Metadata.Name },
                        },
                        Ports = new V1ServicePort[]
                        {
                            new V1ServicePort()
                            {
                                 Protocol = "TCP",
                                 Port = 4774,
                                 TargetPort = 4774,
                            },
                        },
                    };
                })
                .PostsFeedback((context, cancellationToken) =>
                {
                    JsonPatchDocument<WebDriverSession> patch = null;

                    var session = context.Parent;
                    var service = context.Child;

                    if (service != null && !session.Status.ServiceReady)
                    {
                        patch = new JsonPatchDocument<WebDriverSession>();
                        patch.Add(s => s.Status.ServiceReady, true);
                    }

                    return Task.FromResult(patch);
                });
        }
    }
}