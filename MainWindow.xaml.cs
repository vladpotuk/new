using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private TcpListener server;
        private List<TcpClient> clients = new List<TcpClient>();
        private Dictionary<TcpClient, string> clientMoves = new Dictionary<TcpClient, string>();
        private Dictionary<TcpClient, int> clientScores = new Dictionary<TcpClient, int>();
        private int round = 0;
        private object lockObj = new object();
        private int totalRounds = 5;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Log("Сервер запущено...");

            Thread serverThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    lock (lockObj)
                    {
                        clients.Add(client);
                        clientScores[client] = 0;
                    }
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            });
            serverThread.Start();
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[256];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Log("Клієнт: " + clientMessage);

                lock (lockObj)
                {
                    if (clientMessage == "Визнати поразку")
                    {
                        clientScores[client] = -1;
                        EndGame();
                        return;
                    }

                    clientMoves[client] = clientMessage;
                    if (clientMoves.Count == clients.Count)
                    {
                        
                        ProcessRound();
                        clientMoves.Clear();
                        round++;

                        if (round == totalRounds || clientScores.ContainsValue(-1))
                        {
                            EndGame();
                            break;
                        }
                    }
                }
            }
        }

        private void ProcessRound()
        {
            
            foreach (var client in clients)
            {
                string move1 = clientMoves[clients[0]];
                string move2 = clientMoves[clients[1]];
                string result = DetermineRoundWinner(move1, move2);
                if (result == "Перемога гравця 1")
                {
                    clientScores[clients[0]]++;
                }
                else if (result == "Перемога гравця 2")
                {
                    clientScores[clients[1]]++;
                }

                string serverResponse = $"Раунд {round + 1}:\n{result}\nРахунок: Гравець 1 - {clientScores[clients[0]]}, Гравець 2 - {clientScores[clients[1]]}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(serverResponse);
                foreach (var c in clients)
                {
                    NetworkStream ns = c.GetStream();
                    ns.Write(responseBytes, 0, responseBytes.Length);
                }
            }
        }

        private string DetermineRoundWinner(string move1, string move2)
        {
            if (move1 == move2)
            {
                return "Нічия";
            }

            if ((move1 == "Камінь" && move2 == "Ножиці") ||
                (move1 == "Ножиці" && move2 == "Папір") ||
                (move1 == "Папір" && move2 == "Камінь"))
            {
                return "Перемога гравця 1";
            }

            return "Перемога гравця 2";
        }

        private void EndGame()
        {
            string finalResult = DetermineGameWinner();
            byte[] finalResponseBytes = Encoding.UTF8.GetBytes(finalResult);

            foreach (var client in clients)
            {
                NetworkStream ns = client.GetStream();
                ns.Write(finalResponseBytes, 0, finalResponseBytes.Length);
                ns.Close();
                client.Close();
            }
            Log(finalResult);
        }

        private string DetermineGameWinner()
        {
            int score1 = clientScores[clients[0]];
            int score2 = clientScores[clients[1]];

            if (score1 > score2)
            {
                return "Гравець 1 переміг у грі!";
            }
            else if (score2 > score1)
            {
                return "Гравець 2 переміг у грі!";
            }
            else
            {
                return "Гра завершилась нічиєю!";
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ServerLog.Text += message + Environment.NewLine;
            });
        }
    }
}
