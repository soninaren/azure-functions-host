// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Host.Loggers;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class LogRecordManager : ILogRecordManager
    {
        private readonly ILogRecordRepository _logRecordRepository;

        public LogRecordManager()
        {
            _logRecordRepository = new InMemoryLogRecordRepository();
        }

        public void Log(string category, LogRecord log)
        {
            _logRecordRepository.AddLogRecord(category, log);
        }

        public List<LogRecord> GetLogs(string category, int offset)
        {
            return _logRecordRepository.GetLatestLogRecords(category, offset, 10);
        }
    }
}
