﻿// <copyright file="ScrCpyClient.cs" company="Quamotion bv">
// Copyright (c) Quamotion bv. All rights reserved.
// </copyright>

using Kaponata.Android.Adb;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Kaponata.Android.ScrCpy
{
    /// <summary>
    /// A client used to manage scrcpy.
    /// </summary>
    public class ScrCpyClient : IAsyncDisposable
    {
        /// <summary>
        /// The folder used to upload the scrcpy server.
        /// This folder needs to be alligned with <seealso url="https://github.com/Genymobile/scrcpy/blob/master/server/src/main/java/com/genymobile/scrcpy/Server.java"/>
        /// as this path will be used to clean-up the server (unlinkSelf()).
        /// </summary>
        private const string ScrCpyRunDirectory = "/data/local/tmp";

        /// <summary>
        /// The folder used to store the different scrcpy versions.
        /// </summary>
        private const string ScrCpyReleasesDirectory = "/data/local/tmp/quamotion/scrcpy";

        /// <summary>
        /// The scrcpy file name.
        /// </summary>
        private const string ScrCpyFileName = "scrcpy-server.jar";

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly ILogger<ScrCpyClient> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly DeviceData device;
        private readonly AdbClient adbClient;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrCpyClient"/> class.
        /// </summary>
        /// <param name="logger">
        /// The logger to use when logging.
        /// </param>
        /// <param name="loggerFactory">
        /// A <see cref="ILoggerFactory"/> which can be used to initialise new loggers.
        /// </param>
        /// <param name="adbClient">
        /// The adb client to be used..
        /// </param>
        /// <param name="device">
        /// The device on which to launch scrcpy.
        /// </param>
        public ScrCpyClient(ILogger<ScrCpyClient> logger, ILoggerFactory loggerFactory, AdbClient adbClient, DeviceData device)
        {
            this.adbClient = adbClient;
            this.device = device;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets the scrcpy server binary stream.
        /// </summary>
        public Stream ScrCpyServer
        {
            get
            {
                return this.GetType().Assembly
                .GetManifestResourceStream("Kaponata.Android.ScrCpy.scrcpy-server-v1.17");
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ShellStream"/> of the scrcpy process.
        /// </summary>
        private ShellStream ScrCpyServerShellStream { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Task"/> representing the logging of the scrcpy shell output.
        /// </summary>
        private Task LogScrCpyShellOutputTask { get; set; }

        /// <summary>
        /// Launches the scrcpy server on the device using the default options.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous task.
        /// </param>
        /// <returns>
        /// The scrcpy stream.
        /// </returns>
        public virtual Task<Socket> LaunchScrCpyAsync(CancellationToken cancellationToken)
        {
            return this.LaunchScrCpyAsync(ScrCpyOptions.DefaultOptions, cancellationToken);
        }

        /// <summary>
        /// Launches the scrcpy server on the device.
        /// </summary>
        /// <param name="options">
        /// The scrcpy options.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous task.
        /// </param>
        /// <returns>
        /// The scrcpy stream.
        /// </returns>
        public virtual async Task<Socket> LaunchScrCpyAsync(ScrCpyOptions options, CancellationToken cancellationToken)
        {
            var command = options.GetCommand($"{ScrCpyRunDirectory}/{ScrCpyFileName}");
            this.logger.LogInformation($"Launching scrcpy on device '{this.device.Serial}' using command '{command}'.");

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // I _think_ it should be sufficient to just listen on the loopback adapter instead of all network interfaces, right?
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.Listen();

            var endPoint = socket.LocalEndPoint as IPEndPoint;
            var port = endPoint?.Port;
            await this.adbClient.CreateReverseForwardAsync(this.device, true, "localabstract:scrcpy", $"{port}", cancellationToken).ConfigureAwait(false);

            this.ScrCpyServerShellStream = await this.adbClient.ExecuteRemoteShellCommandAsync(this.device, command, this.tokenSource.Token).ConfigureAwait(false);
            this.LogScrCpyShellOutputTask = Task.Run(
                async () =>
                {
                    using var reader = new StreamReader(this.ScrCpyServerShellStream);
                    while (true)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        this.logger.LogInformation(line);
                    }
                },
                this.tokenSource.Token);

            this.logger.LogInformation($"Launched scrcpy on device '{this.device.Serial}'.");
            return socket;
        }

        /// <summary>
        /// Connect to the scrcpy server.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous task.
        /// </param>
        /// <returns>
        /// The scrcpy video stream.
        /// </returns>
        public virtual Task<ScrCpyVideoStream> ConnectToScrCpyAsync(CancellationToken cancellationToken)
        {
            return this.ConnectToScrCpyAsync(ScrCpyOptions.DefaultOptions, cancellationToken);
        }

        /// <summary>
        /// Connect to the scrcpy server.
        /// </summary>
        /// <param name="options">
        /// The scrcpy options.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous task.
        /// </param>
        /// <returns>
        /// The scrcpy video stream.
        /// </returns>
        public virtual async Task<ScrCpyVideoStream> ConnectToScrCpyAsync(ScrCpyOptions options, CancellationToken cancellationToken)
        {
            await this.InstallScrCpyAsync(options, cancellationToken).ConfigureAwait(false);
            using var socket = await this.LaunchScrCpyAsync(options, cancellationToken).ConfigureAwait(false);
            var videoHandler = await socket.AcceptAsync().ConfigureAwait(false);

            using var controlHandler = await socket.AcceptAsync().ConfigureAwait(false);

            this.logger.LogInformation($"Successfully connected to scrcpy on device '{this.device.Serial}'.");

            return new ScrCpyVideoStream(new NetworkStream(videoHandler, true), this.loggerFactory.CreateLogger<ScrCpyVideoStream>());
        }

        /// <summary>
        /// Installs the scrcpy server on the device using the default options.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous operation.
        /// </returns>
        public virtual Task InstallScrCpyAsync(CancellationToken cancellationToken)
        {
            return this.InstallScrCpyAsync(ScrCpyOptions.DefaultOptions, cancellationToken);
        }

        /// <summary>
        /// Installs the scrcpy server on the device.
        /// </summary>
        /// <param name="options">
        /// The scrcpy options.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which represents the asynchronous operation.
        /// </returns>
        public virtual async Task InstallScrCpyAsync(ScrCpyOptions options, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Installing scrcpy on device '{this.device.Serial}'.");

            var scrCpyFilePath = $"{ScrCpyReleasesDirectory}/{options.Version}/{ScrCpyFileName}";
            if (!await this.adbClient.ExistsAsync(this.device, scrCpyFilePath, cancellationToken).ConfigureAwait(false))
            {
                // Create a directory for our resources
                await this.adbClient.CreateDirectoryAsync(this.device, $"{ScrCpyReleasesDirectory}/{options.Version}", cancellationToken).ConfigureAwait(false);
                await this.adbClient.ChModAsync(this.device, $"{ScrCpyReleasesDirectory}/{options.Version}", "0777", cancellationToken).ConfigureAwait(false);
                using var scrCpyServerStream = this.ScrCpyServer;
                await this.adbClient.PushAsync(this.device, scrCpyServerStream, scrCpyFilePath, 511 /* 777 in octal */, DateTime.Now, cancellationToken).ConfigureAwait(false);
            }

            // Create a directory for our resources
            await this.adbClient.ExecuteRemoteShellCommandAsync(this.device, $"cp {scrCpyFilePath} {ScrCpyRunDirectory}/{ScrCpyFileName}", CancellationToken.None).ConfigureAwait(false);

            this.logger.LogInformation($"Successfully installed scrcpy on device '{this.device.Serial}'.");
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                this.logger.LogInformation($"Stopping scrcpy on device '{this.device.Serial}'.");

                try
                {
                    this.disposed = true;
                    this.tokenSource?.Cancel();
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Failed to stop scrcpy on device '{this.device.Serial}': {ex.Message}. The error occurred at {ex}");
                }
                finally
                {
                    this.logger.LogInformation($"Stopped scrcpy on device '{this.device.Serial}'.");
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}