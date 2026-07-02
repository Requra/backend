using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ArtifactsDto
    {
        [JsonPropertyName("excel_file")]
        public ExcelFileDto ExcelFile { get; set; }
    }
}
