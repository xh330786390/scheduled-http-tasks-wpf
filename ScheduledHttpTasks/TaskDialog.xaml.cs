using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace ScheduledHttpTasks
{
    public partial class TaskDialog : Window, INotifyPropertyChanged
    {
        public ScheduledTask Task { get; set; }

        private string _curlText;
        public string CurlText
        {
            get => _curlText;
            set => SetProperty(ref _curlText, value);
        }

        public System.Windows.Input.ICommand ParseCurlCommand { get; }

        public TaskDialog(ScheduledTask task)
        {
            InitializeComponent();
            Task = task;
            DataContext = this; // 将数据上下文设置为 TaskDialog 自身

            ParseCurlCommand = new RelayCommand(ParseCurl);
        }

        private void ParseCurl()
        {
            try
            {
                var curlData = ParseCurlInternal(CurlText);
                
                // 更新任务属性，属性设置器会自动触发变更通知
                Task.Url = curlData.Url;
                Task.Method = curlData.Method;
                Task.Headers = JsonConvert.SerializeObject(curlData.Headers, Formatting.Indented);
                Task.Body = curlData.Body;

                // 通过重新赋值来强制触发属性变更通知
                Task.Url = Task.Url;
                Task.Method = Task.Method;
                Task.Headers = Task.Headers;
                Task.Body = Task.Body;

                MessageBox.Show($"CURL 解析成功！URL: {curlData.Url}, 方法: {curlData.Method}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Url, string Method, dynamic Headers, string Body) ParseCurlInternal(string curl)
        {
            // 清理 curl 命令：移除反斜杠换行符，合并为单行
            var cleaned = curl.Trim()
                .Replace("\\\r\n", " ")  // Windows 换行符
                .Replace("\\\n", " ")   // Unix 换行符
                .Replace("\\\r", " ")   // Mac 换行符
                .Replace("\r\n", " ")   // 普通换行符
                .Replace("\n", " ")
                .Replace("\r", " ");
            
            if (!cleaned.StartsWith("curl "))
                throw new ArgumentException("不是有效的 CURL 命令");

            string url = null;
            string method = "GET";
            var headers = new System.Dynamic.ExpandoObject() as System.Collections.Generic.IDictionary<string, object>;
            string body = "{}";

            // 移除 curl 前缀
            var curlCommand = cleaned.Substring(5).Trim();

            // 解析方法 (-X POST)
            var methodMatch = Regex.Match(curlCommand, @"-X\s+(\S+)");
            if (methodMatch.Success)
            {
                method = methodMatch.Groups[1].Value.ToUpper();
                curlCommand = curlCommand.Replace(methodMatch.Value, "").Trim();
            }

            // 解析请求头 (-H "Header: Value")
            var headerMatches = Regex.Matches(curlCommand, @"-H\s+'([^']+)'|-H\s+""([^""]+)""|-H\s+(\S+)");
            foreach (Match match in headerMatches)
            {
                string header;
                if (match.Groups[1].Success)
                    header = match.Groups[1].Value;
                else if (match.Groups[2].Success)
                    header = match.Groups[2].Value;
                else
                    header = match.Groups[3].Value;

                if (!string.IsNullOrEmpty(header))
                {
                    var parts = header.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        headers[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                curlCommand = curlCommand.Replace(match.Value, "").Trim();
            }

            // 解析请求体 (-d '{"key":"value"}')
            var bodyMatch = Regex.Match(curlCommand, @"-d\s+'([^']+)'|-d\s+""([^""]+)""|-d\s+([^\s]+)");
            if (bodyMatch.Success)
            {
                string bodyContent;
                if (bodyMatch.Groups[1].Success)
                    bodyContent = bodyMatch.Groups[1].Value;
                else if (bodyMatch.Groups[2].Success)
                    bodyContent = bodyMatch.Groups[2].Value;
                else
                    bodyContent = bodyMatch.Groups[3].Value;

                try
                {
                    // 尝试解析为 JSON 并重新格式化
                    dynamic jsonBody = JsonConvert.DeserializeObject(bodyContent);
                    body = JsonConvert.SerializeObject(jsonBody, Formatting.Indented);
                }
                catch
                {
                    // 如果不是有效的 JSON，直接使用原内容
                    body = bodyContent;
                }
                curlCommand = curlCommand.Replace(bodyMatch.Value, "").Trim();
            }

            // 解析 URL - 使用更精确的匹配
            // 先尝试匹配单引号或双引号包围的 URL
            var urlMatch = Regex.Match(curlCommand, @"'([^']+)'|""([^""]+)""");
            if (urlMatch.Success)
            {
                if (urlMatch.Groups[1].Success)
                    url = urlMatch.Groups[1].Value;
                else if (urlMatch.Groups[2].Success)
                    url = urlMatch.Groups[2].Value;
            }
            else
            {
                // 如果没有引号包围，尝试匹配 HTTP/HTTPS 开头的 URL
                var httpMatch = Regex.Match(curlCommand, @"(https?://[^\s]+)");
                if (httpMatch.Success)
                {
                    url = httpMatch.Groups[1].Value;
                }
                else
                {
                    // 最后尝试将剩余内容作为 URL
                    var remaining = curlCommand.Trim();
                    if (!string.IsNullOrEmpty(remaining) && !remaining.Contains("-"))
                    {
                        url = remaining;
                    }
                }
            }

            // 清理 URL 中的多余字符和符号
            if (!string.IsNullOrEmpty(url))
            {
                // 移除所有引号、空格、反引号和其他特殊符号
                url = url.Trim('\'', '"', ' ', '`', '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '=', '+', '[', ']', '{', '}', '|', ';', ':', ',', '.', '<', '>', '/', '?');
                
                // 移除 URL 开头和结尾的特殊符号（更严格的清理）
                url = Regex.Replace(url, @"^[^a-zA-Z0-9:/]+|[^a-zA-Z0-9/]+$", "");
                
                // 确保 URL 以 http:// 或 https:// 开头
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    // 如果 URL 不包含协议，尝试从原始命令中提取
                    var protocolMatch = Regex.Match(curlCommand, @"(https?://)");
                    if (protocolMatch.Success)
                    {
                        url = protocolMatch.Groups[1].Value + url;
                    }
                }
            }

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("无法解析 URL");

            return (url, method, headers, body);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 检查任务名是否为空
            if (string.IsNullOrWhiteSpace(Task.Name))
            {
                MessageBox.Show("任务名称不能为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 检查任务名是否重复
            if (IsTaskNameExists(Task.Name, Task.Id))
            {
                MessageBox.Show("任务名已存在，请重新命名！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }

        private bool IsTaskNameExists(string taskName, int? currentTaskId = null)
        {
            try
            {
                // 获取所有任务
                var allTasks = TaskRepository.GetAllTasks();
                
                // 检查是否存在同名任务（排除当前编辑的任务）
                return allTasks.Any(t => 
                    t.Name.Equals(taskName.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    t.Id != currentTaskId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查任务名是否存在时出错: {ex.Message}");
                // 如果检查失败，默认允许保存（避免阻止用户操作）
                return false;
            }
        }

        private async void SendTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(Task.Url))
                {
                    TestResultTextBox.Text = "错误: API URL不能为空";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Task.Method))
                {
                    TestResultTextBox.Text = "错误: 请求方法不能为空";
                    return;
                }

                // 显示正在测试的信息
                TestResultTextBox.Text = "正在发送测试请求...";

                // 创建临时任务对象用于测试
                var testTask = new ScheduledTask
                {
                    Name = Task.Name + " (测试)",
                    Url = Task.Url,
                    Method = Task.Method,
                    Headers = Task.Headers,
                    Body = Task.Body
                };

                // 调用API测试
                var result = await ApiCaller.CallApiAsync(testTask);
                
                // 显示成功结果
                TestResultTextBox.Text = $"✅ 测试成功！\n{result}";
            }
            catch (Exception ex)
            {
                // 显示错误信息
                TestResultTextBox.Text = $"❌ 测试失败！\n错误信息: {ex.Message}";
            }
        }

        private void SetTime_Click(object sender, RoutedEventArgs e)
        {
            // 创建时间选择对话框
            var timeDialog = new Window
            {
                Title = "设置执行间隔",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 间隔数值（放在上面）
            var intervalLabel = new Label { Content = "间隔数值:", Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(intervalLabel, 0);
            var intervalTextBox = new TextBox { Margin = new Thickness(80, 10, 0, 0), Width = 120, Text = "10" };
            Grid.SetRow(intervalTextBox, 0);

            // 间隔时间单位选择（放在下面）
            var unitLabel = new Label { Content = "间隔单位:", Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(unitLabel, 1);
            var unitComboBox = new ComboBox { Margin = new Thickness(80, 10, 0, 0), Width = 120 };
            Grid.SetRow(unitComboBox, 1);
            unitComboBox.Items.Add("秒");
            unitComboBox.Items.Add("分钟");
            unitComboBox.Items.Add("小时");
            unitComboBox.Items.Add("日");
            unitComboBox.Items.Add("周");
            unitComboBox.SelectedIndex = 0;

            // 说明文本
            var descriptionLabel = new Label { Content = "例如：间隔10秒=每10秒执行，间隔2天=每2天执行", Margin = new Thickness(0, 5, 0, 0), FontSize = 10, Foreground = Brushes.Gray };
            Grid.SetRow(descriptionLabel, 2);

            // 按钮
            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(buttonStack, 3);
            var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "取消", Width = 80 };
            buttonStack.Children.Add(okButton);
            buttonStack.Children.Add(cancelButton);

            grid.Children.Add(unitLabel);
            grid.Children.Add(unitComboBox);
            grid.Children.Add(intervalLabel);
            grid.Children.Add(intervalTextBox);
            grid.Children.Add(descriptionLabel);
            grid.Children.Add(buttonStack);
            timeDialog.Content = grid;

            okButton.Click += (s, args) =>
            {
                if (!int.TryParse(intervalTextBox.Text, out int interval) || interval <= 0)
                {
                    MessageBox.Show("请输入有效的间隔数值（大于0的整数）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string unit = unitComboBox.SelectedItem?.ToString() ?? "秒";
                
                // 根据单位生成Cron表达式
                switch (unit)
                {
                    case "秒":
                        // 每N秒执行：0/N * * * * ?
                        Task.CronExpression = $"0/{interval} * * * * ?";
                        break;
                    case "分钟":
                        // 每N分钟执行：0 */N * * * ?
                        Task.CronExpression = $"0 */{interval} * * * ?";
                        break;
                    case "小时":
                        // 每N小时执行：0 0 */N * * ?
                        Task.CronExpression = $"0 0 */{interval} * * ?";
                        break;
                    case "日":
                        // 每N天执行：0 0 0 */N * ?
                        Task.CronExpression = $"0 0 0 */{interval} * ?";
                        break;
                    case "周":
                        // 每N周执行：0 0 0 ? * {interval}/7
                        // 注意：周的间隔需要特殊处理，因为Cron不支持直接的周间隔
                        if (interval == 1)
                        {
                            // 每周执行：0 0 0 ? * 1 (周日)
                            Task.CronExpression = $"0 0 0 ? * 1";
                        }
                        else
                        {
                            // 对于多周间隔，使用天的倍数
                            int days = interval * 7;
                            Task.CronExpression = $"0 0 0 */{days} * ?";
                        }
                        break;
                    default:
                        Task.CronExpression = $"0/{interval} * * * * ?";
                        break;
                }
                
                Console.WriteLine($"生成的Cron表达式: {Task.CronExpression} (间隔{interval}{unit})");
                timeDialog.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                timeDialog.Close();
            };

            timeDialog.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}