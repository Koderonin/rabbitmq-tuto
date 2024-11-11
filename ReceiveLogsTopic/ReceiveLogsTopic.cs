using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ReceiveLogsDirect
{
    public static void Main(string[] args)
    {
        // makes more sense to check this up here, don't you think?
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: {0} [binding_key...]", Environment.GetCommandLineArgs()[0]);
            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
            Environment.ExitCode = 1;
            return;
        }

        ConnectionFactory factory = new() 
        { 
            HostName = "localhost",
            Port = 5672,
            ClientProvidedName = "Worker",
            UserName = "vicen",
            Password = "admin"
        };

        using IConnection? connection = factory.CreateConnection();
        using IModel? channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: "topic_logs",
                                type: ExchangeType.Topic);
        // this creates a non-durable, exclusive, autodelete queue with a generated name
        var queueName = channel.QueueDeclare().QueueName;

        foreach (var bindingKey in args)
        {
            channel.QueueBind(queue: queueName,
                            exchange: "topic_logs",
                            routingKey: bindingKey);
        }

        Console.WriteLine(" [*] Waiting for logs. Press [enter] to exit.");

        EventingBasicConsumer consumer = new(channel);
        consumer.Received += (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {ea.RoutingKey}:{message}");
            
            // here channel could also be accessed as ((EventingBasicConsumer)sender).Model
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        
        channel.BasicConsume(queue: queueName,
                            autoAck: false,
                            consumer: consumer);

        Console.ReadLine();
    }
}