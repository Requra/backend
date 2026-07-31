using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExcelExportDto
    {
        [JsonPropertyName("rows")]
        public List<ExcelRowDto> Rows { get; set; }

    }
}

