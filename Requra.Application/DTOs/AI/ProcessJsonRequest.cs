using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    //public class ProcessJsonRequest
    //{
    //    public Guid Job_Id { get; set; }

    //    public string Source_Type { get; set; }
    //    // e.g. "multi_document" or "meeting"

    //    public string Content { get; set; }

    //    public List<SourceDocumentDto> Source_Documents { get; set; } = new();

    //    public MetadataDto Metadata { get; set; }
    //}
    public class ProcessJsonRequest
    {

        //[Required]
        [JsonPropertyName("job_id")]
        public string JobId { get; set; }

        //[Required]
        [JsonPropertyName("content")]
        public string Content { get; set; }

        //[Required]
        [JsonPropertyName("source_type")]
        public string SourceType { get; set; }

        [JsonPropertyName("source_documents")]
        public List<SourceDocumentDto>? SourceDocuments { get; set; }

        [JsonPropertyName("metadata")]
        public MetadataDto? Metadata { get; set; }
    }
}
