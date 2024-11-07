using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ReceiveLogs
{
    public static void Main(string[] args)
    {
        ConnectionFactory factory = new() 
        { 
            HostName = "localhost",
            Port = 5672,
            ClientProvidedName = "Worker",
            UserName = "vicen",
            Password = "admin"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: "logs",
                                type: ExchangeType.Fanout);
        // this creates a non-durable, exclusive, autodelete queue with a generated name
        var queueName = channel.QueueDeclare().QueueName;

        channel.QueueBind(queue: queueName,
                        exchange: "logs",
                        routingKey: string.Empty);

        Console.WriteLine(" [*] Waiting for logs.");

        EventingBasicConsumer consumer = new(channel);
        consumer.Received += (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");
            Console.WriteLine(" [x] Done");
            
            // here channel could also be accessed as ((EventingBasicConsumer)sender).Model
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        
        channel.BasicConsume(queue: queueName,
                            autoAck: false,
                            consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}