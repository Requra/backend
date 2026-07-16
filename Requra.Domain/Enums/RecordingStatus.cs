using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Enums
{
    public enum RecordingStatus
    {

        READY,      //started
        ACTIVE,     //uploading
        STOPPED,    //completed
        FINALIZING, //ending
        FAILED,
        EXPIRED
    
    }
}
