using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main()
    {
        UDPServer udpServer = new UDPServer();
        udpServer.Start();
    }
    public static Dictionary<string, decimal> products = new Dictionary<string, decimal>()
        {
            {"product1",50.00m },
            {"product2",60.00m },
            {"product3",70.00m },
            {"product4",80.00m },
            {"product5",90.00m },
            {"product6",100.00m }
        };
    public class UDPServer
    {
        private UdpClient udpServer;
        private IPEndPoint remoteEndPoint;
        private ConcurrentDictionary<string, ClientInfo> clients;
        private Timer cleanupTimer;
        private readonly int port = 9000;
        private readonly int maxRequestsPerHour = 10;
        private readonly TimeSpan clientTimeout = TimeSpan.FromMinutes(1);

        public UDPServer()
        {
            udpServer = new UdpClient(port);
            clients = new ConcurrentDictionary<string, ClientInfo>();
            cleanupTimer = new Timer(CleanInactiveClients, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            Console.WriteLine("Сервер запущен...");
        }

        public void Start()
        {
            while (true)
            {
                remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
                byte[] data = udpServer.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                string clientKey = remoteEndPoint.ToString();

                Console.WriteLine($"Запрос от {clientKey}: {message}");

                if (clients.TryGetValue(clientKey, out ClientInfo client))
                {
                    if (client.CanSendRequest(maxRequestsPerHour))
                    {
                        HandleClientRequest(client, message);
                    }
                    else
                    {
                        string response = "Превышено количество запросов.";
                        SendResponse(response, remoteEndPoint);
                    }
                }
                else
                {
                    var newClient = new ClientInfo { LastActive = DateTime.Now };
                    clients.TryAdd(clientKey, newClient);
                    HandleClientRequest(newClient, message);
                }
            }
        }

        private void HandleClientRequest(ClientInfo client, string message)
        {
            client.AddRequest();
            client.LastActive = DateTime.Now;
            string[] splitMessage = message.Split(':');
            if (splitMessage[0] == "PRICE")
            {
                string response = GetPriceForPart(splitMessage[1]);
                SendResponse(response, remoteEndPoint);
            }
            else
            {
                if (splitMessage[0]=="LIST")
                {
                    string response = String.Join(", ", products.Keys);
                    SendResponse(response, remoteEndPoint);
                }
            }

            LogClientRequest(remoteEndPoint.ToString(), message);
        }

        private string GetPriceForPart(string part)
        {
            if (products.ContainsKey(part)) return $"Цена на {part}: {products[part]}$";
            else return $"Продукт {part} отсутствует!";
        }

        private void SendResponse(string response, IPEndPoint clientEndPoint)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(response);
            udpServer.Send(responseData, responseData.Length, clientEndPoint);
        }

        private void CleanInactiveClients(object state)
        {
            foreach (var client in clients)
            {
                if (DateTime.Now - client.Value.LastActive > clientTimeout)
                {
                    clients.TryRemove(client.Key, out _);
                    Console.WriteLine($"Клиент {client.Key} отключен по таймауту.");
                }
            }
        }

        private void LogClientRequest(string client, string request)
        {
            string logEntry = $"{DateTime.Now}: Клиент {client} запросил {request}";
            System.IO.File.AppendAllText("server_log.txt", logEntry + Environment.NewLine);
        }
    }

    public class ClientInfo
    {
        public DateTime LastActive { get; set; }
        private int requestCount;
        private DateTime requestWindowStart;

        public ClientInfo()
        {
            requestCount = 0;
            requestWindowStart = DateTime.Now;
        }

        public bool CanSendRequest(int maxRequests)
        {
            if (DateTime.Now - requestWindowStart > TimeSpan.FromHours(1))
            {
                requestCount = 0;
                requestWindowStart = DateTime.Now;
            }

            return requestCount < maxRequests;
        }

        public void AddRequest()
        {
            requestCount++;
        }
    }

}