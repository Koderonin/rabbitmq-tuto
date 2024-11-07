using System.Text;
using RabbitMQ.Client;

public class EmitLogDirect
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

        channel.ExchangeDeclare(exchange: "direct_logs",
                                type: ExchangeType.Direct);
        
        var severity = (args.Length > 0) ? args[0] : "info";
        
        string message = GetMessage(args);
        byte[] body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "direct_logs",
                            routingKey: severity,
                            basicProperties: null,
                            body: body);
                            
        Console.WriteLine($" [x] Sent {message} - Priority {severity}");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    static string GetMessage(string[] args)
    {
        return (args.Length > 1) ? string.Join(" ", args.Skip(1).ToArray()) : "Hello World!";
    }
}