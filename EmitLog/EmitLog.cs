using System.Text;
using RabbitMQ.Client;

public class EmitLog
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

        channel.ExchangeDeclare(exchange: "logs",
                                type: ExchangeType.Fanout);

        string message = GetMessage(args);
        byte[] body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "logs",
                            routingKey: string.Empty,
                            basicProperties: null,
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