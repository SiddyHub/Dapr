using GloboTicket.Services.ShoppingBasket.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GloboTicket.Services.ShoppingBasket.Repositories
{
    public class BasketLinesIntegrationRepository: IBasketLinesIntegrationRepository
    {
        private readonly DbContextOptions<ShoppingBasketDbContext> dbContextOptions;


        public BasketLinesIntegrationRepository(DbContextOptions<ShoppingBasketDbContext> dbContextOptions)
        {
            this.dbContextOptions = dbContextOptions;
        }

        public async Task UpdatePricesForIntegrationEvent(Models.PriceUpdate priceUpdate)
        {
            await using (var shoppingBasketDbContext = new ShoppingBasketDbContext(dbContextOptions))
            {
                var basketLinesToUpdate = shoppingBasketDbContext.BasketLines.Where(x => x.EventId == priceUpdate.EventId);

                await basketLinesToUpdate.ForEachAsync((basketLineToUpdate) =>
                    basketLineToUpdate.Price = priceUpdate.Price
                );
                await shoppingBasketDbContext.SaveChangesAsync();
            }
        }
    }
}
