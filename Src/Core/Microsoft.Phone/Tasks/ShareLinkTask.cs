using System;
using System.Diagnostics;

namespace Microsoft.Phone.Tasks
{
    public class ShareLinkTask
    {
        public ShareLinkTask()
        {
        }

        public string? Title { get; set; }
        public string? Message { get; set; }       
        public Uri? LinkUri { get; set; }
       
    }
}
