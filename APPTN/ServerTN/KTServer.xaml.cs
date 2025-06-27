using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace QuizServer
{
    public partial class KTServer : Window
    {
        private TcpListener server;
        private Thread serverThread;
        private string connectionString = "Data Source=LAPTOP-85NRQVNH;Initial Catalog=Quizz;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";


        private bool isServerRunning = false;
        private bool isQuizStarted = false;
        private bool isServerStopping = false;
        private bool isFinalResultsSent = false;  // Đảm bảo gửi 1 lần


        // Lớp lưu phiên làm việc của client
        private class ClientSession
        {
            public TcpClient TcpClient { get; set; }
            public StreamWriter Writer { get; set; }
            public int CurrentQuestionIndex { get; set; } = 0;
            // Tuple gồm: QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, TimeLimit
            public List<(string, string, string, string, string, string, int)> Questions { get; set; } = new List<(string, string, string, string, string, string, int)>();
            public int Score { get; set; } = 0;  // điểm tích lũy
            public bool SessionEnded { get; set; } = false;  // thêm dòng này
            public string PlayerName { get; set; } // thêm dòng này
        }

        // Danh sách client hiện tại, khóa để đồng bộ
        private Dictionary<TcpClient, ClientSession> clientSessions = new Dictionary<TcpClient, ClientSession>();
        private readonly object clientLock = new object();

        public KTServer()
        {
            InitializeComponent();
        }

        // Bắt đầu server
        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Port không hợp lệ!");
                return;
            }

            if (isServerRunning)
            {
                MessageBox.Show("Server đã đang chạy.");
                return;
            }

            isServerStopping = false;
            serverThread = new Thread(() => StartServer(port));
            serverThread.IsBackground = true;
            serverThread.Start();

            isServerRunning = true;
            btnStartQuiz.IsEnabled = true;
            btnStartServer.IsEnabled = false;
            btnStopServer.IsEnabled = true;

            lblServerStatus.Text = "Đang chạy trên cổng " + port;
            Log("Server khởi động trên cổng " + port);

            CheckDatabaseConnection();
        }

        // Vòng lặp chờ client kết nối
        private void StartServer(int port)
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                Log("Server đã khởi động.");

                while (!isServerStopping)
                {
                    if (!server.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    TcpClient client = server.AcceptTcpClient();
                    Log("Client kết nối từ: " + client.Client.RemoteEndPoint);

                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (SocketException ex)
            {
                if (!isServerStopping)
                    Log("Lỗi Server: " + ex.Message);
            }
        }

        // Xử lý từng client riêng biệt
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            StreamWriter writer = null;
            StreamReader reader = null;

            try
            {
                stream = client.GetStream();
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                reader = new StreamReader(stream, Encoding.UTF8);

                var session = new ClientSession
                {
                    TcpClient = client,
                    Writer = writer
                };

                lock (clientLock)
                {
                    clientSessions[client] = session;
                }

                // Nếu quiz đã bắt đầu, gửi thông báo và câu hỏi đầu tiên
                if (isQuizStarted)
                {
                    writer.WriteLine("START");
                    session.Questions = LoadQuestionsFromDatabase();
                    session.CurrentQuestionIndex = 0;
                    SendQuestionToClient(session);
                }

                while (!isServerStopping && client.Connected)
                {
                    string line = null;
                    try
                    {
                        line = reader.ReadLine();
                        if (line == null) break;  // client đóng kết nối bình thường
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException is SocketException sockEx &&
                            sockEx.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            Log("Client đóng kết nối đột ngột, không phải lỗi nghiêm trọng.");
                        }
                        else
                        {
                            Log("Lỗi xử lý client: " + ex.Message);
                        }
                        break; // thoát vòng lặp khi lỗi kết nối
                    }

                    Log($"Từ client: {line}");

                    if (line.StartsWith("ANSWER|"))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 4)
                        {
                            string playerName = parts[1];
                            string selectedAnswer = parts[2];
                            bool isCorrect = bool.Parse(parts[3]);

                            session.PlayerName = playerName; // cập nhật tên người chơi

                            if (isCorrect)
                            {
                                session.Score += 10;
                            }

                            Log($"Client {playerName} trả lời: {selectedAnswer}, đúng: {isCorrect}, điểm hiện tại: {session.Score}");
                        }
                        else
                        {
                            Log("Dữ liệu trả lời client sai định dạng.");
                        }
                    }
                    else if (line == "NEXT")
                    {
                        if (session.CurrentQuestionIndex >= session.Questions.Count)
                        {
                            session.Writer.WriteLine("END|Đã hết câu hỏi");
                            session.SessionEnded = true;
                            Log("Đã gửi thông báo hết câu hỏi.");

                            bool allEnded;
                            lock (clientLock)
                            {
                                allEnded = clientSessions.Values.All(s => s.SessionEnded);
                            }

                            if (allEnded && !isFinalResultsSent)
                            {
                                SendFinalResultsToAllClients();
                                isFinalResultsSent = true;
                            }
                        }
                        SendQuestionToClient(session);
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Log("Lỗi xử lý client: " + ex.Message);
            }
            finally
            {
                lock (clientLock)
                {
                    if (clientSessions.ContainsKey(client))
                        clientSessions.Remove(client);
                }

                try { writer?.Close(); } catch { }
                try { reader?.Close(); } catch { }
                try { client?.Close(); } catch { }

                Log("Client ngắt kết nối.");
            }
        }


        // Gửi câu hỏi tiếp theo đến client
        private void SendQuestionToClient(ClientSession session)
        {
            try
            {
                if (session.CurrentQuestionIndex < session.Questions.Count)
                {
                    var q = session.Questions[session.CurrentQuestionIndex];
                    string data = $"{q.Item1}|{q.Item2}|{q.Item3}|{q.Item4}|{q.Item5}|{q.Item6}|{q.Item7}";
                    session.Writer.WriteLine(data);
                    Log("Đã gửi câu hỏi cho client.");
                    session.CurrentQuestionIndex++; 
                }
                else
                {
                    session.Writer.WriteLine("END|Đã hết câu hỏi");
                    session.SessionEnded = true;
                    Log("Đã gửi thông báo hết câu hỏi.");

                    // Kiểm tra nếu tất cả client đều đã kết thúc
                    bool allEnded;
                    lock (clientLock)
                    {
                        allEnded = clientSessions.Values.All(s => s.SessionEnded);
                    }

                    if (allEnded)
                    {
                        SendFinalResultsToAllClients();
                    }
                }

            }
            catch (Exception ex)
            {
                Log("Lỗi gửi câu hỏi: " + ex.Message);
            }
        }

        // Load danh sách câu hỏi từ database
        private List<(string, string, string, string, string, string, int)> LoadQuestionsFromDatabase()
        {
            var questions = new List<(string, string, string, string, string, string, int)>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT TOP 10 * FROM Questions ORDER BY NEWID()";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        questions.Add((
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

                // 👉 Sau khi load, xáo trộn ngẫu nhiên
                Random rng = new Random();
                questions = questions.OrderBy(q => rng.Next()).ToList();
            }
            catch (Exception ex)
            {
                Log("Lỗi truy vấn câu hỏi: " + ex.Message);
            }
            return questions;
        }

        // Bắt đầu quiz cho tất cả client đã kết nối
        private void btnStartQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (!isServerRunning)
            {
                MessageBox.Show("Vui lòng khởi động server trước.");
                return;
            }

            if (isQuizStarted)
            {
                MessageBox.Show("Game đã bắt đầu.");
                return;
            }

            isQuizStarted = true;
            isFinalResultsSent = false; // 👈 Reset biến này khi bắt đầu quiz mới
            lblQuizStatus.Text = "Game đang diễn ra...";
            Log("Game trắc nghiệm đã bắt đầu.");

            lock (clientLock)
            {
                foreach (var kvp in clientSessions.ToList())
                {
                    var session = kvp.Value;
                    try
                    {
                        session.Writer.WriteLine("START");
                        session.Questions = LoadQuestionsFromDatabase();
                        session.CurrentQuestionIndex = 0;
                        session.Score = 0; // reset điểm khi bắt đầu
                        SendQuestionToClient(session);
                    }
                    catch (Exception ex)
                    {
                        Log("Lỗi gửi START/câu hỏi: " + ex.Message);
                        clientSessions.Remove(kvp.Key);
                    }
                }
            }
        }

        // Dừng server và ngắt kết nối client
        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            if (!isServerRunning)
            {
                MessageBox.Show("Server chưa được khởi động.");
                return;
            }

            isServerStopping = true;
            isServerRunning = false;
            isQuizStarted = false;

            try
            {
                server.Stop();

                lock (clientLock)
                {
                    foreach (var kvp in clientSessions)
                    {
                        try
                        {
                            kvp.Value.Writer.WriteLine("DISCONNECT");
                            kvp.Value.Writer.Close();
                            kvp.Key.Close();
                        }
                        catch { }
                    }
                    clientSessions.Clear();
                }

                Dispatcher.Invoke(() =>
                {
                    lblServerStatus.Text = "Server đã dừng.";
                    lblQuizStatus.Text = "Quiz đã kết thúc.";
                    btnStartQuiz.IsEnabled = false;
                    btnStartServer.IsEnabled = true;
                    btnStopServer.IsEnabled = false;
                });

                Log("Server đã dừng và ngắt kết nối tất cả client.");
            }
            catch (Exception ex)
            {
                Log("Lỗi khi dừng server: " + ex.Message);
            }
        }

        // Kiểm tra kết nối cơ sở dữ liệu
        private void CheckDatabaseConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    Dispatcher.Invoke(() =>
                    {
                        lblDbStatus.Text = "Kết nối thành công";
                        lblDbStatus.Foreground = Brushes.Green;

                        SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Questions", conn);
                        int count = (int)cmd.ExecuteScalar();
                        lblQuestionCount.Text = $"Tổng câu hỏi: {count}";
                    });

                    Log("Kết nối CSDL thành công.");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    lblDbStatus.Text = "Kết nối thất bại";
                    lblDbStatus.Foreground = Brushes.Red;
                });
                Log("Lỗi khi kết nối SQL: " + ex.Message);
            }
        }

        // Ghi log ra giao diện
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtLog.ScrollToEnd();
            });
        }

        private void SendFinalResultsToAllClients()
        {
            StringBuilder resultBuilder = new StringBuilder("END|");

            lock (clientLock)
            {
                foreach (var session in clientSessions.Values)
                {
                    if (!string.IsNullOrEmpty(session.PlayerName))
                    {
                        resultBuilder.Append($"{session.PlayerName}:{session.Score},");
                    }
                }
            }

            string resultMessage = resultBuilder.ToString().TrimEnd(',');

            lock (clientLock)
            {
                foreach (var session in clientSessions.Values)
                {
                    try
                    {
                        session.Writer.WriteLine(resultMessage);
                    }
                    catch (Exception ex)
                    {
                        Log($"Lỗi gửi kết quả cuối cho {session.PlayerName}: {ex.Message}");
                    }
                }
            }

            Log("Đã gửi kết quả tổng kết: " + resultMessage);
        }

    }
}
