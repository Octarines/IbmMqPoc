using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octarines.IbmMqPoc.Models.Configuration
{
    public class MqSettings
    {
        public string QueueManager { get; set; }
        public string ClientChannelId { get; set; }
        public string HostName { get; set; }
        public string PortNumber { get; set; }
        public string HostIPAddress { get; set; }
        public string QueueName { get; set; }
    }
}
