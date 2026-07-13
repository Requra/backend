using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    public enum RecordingChunkStatus
    {
        Pending ,
        Uploaded,
        Failed ,
        Duplicate,
        Ignored
    }
}
