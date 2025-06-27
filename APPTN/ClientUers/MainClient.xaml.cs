using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Animation;

namespace ClientApp
{
    public partial class MainClient : Window
    {
        private string playerName;
        private string serverIP;
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private DispatcherTimer timer;
        private int timeLeft;
        private string correctAnswer;
        private CancellationTokenSource cts;
        private Queue<string> questionQueue = new Queue<string>();
        private bool isWaitingResult = false;
        private bool isNextPending = false;
        private List<(string, int)> rankings = new List<(string, int)>();

        public MainClient(string name, string ip, TcpClient tcpClient)
        {
            InitializeComponent();
            playerName = name;
            serverIP = ip;
            client = tcpClient;
            stream = client.GetStream();
            reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            writer = new StreamWriter(stream, System.Text.Encoding.UTF8) { AutoFlush = true };

            txtUsername.Text = playerName;
            txtServerIP.Text = serverIP;

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;

            rdoA.Checked += Answer_Checked;
            rdoB.Checked += Answer_Checked;
            rdoC.Checked += Answer_Checked;
            rdoD.Checked += Answer_Checked;

            btnTraLoi.Visibility = Visibility.Collapsed;
            txtQuestion.Text = "⏳ Chờ server bắt đầu...";

            cts = new CancellationTokenSource();
            _ = Task.Run(() => WaitForServerCommands(cts.Token));
        }

        private async Task WaitForServerCommands(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string message = await reader.ReadLineAsync();
                    if (message == null) break;

                    Dispatcher.Invoke(() =>
                    {
                        if (message == "START") StartQuizUI();
                        else if (message.StartsWith("END|Đã hết câu hỏi"))
                        {
                            txtQuestion.Text = "🏁 Quiz kết thúc!";
                            HideQuizControls();
                            ShowResultOverlay("📢 Thông báo:", "Đã hết câu hỏi. Chờ kết quả tổng kết từ server...", Brushes.DarkRed);
                        }
                        else if (message.StartsWith("END|"))
                        {
                            EndQuiz(message.Substring(4));
                        }
                        else if (message.StartsWith("DISCONNECT"))
                        {
                            MessageBox.Show("Server đã ngắt kết nối.");
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            if (isWaitingResult) questionQueue.Enqueue(message);
                            else ProcessQuestion(message);
                        }
                    });

                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                ShowMessageAndClose("❌ Lỗi khi nhận dữ liệu từ server: " + ex.Message);
            }
        }

        private void ProcessQuestion(string data)
        {
            isNextPending = false;
            string[] parts = data.Split('|');
            if (parts.Length < 7)
            {
                txtQuestion.Text = "⚠ Dữ liệu câu hỏi không hợp lệ.";
                return;
            }

            txtQuestion.Text = parts[0];
            rdoA.Content = "A. " + parts[1];
            rdoB.Content = "B. " + parts[2];
            rdoC.Content = "C. " + parts[3];
            rdoD.Content = "D. " + parts[4];
            correctAnswer = parts[5];
            timeLeft = int.TryParse(parts[6], out int t) ? t : 10;
            txtTime.Text = $"{timeLeft}s";

            rdoA.IsChecked = rdoB.IsChecked = rdoC.IsChecked = rdoD.IsChecked = false;
            rdoA.Visibility = rdoB.Visibility = rdoC.Visibility = rdoD.Visibility = Visibility.Visible;
            txtResult.Text = "";
            txtResult.Foreground = Brushes.Black;
            gridOverlay.Visibility = Visibility.Collapsed;
            btnNext.Visibility = Visibility.Collapsed;

            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            txtTime.Text = $"{timeLeft}s";
            if (timeLeft <= 0)
            {
                timer.Stop();
                txtResult.Text = "⏰ Hết thời gian!";
                txtResult.Foreground = Brushes.OrangeRed;
                SendAnswer("", false);
                ShowResultOverlay("⏰ Hết giờ!", "(0 điểm)", Brushes.OrangeRed);
                isWaitingResult = true;
                btnNext.Visibility = Visibility.Visible;
            }
        }

        private void Answer_Checked(object sender, RoutedEventArgs e)
        {
            if (isWaitingResult || string.IsNullOrEmpty(correctAnswer)) return;

            timer.Stop();
            string selected = rdoA.IsChecked == true ? "A" :
                              rdoB.IsChecked == true ? "B" :
                              rdoC.IsChecked == true ? "C" :
                              rdoD.IsChecked == true ? "D" : "";

            bool isCorrect = selected.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase);

            txtResult.Text = isCorrect ? "✅ Chính xác!" : $"❌ Sai! Đáp án đúng là: {correctAnswer}";
            txtResult.Foreground = isCorrect ? Brushes.Green : Brushes.Red;

            SendAnswer(selected, isCorrect);
            ShowResultOverlay(isCorrect ? "🎉 Đúng rồi!" : "😢 Sai mất rồi!", isCorrect ? "(+10 điểm)" : "(0 điểm)", isCorrect ? Brushes.Green : Brushes.Red);
            isWaitingResult = true;
            btnNext.Visibility = Visibility.Visible;
        }

        private void BtnTraLoi_Click(object sender, RoutedEventArgs e)
        {
            if (rdoA.IsChecked == true || rdoB.IsChecked == true || rdoC.IsChecked == true || rdoD.IsChecked == true)
            {
                Answer_Checked(null, null);
            }
            else
            {
                MessageBox.Show("❗ Bạn chưa chọn đáp án nào!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            BtnTiepTuc_Click(sender, e);
        }

        private void BtnTiepTuc_Click(object sender, RoutedEventArgs e)
        {
            gridOverlay.Visibility = Visibility.Collapsed;
            txtResult.Text = "";

            if (questionQueue.Count > 0)
            {
                ProcessQuestion(questionQueue.Dequeue());
                isWaitingResult = false;
                isNextPending = false;
            }
            else
            {
                isWaitingResult = false;
                isNextPending = true;
                txtQuestion.Text = "⏳ Đang chờ câu hỏi mới từ server...";
                SendNextCommand();
            }
            btnNext.Visibility = Visibility.Collapsed;
        }

        private void SendAnswer(string answer, bool isCorrect)
        {
            try
            {
                if (client.Connected)
                    writer.WriteLine($"ANSWER|{playerName}|{answer}|{isCorrect}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi gửi kết quả: " + ex.Message);
            }
        }

        private void EndQuiz(string resultData)
        {
            timer.Stop();
            txtQuestion.Text = "🏁 Quiz kết thúc!";
            HideQuizControls();
            btnThoat.Visibility = Visibility.Visible;
            txtOverlayResult.Text = "📊 Kết quả chung:";
            txtOverlayResult.Foreground = Brushes.DarkBlue;

            if (!string.IsNullOrEmpty(resultData))
            {
                var scores = new List<(string, int)>();
                foreach (var part in resultData.Split(','))
                {
                    var fields = part.Split(':');
                    if (fields.Length == 2 && int.TryParse(fields[1], out int sc))
                        scores.Add((fields[0], sc));
                }

                scores.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                txtOverlayScore.Text = string.Join("\n", scores.ConvertAll(p => $"👤 {p.Item1}: {p.Item2}"));
                rankings.Clear();
                rankings.AddRange(scores);
            }
            else
            {
                txtOverlayScore.Text = "Không có dữ liệu kết quả.";
            }

            btnXemKetQua.Visibility = Visibility.Visible;
        }

        private void BtnKetQua_Click(object sender, RoutedEventArgs e)
        {
            if (rankings.Count == 0)
            {
                MessageBox.Show("⏳ Chưa có dữ liệu xếp hạng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            txtOverlayResult.Text = "📊 Bảng xếp hạng:";
            txtOverlayResult.Foreground = Brushes.DarkBlue;
            txtOverlayScore.Text = string.Join("\n", rankings.ConvertAll(p => $"👤 {p.Item1}: {p.Item2} điểm"));
            txtOverlayScore.Foreground = Brushes.Black;
            gridOverlay.Visibility = Visibility.Visible;
        }

        private void HideQuizControls()
        {
            rdoA.Visibility = rdoB.Visibility = rdoC.Visibility = rdoD.Visibility = Visibility.Collapsed;
            btnTraLoi.Visibility = Visibility.Collapsed;
            btnNext.Visibility = Visibility.Collapsed;
        }

        private void ShowResultOverlay(string message, string scoreText, Brush color)
        {
            txtOverlayResult.Text = message;
            txtOverlayResult.Foreground = color;
            txtOverlayScore.Text = scoreText;
            txtOverlayScore.Foreground = color;
            gridOverlay.Visibility = Visibility.Visible;
        }

        private void SendNextCommand()
        {
            try
            {
                writer?.WriteLine("NEXT");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi gửi lệnh NEXT: " + ex.Message);
            }
        }

        private void ShowMessageAndClose(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message);
                Close();
            });
        }

        private void StartQuizUI()
        {
            txtQuestion.Text = "⏳ Đang chờ câu hỏi từ server...";
            rdoA.Visibility = rdoB.Visibility = rdoC.Visibility = rdoD.Visibility = Visibility.Visible;
            rdoA.IsChecked = rdoB.IsChecked = rdoC.IsChecked = rdoD.IsChecked = false;
            txtResult.Text = "";
            txtTime.Text = "";
            isWaitingResult = false;
            isNextPending = false;
            questionQueue.Clear();
            gridOverlay.Visibility = Visibility.Collapsed;

            PlayFadeIn(txtQuestion);
            PlayFadeIn(rdoA);
            PlayFadeIn(rdoB);
            PlayFadeIn(rdoC);
            PlayFadeIn(rdoD);
        }

        private async void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await writer.WriteLineAsync("QUIT");
                cts?.Cancel();
                reader?.Close();
                writer?.Close();
                client?.Close();
            }
            catch { }
            finally
            {
                Application.Current.Shutdown();
            }
        }

        private void PlayFadeIn(UIElement element)
        {
            if (this.Resources["FadeInStoryboard"] is Storyboard storyboard)
            {
                Storyboard clone = storyboard.Clone();
                Storyboard.SetTarget(clone, element);
                clone.Begin();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            cts?.Cancel();
            timer?.Stop();
            base.OnClosed(e);
        }
    }
}