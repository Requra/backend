using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExcelExportDto
    {
        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("columns")]
        public List<string> Columns { get; set; }

        [JsonPropertyName("rows")]
        public List<ExcelRowDto> Rows { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }

    }
}

