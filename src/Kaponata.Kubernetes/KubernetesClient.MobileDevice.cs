﻿// <copyright file="KubernetesClient.MobileDevice.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Kaponata.Kubernetes.Models;
using Kaponata.Kubernetes.Polyfill;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaponata.Kubernetes
{
    /// <summary>
    /// Implements the <see cref="MobileDevice"/> operations.
    /// </summary>
    public partial class KubernetesClient
    {
        /// <summary>
        /// Asynchronously creates a new mobile device.
        /// </summary>
        /// <param name="value">
        /// The mobile device to create.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous operation, and returns the newly created mobile device
        /// when completed.
        /// </returns>
        public virtual Task<MobileDevice> CreateMobileDeviceAsync(MobileDevice value, CancellationToken cancellationToken)
        {
            return this.mobileDeviceClient.CreateAsync(value, cancellationToken);
        }

        /// <summary>
        /// Asynchronously list or watch <see cref="MobileDevice"/> objects.
        /// </summary>
        /// <param name="namespace">
        /// The namespace in which to list or watch objects.
        /// </param>
        /// <param name="continue">
        /// The continue option should be set when retrieving more results from the server.
        /// Since this value is server defined, clients may only use the continue value from
        /// a previous query result with identical query parameters (except for the value
        /// of continue) and the server may reject a continue value it does not recognize.
        /// If the specified continue value is no longer valid whether due to expiration
        /// (generally five to fifteen minutes) or a configuration change on the server,
        /// the server will respond with a 410 ResourceExpired error together with a continue
        /// token. If the client needs a consistent list, it must restart their list without
        /// the continue field. Otherwise, the client may send another list request with
        /// the token received with the 410 error, the server will respond with a list starting
        /// from the next key, but from the latest snapshot, which is inconsistent from the
        /// previous list results - objects that are created, modified, or deleted after
        /// the first list request will be included in the response, as long as their keys
        /// are after the "next key". This field is not supported when watch is true. Clients
        /// may start a watch from the last resourceVersion value returned by the server
        /// and not miss any modifications.
        /// </param>
        /// <param name="fieldSelector">
        /// A selector to restrict the list of returned objects by their fields. Defaults
        /// to everything.
        /// </param>
        /// <param name="labelSelector">
        /// A selector to restrict the list of returned objects by their labels. Defaults
        /// to everything.
        /// </param>
        /// <param name="limit">
        /// limit is a maximum number of responses to return for a list call. If more items
        /// exist, the server will set the `continue` field on the list metadata to a value
        /// that can be used with the same initial query to retrieve the next set of results.
        /// Setting a limit may return fewer than the requested amount of items (up to zero
        /// items) in the event all requested objects are filtered out and clients should
        /// only use the presence of the continue field to determine whether more results
        /// are available. Servers may choose not to support the limit argument and will
        /// return all of the available results. If limit is specified and the continue field
        /// is empty, clients may assume that no more results are available. This field is
        /// not supported if watch is true. The server guarantees that the objects returned
        /// when using continue will be identical to issuing a single list call without a
        /// limit - that is, no objects created, modified, or deleted after the first request
        /// is issued will be included in any subsequent continued requests. This is sometimes
        /// referred to as a consistent snapshot, and ensures that a client that is using
        /// limit to receive smaller chunks of a very large result can ensure they see all
        /// possible objects. If objects are updated during a chunked list the version of
        /// the object that was present at the time the first list result was calculated
        /// is returned.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="MobileDeviceList"/> which represents the mobile devices which match the query.
        /// </returns>
        public virtual Task<ItemList<MobileDevice>> ListMobileDeviceAsync(string @namespace, string @continue = null, string fieldSelector = null, string labelSelector = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            return this.mobileDeviceClient.ListAsync(
                @namespace: @namespace,
                @continue: @continue,
                fieldSelector: fieldSelector,
                labelSelector: labelSelector,
                limit: limit,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Asynchronously tries to read a mobile device.
        /// </summary>
        /// <param name="namespace">
        /// The namespace in which the mobile device is located.
        /// </param>
        /// <param name="name">
        /// The name which uniquely identifies the mobile device within the namespace.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous operation, and returns the requested mobile device, or
        /// <see langword="null"/> if the mobile device does not exist.
        /// </returns>
        public virtual Task<MobileDevice> TryReadMobileDeviceAsync(string @namespace, string name, CancellationToken cancellationToken)
        {
            return this.mobileDeviceClient.TryReadAsync(@namespace, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously deletes a mobile device.
        /// </summary>
        /// <param name="value">
        /// The mobile device to delete.
        /// </param>
        /// <param name="timeout">
        /// The amount of time in which the mobile device should be deleted.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual Task DeleteMobileDeviceAsync(MobileDevice value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.mobileDeviceClient.DeleteAsync(value, timeout, cancellationToken);
        }

        /// <summary>
        /// Asynchronously watches <see cref="MobileDevice"/> objects.
        /// </summary>
        /// <param name="value">
        /// The object to watch.
        /// </param>
        /// <param name="eventHandler">
        /// An handler which processes a watch event, and lets the watcher know whether
        /// to continue watching or not.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous
        /// operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the watch operation. The task completes
        /// when the watcher stops watching for events. The <see cref="WatchExitReason"/>
        /// return value describes why the watcher stopped. The task errors if the watch
        /// loop errors.
        /// </returns>
        public Task<WatchExitReason> WatchMobileDeviceAsync(
            MobileDevice value,
            WatchEventDelegate<MobileDevice> eventHandler,
            CancellationToken cancellationToken)
        {
            return this.mobileDeviceClient.WatchAsync(value, eventHandler, cancellationToken);
        }

        /// <summary>
        /// Asynchronously watches <see cref="MobileDevice"/> objects.
        /// </summary>
        /// <param name="namespace">
        /// The namespace in which to watch for <see cref="MobileDevice"/> objects.
        /// </param>
        /// <param name="fieldSelector">
        /// A selector to restrict the list of returned objects by their fields. Defaults
        /// to everything.
        /// </param>
        /// <param name="labelSelector">
        /// A selector to restrict the list of returned objects by their labels. Defaults
        /// to everything.
        /// </param>
        /// <param name="resourceVersion">
        /// resourceVersion sets a constraint on what resource versions a request may be
        /// served from. See <see href="https://kubernetes.io/docs/reference/using-api/api-concepts/#resource-versions"/>
        /// for details. Defaults to unset.
        /// </param>
        /// <param name="eventHandler">
        /// An handler which processes a watch event, and lets the watcher know whether
        /// to continue watching or not.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous
        /// operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the watch operation. The task completes
        /// when the watcher stops watching for events. The <see cref="WatchExitReason"/>
        /// return value describes why the watcher stopped. The task errors if the watch
        /// loop errors.
        /// </returns>
        public virtual Task<WatchExitReason> WatchMobileDeviceAsync(
            string @namespace,
            string fieldSelector,
            string labelSelector,
            string resourceVersion,
            WatchEventDelegate<MobileDevice> eventHandler,
            CancellationToken cancellationToken)
        {
            return this.mobileDeviceClient.WatchAsync(@namespace, fieldSelector, labelSelector, resourceVersion, eventHandler, cancellationToken);
        }
    }
}
