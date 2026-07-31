using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    public enum AnalysisRunStatus
    {
        QUEUED,
        PROCESSING,
        COMPLETED,
        FAILED,
        PARTIAL,
        REJECTED,
        CANCELLED
    }
}
