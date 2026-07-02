using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class SourceDocumentDto
    {
        //public Guid Backend_Document_Id { get; set; }

        //public string Title { get; set; }

        //public DocumentType Type { get; set; } 

        //public int Language { get; set; }
        //[Required]
        //[JsonPropertyName("backend_document_id")]
        //public string BackendDocumentId { get; set; }

        ////[Required]
        //[JsonPropertyName("title")]
        //public string Title { get; set; }

        //[JsonPropertyName("type")]
        //public int? Type { get; set; }

        //[JsonPropertyName("language")]
        //public int? Language { get; set; }

        //[JsonPropertyName("mime_type")]
        //public string? MimeType { get; set; }

        [JsonPropertyName("source_id")]
        public string SourceId { get; set; }

        [JsonPropertyName("source_type")]
        public string SourceType { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }
    }
}
