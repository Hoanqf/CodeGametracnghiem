using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuizServer
{
    public partial class KTServer : Window
    {
        private Socket listener;
        private List<Socket> clients = new List<Socket>();
        private DispatcherTimer selectorTimer;
        private Dictionary<Socket, ClientSession> clientSessions = new Dictionary<Socket, ClientSession>();
        private string connectionString = "Data Source=LAPTOP-7O4BQK2O\\MSSQLSERVER01;Initial Catalog=QuizDatabase;Persist Security Info=True;User ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True";
        private bool isQuizStarted = false;
        private bool isFinalResultsSent = false;

        private class ClientSession
        {
            public Socket Socket { get; set; }
            public int CurrentQuestionIndex { get; set; } = 0;
            public List<Tuple<string, string, string, string, string, string, int>> Questions { get; set; } = new List<Tuple<string, string, string, string, string, string, int>>();
            public int Score { get; set; } = 0;
            public bool SessionEnded { get; set; } = false;
            public string PlayerName { get; set; }
        }

        public KTServer()
        {
            InitializeComponent();
            selectorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            selectorTimer.Tick += SelectorLoop;
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Port không hợp lệ!");
                return;
            }

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Listen(100);
            listener.Blocking = false;

            clients.Clear();
            clients.Add(listener);
            clientSessions.Clear();
            selectorTimer.Start();

            lblServerStatus.Text = $"Đang chạy trên cổng {port}";
            Log($"Server đang chạy tại cổng {port}");
            CheckDatabaseConnection();
        }

        private void SelectorLoop(object sender, EventArgs e)
        {
            List<Socket> readList = new List<Socket>(clients);
            Socket.Select(readList, null, null, 1000);

            foreach (var socket in readList)
            {
                if (socket == listener)
                {
                    Socket client = listener.Accept();
                    client.Blocking = false;
                    clients.Add(client);
                    clientSessions[client] = new ClientSession { Socket = client };
                    Log("Kết nối từ: " + client.RemoteEndPoint);
                }
                else
                {
                    byte[] buffer = new byte[1024];
                    try
                    {
                        int received = socket.Receive(buffer);
                        if (received > 0)
                        {
                            string msg = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                            Log("Từ client: " + msg);

                            HandleClientMessage(socket, msg);
                        }
                        else
                        {
                            CloseClient(socket);
                        }
                    }
                    catch
                    {
                        CloseClient(socket);
                    }
                }
            }
        }

        private void CloseClient(Socket socket)
        {
            try { Log("Ngắt kết nối: " + socket.RemoteEndPoint); } catch { }
            try { clients.Remove(socket); } catch { }
            try { clientSessions.Remove(socket); } catch { }
            try { socket.Close(); } catch { }
        }

        private void HandleClientMessage(Socket socket, string msg)
        {
            if (msg.StartsWith("ANSWER|"))
            {
                string[] parts = msg.Split('|');
                if (parts.Length == 4)
                {
                    string playerName = parts[1];
                    string selectedAnswer = parts[2];
                    bool isCorrect = bool.Parse(parts[3]);

                    var session = clientSessions[socket];
                    session.PlayerName = playerName;
                    if (isCorrect) session.Score += 10;

                    Log($"Client {playerName} trả lời: {selectedAnswer}, đúng: {isCorrect}, điểm hiện tại: {session.Score}");
                }
            }
            else if (msg == "NEXT")
            {
                var session = clientSessions[socket];
                if (session.CurrentQuestionIndex >= session.Questions.Count)
                {
                    socket.Send(Encoding.UTF8.GetBytes("END|Đã hết câu hỏi\n"));
                    session.SessionEnded = true;
                    if (!isFinalResultsSent && AllSessionsEnded())
                    {
                        SendFinalResultsToAllClients();
                        isFinalResultsSent = true;
                    }
                }
                else
                {
                    SendQuestionToClient(session);
                }
            }
        }

        private void SendQuestionToClient(ClientSession session)
        {
            try
            {
                if (session.Questions.Count == 0) return;

                if (session.CurrentQuestionIndex < session.Questions.Count)
                {
                    var q = session.Questions[session.CurrentQuestionIndex];
                    string data = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}\n", q.Item1, q.Item2, q.Item3, q.Item4, q.Item5, q.Item6, q.Item7);
                    session.Socket.Send(Encoding.UTF8.GetBytes(data));
                    session.CurrentQuestionIndex++;
                }
                else
                {
                    session.Socket.Send(Encoding.UTF8.GetBytes("END|Đã hết câu hỏi\n"));
                    session.SessionEnded = true;
                    if (!isFinalResultsSent && AllSessionsEnded())
                    {
                        SendFinalResultsToAllClients();
                        isFinalResultsSent = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Lỗi gửi câu hỏi: " + ex.Message);
            }
        }

        private bool AllSessionsEnded()
        {
            foreach (var session in clientSessions.Values)
                if (!session.SessionEnded) return false;
            return true;
        }

        private void btnStartQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (clientSessions.Count == 0)
            {
                MessageBox.Show("Chưa có client nào kết nối.");
                return;
            }

            isQuizStarted = true;
            isFinalResultsSent = false;
            lblQuizStatus.Text = "Game đang diễn ra...";

            foreach (var session in clientSessions.Values)
            {
                try
                {
                    session.Questions = LoadQuestionsFromDatabase();
                    session.CurrentQuestionIndex = 0;
                    session.Score = 0;
                    session.Socket.Send(Encoding.UTF8.GetBytes("START\n"));
                }
                catch (Exception ex)
                {
                    Log("Lỗi khởi tạo câu hỏi: " + ex.Message);
                }
            }

            Log("Quiz bắt đầu.");
        }

        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            selectorTimer.Stop();
            foreach (var client in clients)
            {
                try { client.Close(); } catch { }
            }
            clients.Clear();
            clientSessions.Clear();
            lblServerStatus.Text = "Server đã dừng.";
            lblQuizStatus.Text = "Quiz kết thúc.";
            Log("Server đã dừng và ngắt mọi kết nối.");
        }

        private List<Tuple<string, string, string, string, string, string, int>> LoadQuestionsFromDatabase()
        {
            List<Tuple<string, string, string, string, string, string, int>> questions = new List<Tuple<string, string, string, string, string, string, int>>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT TOP 10 * FROM Questions ORDER BY NEWID()", conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        questions.Add(Tuple.Create(
                            reader["QuestionText"].ToString(),
                            reader["OptionA"].ToString(),
                            reader["OptionB"].ToString(),
                            reader["OptionC"].ToString(),
                            reader["OptionD"].ToString(),
                            reader["CorrectAnswer"].ToString(),
                            Convert.ToInt32(reader["TimeLimit"])
                        ));
                    }
                }
                catch (Exception ex)
                {
                    Log("Lỗi truy vấn câu hỏi: " + ex.Message);
                }
            }
            return questions;
        }

        private void SendFinalResultsToAllClients()
        {
            StringBuilder sb = new StringBuilder("END|");
            foreach (var session in clientSessions.Values)
            {
                if (!string.IsNullOrEmpty(session.PlayerName))
                {
                    sb.Append(session.PlayerName + ":" + session.Score + ",");
                }
            }
            string result = sb.ToString().TrimEnd(',') + "\n";

            foreach (var session in clientSessions.Values)
            {
                try
                {
                    session.Socket.Send(Encoding.UTF8.GetBytes(result));
                }
                catch (Exception ex)
                {
                    Log("Lỗi gửi kết quả cuối: " + ex.Message);
                }
            }

            Log("Đã gửi kết quả tổng kết: " + result);
        }

        private void CheckDatabaseConnection()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    Dispatcher.Invoke(() =>
                    {
                        lblDbStatus.Text = "Kết nối thành công";
                        lblDbStatus.Foreground = Brushes.Green;
                        SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Questions", conn);
                        int count = (int)cmd.ExecuteScalar();
                        lblQuestionCount.Text = "Tổng câu hỏi: " + count;
                    });
                    Log("Kết nối CSDL thành công.");
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lblDbStatus.Text = "Kết nối thất bại";
                        lblDbStatus.Foreground = Brushes.Red;
                    });
                    Log("Lỗi kết nối CSDL: " + ex.Message);
                }
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\n");
                txtLog.ScrollToEnd();
            });
        }
    }
}