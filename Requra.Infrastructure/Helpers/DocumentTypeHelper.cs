using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Helpers
{
    public static class DocumentTypeHelper
    {
        public static string GetCategory(DocumentType type)
        {
            return type switch
            {
                DocumentType.pdf or DocumentType.docx or DocumentType.txt => "document",
                DocumentType.audio => "audio",
                _ => "unknown"
            };
        }
    }
}
