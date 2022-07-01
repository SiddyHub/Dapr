using GloboTicket.Integration.MessagingBus;
using GloboTicket.Services.ShoppingBasket.Models;
using GloboTicket.Services.ShoppingBasket.Repositories;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GloboTicket.Services.ShoppingBasket.Worker
{
    public class ServiceBusListener : IHostedService
    {
        private readonly IConfiguration configuration;
        private ISubscriptionClient subscriptionClient;
        private readonly BasketLinesIntegrationRepository basketLinesRepository;

        public ServiceBusListener(IConfiguration configuration, BasketLinesIntegrationRepository basketLinesRepository)
        {
            this.configuration = configuration;
            this.basketLinesRepository = basketLinesRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            subscriptionClient = new SubscriptionClient(configuration.GetValue<string>("ServiceBusConnectionString"), configuration.GetValue<string>("PriceUpdatedMessageTopic"), configuration.GetValue<string>("subscriptionName"));

            var messageHandlerOptions = new MessageHandlerOptions(e =>
            {
                ProcessError(e.Exception);
                return Task.CompletedTask;
            })
            {
                MaxConcurrentCalls = 3,
                AutoComplete = false
            };

            subscriptionClient.RegisterMessageHandler(ProcessMessageAsync, messageHandlerOptions);

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken token)
        {
            var messageBody = Encoding.UTF8.GetString(message.Body);
            PriceUpdate priceUpdate = JsonConvert.DeserializeObject<PriceUpdate>(messageBody);

            await basketLinesRepository.UpdatePricesForIntegrationEvent(priceUpdate);

            await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.subscriptionClient.CloseAsync();
        }

        protected void ProcessError(Exception e)
        {
        }
    }
}
