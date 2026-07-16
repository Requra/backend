using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Recordings
{
    public class StopRecordingRequest
    {
        public Guid RecordingId { get; set; }
        public int DurationSeconds { get; set; }
        public int? lastChunkIndex { get; set; }
    }
    public class StopRecordingApiRequest
    {
        public int DurationSeconds { get; set; }
        public int? lastChunkIndex { get; set; }
    }
}
