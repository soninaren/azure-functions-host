// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public interface ILogRecordRepository
    {
        bool AddLogRecord(string category, LogRecord record);

        List<LogRecord> GetLatestLogRecords(string category, int offset, int pageSize);

        bool DeleteLogRecord(int days);

        bool Purge();
    }
}
