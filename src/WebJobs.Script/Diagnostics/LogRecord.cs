// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class LogRecord
    {
        public DateTime TimeStamp { get; set; }

        public string ErrorCode { get; set; }

        public string Source { get; set; }

        public string Category { get; set; }

        public string Summary { get; set; }

        public string Details { get; set; }

        public string HelpLink { get; set; }

        public string RoleInstanceName { get; set; }
    }
}
