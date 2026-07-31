using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExportsDto
    {
       

        [JsonPropertyName("jira")]
        public JiraExportDto Jira { get; set; }

        [JsonPropertyName("excel")]
        public ExcelExportDto Excel { get; set; }
    }
}
