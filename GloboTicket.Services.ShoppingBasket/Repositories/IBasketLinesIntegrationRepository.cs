using System.Threading.Tasks;

namespace GloboTicket.Services.ShoppingBasket.Repositories
{
    public interface IBasketLinesIntegrationRepository
    {
        Task UpdatePricesForIntegrationEvent(Models.PriceUpdate priceUpdate);
    }
}
