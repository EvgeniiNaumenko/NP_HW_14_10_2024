using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
class Program
{
    static void Main()
    {
        Client client = new Client();
        bool exit = false;

        while (!exit)
        {
            Console.Clear();
            Console.WriteLine("=== Главное меню ===");
            Console.WriteLine("1. Список комплектующих");
            Console.WriteLine("2. Узнать стоимость детали");
            Console.WriteLine("3. Выйти");

            Console.Write("Выберите опцию: ");
            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    client.GetComponentList();
                    break;
                case "2":
                    Console.WriteLine("Введите название комплектующей:");
                    client.SendRequest(Console.ReadLine());
                    break;
                case "3":
                    Console.WriteLine("Выход из программы...");
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Неправильный ввод, попробуйте снова.");
                    break;
            }

            if (!exit)
            {
                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey();
            }
        }
    }
}
public class Client
{
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private readonly int serverPort = 9000;

    public Client()
    {
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
    }

    public void SendRequest(string part)
    {
        string message = $"PRICE:{part}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, serverEndPoint);

        byte[] responseData = udpClient.Receive(ref serverEndPoint);
        string response = Encoding.UTF8.GetString(responseData);
        Console.WriteLine("Ответ от сервера: " + response);
    }
    public void GetComponentList()
    {
        string message = "LIST";
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, serverEndPoint);

        byte[] responseData = udpClient.Receive(ref serverEndPoint);
        string response = Encoding.UTF8.GetString(responseData);
        Console.WriteLine("Список комплектующих: " + response);
    }
}
