using GloboTicket.Integration.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GloboTicket.Services.EventCatalog.Messages
{
    public class PriceUpdatedMessage: IntegrationBaseMessage
    {
        public Guid EventId { get; set; }
        public int Price { get; set; }
    }
}
