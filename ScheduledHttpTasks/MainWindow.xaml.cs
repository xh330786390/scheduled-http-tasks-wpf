
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dapper;

namespace ScheduledHttpTasks
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ScheduledTask> _tasks;
        private ScheduledTask _selectedTask;
        private ObservableCollection<dynamic> _taskLogs;
        private ObservableCollection<dynamic> _taskFilterOptions;
        private int _selectedTaskId;

        public ObservableCollection<ScheduledTask> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        public ScheduledTask SelectedTask
        {
            get => _selectedTask;
            set
            {
                SetProperty(ref _selectedTask, value);
                if (value != null)
                {
                    SelectedTaskId = value.Id;
                    LoadTaskLogs();
                }
            }
        }

        public ObservableCollection<dynamic> TaskLogs
        {
            get => _taskLogs;
            set => SetProperty(ref _taskLogs, value);
        }

        public ObservableCollection<dynamic> TaskFilterOptions
        {
            get => _taskFilterOptions;
            set => SetProperty(ref _taskFilterOptions, value);
        }

        public int SelectedTaskId
        {
            get => _selectedTaskId;
            set
            {
                if (SetProperty(ref _selectedTaskId, value))
                {
                    // 当任务选择改变时，重新加载日志
                    LoadTaskLogs();
                }
            }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand StartTaskCommand { get; }
        public ICommand StopTaskCommand { get; }
        public ICommand TestTaskCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand RefreshLogsCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            AddTaskCommand = new RelayCommand(AddTask);
            EditTaskCommand = new RelayCommand(EditTask, () => SelectedTask != null);
            DeleteTaskCommand = new RelayCommand(DeleteTask, () => SelectedTask != null);
            StartTaskCommand = new RelayCommand(StartTask, () => SelectedTask != null);
            StopTaskCommand = new RelayCommand(StopTask, () => SelectedTask != null);
            TestTaskCommand = new RelayCommand(TestTask, () => SelectedTask != null);
            ViewLogsCommand = new RelayCommand(ViewLogs, () => SelectedTask != null);
            RefreshLogsCommand = new RelayCommand(RefreshLogs);
            ClearLogsCommand = new RelayCommand(ClearLogs);

            // 订阅任务执行事件，自动刷新日志
            TaskScheduler.TaskExecuted += OnTaskExecuted;

            LoadTasks();
            UpdateTaskStatus();
            TaskLogs = new ObservableCollection<dynamic>();
            TaskFilterOptions = new ObservableCollection<dynamic>();
            
            // 订阅任务列表变化事件
            Tasks.CollectionChanged += (s, e) => UpdateTaskFilterOptions();
            
            // 初始化任务筛选选项
            UpdateTaskFilterOptions();
        }

        private void LoadTasks()
        {
            var tasks = TaskRepository.GetAllTasks();
            Tasks = new ObservableCollection<ScheduledTask>(tasks);
            
            // 为每个任务添加属性变更监听
            foreach (var task in Tasks)
            {
                task.PropertyChanged += OnTaskPropertyChanged;
                
                // 确保任务状态正确反映实际运行状态
                task.Status = TaskScheduler.IsTaskRunning(task.Id) ? "运行中" : "已停止";
            }
        }
        
        private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScheduledTask.Name))
            {
                // 任务名称变更时更新下拉列表
                UpdateTaskFilterOptions();
            }
        }

        private void UpdateTaskStatus()
        {
            // 只更新当前选中任务的状态，避免影响其他任务
            if (SelectedTask != null)
            {
                SelectedTask.Status = TaskScheduler.IsTaskRunning(SelectedTask.Id) ? "运行中" : "已停止";
            }
        }

        private void UpdateTaskFilterOptions()
        {
            TaskFilterOptions.Clear();
            
            // 添加"全部"选项
            TaskFilterOptions.Add(new { Id = 0, Name = "全部" });
            
            // 添加所有任务选项
            foreach (var task in Tasks)
            {
                TaskFilterOptions.Add(new { Id = task.Id, Name = task.Name });
            }
            
            // 如果当前没有选中任务，默认选中"全部"
            if (SelectedTaskId == 0 && TaskFilterOptions.Count > 0)
            {
                SelectedTaskId = 0;
                LoadTaskLogs();
            }
        }

        private void AddTask()
        {
            var task = new ScheduledTask
            {
                Name = "新任务",
                Method = "POST",
                Url = "https://api.example.com",
                CronExpression = "0 0 0 * * ?",
                Headers = "{}",
                Body = "{}"
            };

            var dialog = new TaskDialog(task);
            if (dialog.ShowDialog() == true)
            {
                TaskRepository.SaveTask(task);
                LoadTasks();
                UpdateTaskStatus();
            }
        }

        private void EditTask()
        {
            if (SelectedTask == null) return;

            var task = new ScheduledTask
            {
                Id = SelectedTask.Id,
                Name = SelectedTask.Name,
                Method = SelectedTask.Method,
                Url = SelectedTask.Url,
                CronExpression = SelectedTask.CronExpression,
                Headers = SelectedTask.Headers,
                Body = SelectedTask.Body
            };

            var dialog = new TaskDialog(task);
            if (dialog.ShowDialog() == true)
            {
                TaskRepository.UpdateTask(task);
                LoadTasks();
                UpdateTaskStatus();
                // 更新下拉列表选项
                UpdateTaskFilterOptions();
            }
        }

        private void DeleteTask()
        {
            if (SelectedTask == null) return;

            if (MessageBox.Show("确定要删除选中的任务吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                TaskRepository.DeleteTask(SelectedTask.Id);
                LoadTasks();
                UpdateTaskStatus();
            }
        }

        private async void StartTask()
        {
            if (SelectedTask == null) return;

            try
            {
                Console.WriteLine($"开始启动任务: {SelectedTask.Name}");
                await TaskScheduler.StartTask(SelectedTask);
                // 直接设置状态，不再调用UpdateTaskStatus()
                SelectedTask.Status = "运行中";
                Console.WriteLine($"任务 {SelectedTask.Name} 启动成功");
                MessageBox.Show("任务已启动", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动任务失败: {ex.Message}");
                MessageBox.Show($"启动任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopTask()
        {
            if (SelectedTask == null) return;

            try
            {
                TaskScheduler.StopTask(SelectedTask.Id);
                // 直接设置状态，不再调用UpdateTaskStatus()
                SelectedTask.Status = "已停止";
                MessageBox.Show("任务已停止", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestTask()
        {
            if (SelectedTask == null) return;

            try
            {
                // 显示执行开始提示
                MessageBox.Show($"开始执行任务测试: {SelectedTask.Name}", "测试开始", MessageBoxButton.OK, MessageBoxImage.Information);
                
                var result = await ApiCaller.CallApiAsync(SelectedTask);
                
                // 显示详细测试结果
                MessageBox.Show($"测试完成！\n任务: {SelectedTask.Name}\n结果: {result}", "测试完成", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 测试完成后立即刷新日志
                LoadTaskLogs();
                
                // 显示日志刷新提示
                MessageBox.Show($"执行日志已刷新，请查看底部日志区域", "日志已更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 即使失败也要刷新日志
                LoadTaskLogs();
            }
        }

        private void ViewLogs()
        {
            if (SelectedTask == null) return;
            
            // 确保日志区域可见
            LoadTaskLogs();
            MessageBox.Show($"正在显示任务 '{SelectedTask.Name}' 的执行日志", "查看日志", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyTask_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ScheduledTask originalTask)
            {
                // 复制任务数据
                var copiedTask = new ScheduledTask
                {
                    Name = GenerateCopyTaskName(originalTask.Name),
                    Url = originalTask.Url,
                    Method = originalTask.Method,
                    Headers = originalTask.Headers,
                    Body = originalTask.Body,
                    CronExpression = originalTask.CronExpression,
                    Status = "停止"
                };

                // 检查任务名是否重复
                if (IsTaskNameExists(copiedTask.Name))
                {
                    MessageBox.Show("任务名已存在，请重新命名", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 打开任务配置对话框
                var dialog = new TaskDialog(copiedTask);
                if (dialog.ShowDialog() == true)
                {
                    // 保存复制的任务
                    TaskRepository.SaveTask(dialog.Task);
                    
                    // 刷新任务列表
                    LoadTasks();
                    
                    MessageBox.Show("任务创建成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private string GenerateCopyTaskName(string originalName)
        {
            var baseName = originalName + "_副本";
            var newName = baseName;
            var counter = 1;

            // 检查名称是否已存在，如果存在则添加数字后缀
            while (IsTaskNameExists(newName))
            {
                newName = $"{baseName}{counter}";
                counter++;
            }

            return newName;
        }

        private bool IsTaskNameExists(string taskName)
        {
            return Tasks.Any(t => t.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadTaskLogs()
        {
            try
            {
                // 默认显示所有任务的日志（SelectedTaskId为0时显示全部）
                var logs = TaskRepository.GetTaskLogs(SelectedTaskId);
                TaskLogs = new ObservableCollection<dynamic>(logs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载任务日志失败: {ex.Message}");
                TaskLogs = new ObservableCollection<dynamic>();
            }
        }

        private void RefreshLogs()
        {
            LoadTaskLogs();
            // 移除弹框提示，直接刷新日志
        }

        private void ClearLogs()
        {
            if (SelectedTaskId <= 0) return;

            var result = MessageBox.Show("确定要清空当前任务的执行日志吗？", "确认清空", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 清空指定任务的日志
                    using (var connection = DatabaseInitializer.GetConnection())
                    {
                        connection.Open();
                        connection.Execute("DELETE FROM TaskLogs WHERE TaskId = @TaskId", new { TaskId = SelectedTaskId });
                    }
                    
                    LoadTaskLogs();
                    MessageBox.Show("执行日志已清空", "清空完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清空日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnTaskExecuted(int taskId)
        {
            // 在UI线程中刷新日志
            Dispatcher.Invoke(() =>
            {
                // 总是刷新日志，无论当前选中哪个任务
                LoadTaskLogs();
                Console.WriteLine($"任务 {taskId} 执行完成，日志已自动刷新");
                
                // 如果当前选中了其他任务，显示提示信息
                if (SelectedTaskId != taskId && SelectedTaskId > 0)
                {
                    var currentTask = Tasks.FirstOrDefault(t => t.Id == SelectedTaskId);
                    var executedTask = Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (currentTask != null && executedTask != null)
                    {
                        Console.WriteLine($"当前选中任务: {currentTask.Name}, 执行任务: {executedTask.Name}");
                    }
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            TaskScheduler.TaskExecuted -= OnTaskExecuted;
            base.OnClosed(e);
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
