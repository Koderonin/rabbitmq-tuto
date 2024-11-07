using System.Text;
using RabbitMQ.Client;

public class EmitLogTopic
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

        channel.ExchangeDeclare(exchange: "topic_logs",
                                type: ExchangeType.Topic);
        
        var routingKey = (args.Length > 0) ? args[0] : "anonymous.info";
        
        string message = GetMessage(args);
        byte[] body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "topic_logs",
                            routingKey: routingKey,
                            basicProperties: null,
                            body: body);
                            
        Console.WriteLine($" [x] Sent {message} - Key: {routingKey}");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    static string GetMessage(string[] args)
    {
        return (args.Length > 1) ? string.Join(" ", args.Skip(1).ToArray()) : "Hello World!";
    }
}