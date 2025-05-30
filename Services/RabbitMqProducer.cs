using RabbitMQ.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MultiChainAPI.Functionality;

namespace MultiChainAPI.Services
{
    public class RabbitMqProducer : IRabbitMqProducer
    {
        // List of RabbitMQ instances
        private readonly List<string> rabbitMqUrls = new List<string>
        {
            "amqp://localhost:5672",  // Node 1
            //"amqp://localhost:5673",  // Node 2
            //"amqp://localhost:5674" ,  // Node 3
            "amqp://localhost"
        };

        //private const string QueueName = "MyQueue";
       // private const string ExamQueueName = "ExamQueue";

        // A static variable to maintain the round-robin index
        private static int currentIndex = -1;

        public async Task SendDataToQueue(object message, string QueueName)
        {
            IConnection connection = null;
            IModel channel = null;

            // Determine the next RabbitMQ instance using round-robin
            string rabbitMqUrl = GetNextRabbitMqUrl();

            try
            {
                var factory = new ConnectionFactory() { Uri = new Uri(rabbitMqUrl) };
                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                // Declare the queue (ensure it exists)
                channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Serialize the message
                var jsonMessage = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                // Publish the message to the queue
                channel.BasicPublish(
                    exchange: "",
                    routingKey: QueueName,
                    basicProperties: null,
                    body: body
                );

                Console.WriteLine($"Data sent to RabbitMQ queue on node {rabbitMqUrl}: {QueueName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending message to RabbitMQ at {rabbitMqUrl}: {ex.Message}");
            }
            finally
            {
                channel?.Close();
                connection?.Close();
            }

            await Task.CompletedTask;
        }

        private static readonly object lockObject = new object();

        private string GetNextRabbitMqUrl()
        {
            lock (lockObject)
            {
                currentIndex = (currentIndex + 1) % rabbitMqUrls.Count;
                return rabbitMqUrls[currentIndex];
            }
        }

    }
}
