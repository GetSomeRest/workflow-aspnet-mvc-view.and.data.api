using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DropnWatch.Models
{
    public class PushMessage
    {
        public PushMessage()
        { 
            
        }

        public PushMessage(string fileName, string fileUrn)
        {
            FileName = fileName;
            FileUrn = fileUrn;
        }

        public string FileName
        {
            get;
            set;
        }

        public string FileUrn
        {
            get;
            set;
        }

        public string Dt
        {
            get;
            set;
        }
    }
}