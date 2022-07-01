using Dapr.Client;
using GloboTicket.Web.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GloboTicket.Web.Services
{
    public class DiscountDaprService : IDiscountService
    {
        private readonly DaprClient daprClient;

        public DiscountDaprService(DaprClient daprClient)
        {
            this.daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        }

        public async Task<Coupon> GetCouponByCode(string code)
        {            
            if (code == string.Empty)
                return null;

            var response = await daprClient.InvokeMethodAsync<Coupon>(HttpMethod.Get, "discountgrpc", $"/api/discount/code/{code}");                        
            return response;
        }

        public async Task<Coupon> GetCouponById(Guid couponId)
        {
            var response = await daprClient.InvokeMethodAsync<Coupon>(HttpMethod.Get, "discountgrpc", $"/api/discount/{couponId}");
            return response;
        }

        public async Task UseCoupon(Guid couponId)
        {
            await daprClient.InvokeMethodAsync(HttpMethod.Put, "discountgrpc", $"/api/discount/use/{couponId}", couponId);            
        }
    }
}
