// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics
{
    public class LinuxAppServiceFileLogger : IDisposable
    {
        private readonly string _logFileName;
        private readonly string _logFileDirectory;
        private readonly string _logFilePath;
        private readonly BlockingCollection<string> _buffer;
        private readonly List<string> _currentBatch;
        private readonly IFileSystem _fileSystem;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _outputTask;
        private bool _disposed;
        private DateTime _lastPurgeTimetamp;

        public LinuxAppServiceFileLogger(string logFileName, string logFileDirectory, IFileSystem fileSystem, bool startOnCreate = true)
        {
            _logFileName = logFileName;
            _logFileDirectory = logFileDirectory;
            _logFilePath = Path.Combine(_logFileDirectory, _logFileName + ".log");
            _buffer = new BlockingCollection<string>(new ConcurrentQueue<string>());
            _currentBatch = new List<string>();
            _fileSystem = fileSystem;
            _cancellationTokenSource = new CancellationTokenSource();

            if (startOnCreate)
            {
                Start();
            }
        }

        // Maximum number of files
        public int MaxFileCount { get; set; } = 3;

        // Maximum size of individual log file in MB
        public int MaxFileSizeMb { get; set; } = 10;

        // Maximum time between successive flushes (seconds)
        public int FlushFrequencySeconds { get; set; } = 30;

        public int PurgeFrequencyMinutes { get; set; } = 5;

        public virtual void Log(string message)
        {
            try
            {
                _buffer.Add(message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Start()
        {
            if (_outputTask == null)
            {
                _outputTask = Task.Factory.StartNew(ProcessLogQueue, null, TaskCreationOptions.LongRunning);
            }
        }

        public virtual async Task ProcessLogQueue(object state)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await InternalProcessLogQueue();

                await Task.Delay(TimeSpan.FromSeconds(FlushFrequencySeconds), _cancellationTokenSource.Token);

                if ((DateTime.UtcNow - _lastPurgeTimetamp).TotalMinutes > PurgeFrequencyMinutes)
                {
                    PurgeOldFiles();
                    _lastPurgeTimetamp = DateTime.UtcNow;
                }
            }
        }

        // internal for unittests
        internal async Task InternalProcessLogQueue()
        {
            if (_disposed)
            {
                return;
            }

            string currentMessage;
            while (_buffer.TryTake(out currentMessage))
            {
                _currentBatch.Add(currentMessage);
            }

            if (_currentBatch.Any())
            {
                try
                {
                    await WriteLogs(_currentBatch);
                }
                catch (Exception)
                {
                }

                _currentBatch.Clear();
            }
        }

        private async Task WriteLogs(IEnumerable<string> currentBatch)
        {
            try
            {
                await WriteLogsCore(currentBatch);
            }
            catch (DirectoryNotFoundException)
            {
                // ensure directory and retry
                _fileSystem.Directory.CreateDirectory(_logFileDirectory);
                await WriteLogsCore(currentBatch);
            }
        }

        private async Task WriteLogsCore(IEnumerable<string> currentBatch)
        {
            RollLogFileIfNecessary();

            await AppendLogs(currentBatch);
        }

        private async Task AppendLogs(IEnumerable<string> logs)
        {
            using (var streamWriter = _fileSystem.File.AppendText(_logFilePath))
            {
                foreach (var log in logs)
                {
                    await streamWriter.WriteLineAsync(log);
                }
            }
        }

        private void RollLogFileIfNecessary()
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(_logFilePath);
            if (fileInfo.Exists && (fileInfo.Length / (1024 * 1024) >= MaxFileSizeMb))
            {
                // move the current file to an archive file
                // new current file will be created next time it's written to
                _fileSystem.File.Move(_logFilePath, GetArchiveFileName(DateTime.UtcNow));
            }
        }

        private void PurgeOldFiles()
        {
            // Delete files over the max number
            var files = _fileSystem.DirectoryInfo.FromDirectoryName(_logFileDirectory).GetFiles(_logFileName + "*", SearchOption.TopDirectoryOnly);
            var filesToDelete = files.OrderByDescending(f => f.Name).Skip(MaxFileCount).ToArray();
            foreach (var fileToDelete in filesToDelete)
            {
                fileToDelete.Delete();
            }
        }

        public string GetArchiveFileName(DateTime dateTime)
        {
            return Path.Combine(_logFileDirectory, $"{_logFileName}{dateTime:yyyyMMddHHmmss}.log");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Dispose();
                    _buffer.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
