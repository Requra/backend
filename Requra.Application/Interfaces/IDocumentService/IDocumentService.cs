using Requra.Application.DTOs.Document;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IDocumentService
{
    public interface IDocumentService
    {
        public Task<Response<List<DocumentDto>>> GetDocumentsByProjectIdAsync(Guid projectId);
    }
}
