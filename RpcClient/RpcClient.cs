using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

    public RpcClient()
    {
        ConnectionFactory factory = new() 
        { 
            HostName = "localhost",
            Port = 5672,
            ClientProvidedName = "Worker",
            UserName = "vicen",
            Password = "admin"
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        // de nuevo, cola con nombre autogenerado
        replyQueueName = channel.QueueDeclare().QueueName;

        EventingBasicConsumer consumer = new(channel);
        consumer.Received += (model, ea) =>
        {
            string? correlationId = ea.BasicProperties.CorrelationId;
            if (callbackMapper.TryRemove(correlationId, out TaskCompletionSource<string>? tcs))
            {
                byte[]? body = ea.Body.ToArray();
                string? response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        channel.BasicConsume(queue: replyQueueName,
                autoAck: false,
                consumer: consumer);
    }

    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        IBasicProperties reqProperties = channel.CreateBasicProperties();
        string correlationId = Guid.NewGuid().ToString();
        reqProperties.CorrelationId = correlationId;
        reqProperties.ReplyTo = replyQueueName;
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        TaskCompletionSource<string> tcs = new();
        callbackMapper.TryAdd(correlationId, tcs);

        channel.BasicPublish(exchange: string.Empty,
                            routingKey: QUEUE_NAME,
                            basicProperties: reqProperties,
                            body: messageBytes);

        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
        return tcs.Task;
    }

    public void Dispose()
    {
        channel.Close();
        connection.Close();
    }

    public class Rpc
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("RPC Client");
            string n = args.Length > 0 ? args[0] : "30";
            await InvokeAsync(n);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static async Task InvokeAsync(string n)
        {
            using RpcClient? rpcClient = new RpcClient();

            Task<string> call = rpcClient.CallAsync(n);
            
            Console.WriteLine(" [x] Requesting fib({0})", n);
            
            string? response = await call;
            
            Console.WriteLine(" [.] Got '{0}'", response);
        }
    }
}