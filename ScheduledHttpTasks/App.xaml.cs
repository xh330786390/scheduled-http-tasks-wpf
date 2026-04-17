
using System;
using System.Windows;

namespace ScheduledHttpTasks
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化数据库
            DatabaseInitializer.Initialize();
            
            // 异步初始化定时任务调度器
            try
            {
                await TaskScheduler.Initialize();
                Console.WriteLine("调度器初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度器初始化失败: {ex.Message}");
                MessageBox.Show($"调度器初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // 异步关闭定时任务调度器
            try
            {
                await TaskScheduler.Shutdown();
                Console.WriteLine("调度器已关闭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度器关闭失败: {ex.Message}");
            }
            base.OnExit(e);
        }
    }
}
