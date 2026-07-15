using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StopRecordingRequest
    {
        public Guid RecordingId { get; set; }
        public int? ExpectedChunks { get; set; }
    }
}
