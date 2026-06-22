using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class MetadataDto
    {
        public Guid Project_Id { get; set; }

        public Guid? Meeting_Id { get; set; }

        public Guid Analysis_Run_Id { get; set; }
    }
}
