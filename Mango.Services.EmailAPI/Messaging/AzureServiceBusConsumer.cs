using Azure.Messaging.ServiceBus;
using Mango.Services.EmailAPI.Message;
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string emailCartQueue;
        private readonly string emailRegisteredUserQueue;

        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        private readonly string orderCreated_Topic;
        private readonly string orderCreated_Email_Subscription;

        private ServiceBusProcessor _processorEmailCart;
        private ServiceBusProcessor _processorRegisteredUser;
        private ServiceBusProcessor _processorEmailOrderPlaced;

        public AzureServiceBusConsumer(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;

            serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
            var client = new ServiceBusClient(serviceBusConnectionString);
            
            emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");
            _processorEmailCart = client.CreateProcessor(emailCartQueue);

            emailRegisteredUserQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailRegisteredUserQueue");
            _processorRegisteredUser = client.CreateProcessor(emailRegisteredUserQueue);

            orderCreated_Topic = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
            orderCreated_Email_Subscription = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreated_Email_Subscription");
            _processorEmailOrderPlaced = client.CreateProcessor(orderCreated_Topic, orderCreated_Email_Subscription);
        }

        public async Task Start()
        {
            _processorEmailCart.ProcessMessageAsync += OnEmailCartRequestReceived;
            _processorEmailCart.ProcessErrorAsync += ErrorHandler;
            await _processorEmailCart.StartProcessingAsync();

            _processorRegisteredUser.ProcessMessageAsync += OnRegisteredUserRequestReceived;
            _processorRegisteredUser.ProcessErrorAsync += ErrorHandler;
            await _processorRegisteredUser.StartProcessingAsync();

            _processorEmailOrderPlaced.ProcessMessageAsync += OnOrderPlacedRequestReceived;
            _processorEmailOrderPlaced.ProcessErrorAsync += ErrorHandler;
            await _processorEmailOrderPlaced.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await _processorEmailCart.StopProcessingAsync();
            await _processorEmailCart.DisposeAsync();

            await _processorRegisteredUser.StopProcessingAsync();
            await _processorRegisteredUser.DisposeAsync();

            await _processorEmailOrderPlaced.StopProcessingAsync();
            await _processorEmailOrderPlaced.DisposeAsync();
        }

        private async Task OnEmailCartRequestReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CartDto objMessage = JsonConvert.DeserializeObject<CartDto>(body);

            try
            {
                // Try to log email
                await _emailService.EmailCartAndLog(objMessage);

                await args.CompleteMessageAsync(args.Message);
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        private async Task OnOrderPlacedRequestReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            RewardsMessage objMessage = JsonConvert.DeserializeObject<RewardsMessage>(body);

            try
            {
                // Try to log email
                await _emailService.LogOrderPlaced(objMessage);

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task OnRegisteredUserRequestReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            string objMessage = JsonConvert.DeserializeObject<string>(body);

            try
            {
                // Try to log email
                await _emailService.EmailRegisteredUserAndLog(objMessage);

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

    }
}
