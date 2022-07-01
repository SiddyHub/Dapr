using System;

namespace GloboTicket.Services.ShoppingBasket.Models
{
    public class PriceUpdate
    {
        public Guid EventId { get; set; }
        public int Price { get; set; }
    }
}
