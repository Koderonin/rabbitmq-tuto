using System.Text;
using RabbitMQ.Client;

public class NewTask
{
    public static void Main(string[] args)
    {
        ConnectionFactory factory = new() 
        { 
            HostName = "localhost",
            Port = 5672,
            ClientProvidedName = "Sender",
            UserName = "vicen",
            Password = "admin"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "task_queue",
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        string message = GetMessage(args);
        byte[] body = Encoding.UTF8.GetBytes(message);

        IBasicProperties properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(exchange: string.Empty,
                            routingKey: "task_queue",
                            basicProperties: properties,
                            body: body);
                            
        Console.WriteLine($" [x] Sent {message}");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    static string GetMessage(string[] args)
    {
        return (args.Length > 0) ? string.Join(",", args) : "Hello World!";
    }
}