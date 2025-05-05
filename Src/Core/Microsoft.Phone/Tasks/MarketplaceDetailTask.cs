using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Phone.Tasks
{
    public class MarketplaceDetailTask
    {
        public string ContentIdentifier { get; set; }

        private MarketplaceContentType _contentType;

        public MarketplaceContentType ContentType
        {
            get
            {
                if (_contentType == null)
                {
                    _contentType = new MarketplaceContentType()
                    {
                    };
                }
                return _contentType;
            }

            set
            {
                _contentType = value;
            }
        }

        public void Show()
        {

        }
       
    }
}
