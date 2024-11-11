using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RpcServer
{
    private const string QUEUE_NAME = "rpc_queue";

    public static void Main(string[] args)
    {
        ConnectionFactory factory = new()
        {
            HostName = "localhost",
            Port = 5672,
            ClientProvidedName = "RPC Server",
            UserName = "vicen",
            Password = "admin"
        };
        
        using IConnection? connection = factory.CreateConnection();
        using IModel? channel = connection.CreateModel();

        channel.QueueDeclare(queue: QUEUE_NAME,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
        
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        EventingBasicConsumer consumer = new(channel);

        channel.BasicConsume(queue: QUEUE_NAME,
                            autoAck: false,
                            consumer: consumer);
        Console.Write("Awaiting for RPC requests...");

        consumer.Received += (model, ea) =>
        {
            string response = string.Empty;

            byte[] body = ea.Body.ToArray();
            IBasicProperties eaProperties = ea.BasicProperties;
            IBasicProperties replayProperties = channel.CreateBasicProperties();
            replayProperties.CorrelationId = eaProperties.CorrelationId;

            try
            {
                string message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[.] Fib({message})");
                
                int n = int.Parse(message);
                response = Fib(n).ToString();
            }
            finally
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                channel.BasicPublish(exchange: string.Empty,
                                    routingKey: eaProperties.ReplyTo,
                                    basicProperties: replayProperties,
                                    body: responseBytes);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        Console.WriteLine("Press [enter] to exit.");
        Console.ReadLine();
    }
    static int Fib(int n)
    {
        if (n < 0)
        {
            return (-1);
        }
        if (n is 0 or 1)
        {
            return (n);
        }
        return (Fib(n - 1) + Fib(n - 2));
    }
}

