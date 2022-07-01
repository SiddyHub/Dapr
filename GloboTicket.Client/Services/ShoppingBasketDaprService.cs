using Dapr.Client;
using GloboTicket.Web.Models;
using GloboTicket.Web.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GloboTicket.Web.Services
{
    public class ShoppingBasketDaprService : IShoppingBasketService
    {
        private readonly DaprClient daprClient;
        private readonly Settings settings;

        public ShoppingBasketDaprService(DaprClient daprClient, Settings settings)
        {
            this.daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<BasketLine> AddToBasket(Guid basketId, BasketLineForCreation basketLine)
        {
            if (basketId == Guid.Empty)
            {
                var basketForCreation = new BasketForCreation { UserId = settings.UserId };
                var basketResponse = await daprClient.InvokeMethodAsync<BasketForCreation, Basket>("shoppingbasket", "/api/baskets", basketForCreation);                   
                basketId = basketResponse.BasketId;
            }

            var request = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, "shoppingbasket", $"/api/baskets/{basketId}/basketlines", basketLine);            

            var response = await daprClient.InvokeMethodAsync<BasketLine>(request);            
            return response;
        }

        public async Task<Basket> GetBasket(Guid basketId)
        {
            if (basketId == Guid.Empty)
                return null;
            var request = await daprClient.InvokeMethodAsync<Basket>(HttpMethod.Get, "shoppingbasket", $"/api/baskets/{basketId}");            
            return request;
        }

        public async Task<IEnumerable<BasketLine>> GetLinesForBasket(Guid basketId)
        {
            if (basketId == Guid.Empty)
                return new BasketLine[0];
            var response = await daprClient.InvokeMethodAsync<IEnumerable<BasketLine>>(HttpMethod.Get, "shoppingbasket", $"/api/baskets/{basketId}/basketLines");            
            return response;

        }

        public async Task UpdateLine(Guid basketId, BasketLineForUpdate basketLineForUpdate)
        {
            var request = daprClient.CreateInvokeMethodRequest(HttpMethod.Put, "shoppingbasket", $"/api/baskets/{basketId}/basketLines/{basketLineForUpdate.LineId}", basketLineForUpdate);
            await daprClient.InvokeMethodAsync<BasketLine>(request);            
        }

        public async Task RemoveLine(Guid basketId, Guid lineId)
        {
            await daprClient.InvokeMethodAsync(HttpMethod.Delete, "shoppingbasket", $"/api/baskets/{basketId}/basketLines/{lineId}");
            //await daprClient.InvokeMethodAsync<BasketLine>(request);            
        }

        public async Task<BasketForCheckout> Checkout(Guid basketId, BasketForCheckout basketForCheckout)
        {
            var request = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, "shoppingbasket", "/api/baskets/checkout", basketForCheckout);
            var response = await daprClient.InvokeMethodAsync<BasketForCheckout>(request);
            
            return response;
        }

        public async Task ApplyCouponToBasket(Guid basketId, CouponForUpdate couponForUpdate)
        {
            await daprClient.InvokeMethodAsync(HttpMethod.Put, "shoppingbasket", $"/api/baskets/{basketId}/coupon", couponForUpdate);
        }
    }
}
