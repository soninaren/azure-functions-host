// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs.Host.Loggers;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class InMemoryLogRecordRepository : ILogRecordRepository
    {
        private Dictionary<string, List<LogRecord>> records;

        public InMemoryLogRecordRepository()
        {
            records = new Dictionary<string, List<LogRecord>>();
        }

        private string GetTodaysKey(string category) => $"{DateTime.UtcNow.ToShortDateString()}/{category}";

        public bool AddLogRecord(string category, LogRecord record)
        {
            string key = GetTodaysKey(category);

            List<LogRecord> recordList;
            if (!records.ContainsKey(key))
            {
                recordList = new List<LogRecord>();
                records[key] = recordList;
            }

            recordList = records[key];
            recordList.Add(record);
            return true;
        }

        public bool DeleteLogRecord(int days)
        {
            throw new NotImplementedException();
        }

        public List<LogRecord> GetLatestLogRecords(string category, int offset, int pageSize)
        {
            string key = GetTodaysKey(category);
            var currentRecords = records[key].ToList();
            currentRecords.Reverse();

            var list = new List<LogRecord>();
            if (currentRecords.Count - offset > 0)
            {
                int end = Math.Min(offset + pageSize, currentRecords.Count);
                for (int i = offset; i < end; i++)
                {
                    list.Add(currentRecords[i]);
                }
            }

            return list;
        }

        public bool Purge()
        {
            records.Clear();
            return true;
        }
    }
}
