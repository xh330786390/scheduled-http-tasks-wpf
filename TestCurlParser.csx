
#r "System.Drawing.Common"
#r "System.Windows.Forms"
#r "System.Xml.Linq"

// 引用当前项目的编译后的 DLL 文件
#r "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks\bin\Release\net8.0-windows\ScheduledHttpTasks.dll"
#r "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks\bin\Release\net8.0-windows\Newtonsoft.Json.dll"
#r "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks\bin\Release\net8.0-windows\Quartz.dll"

using System;
using ScheduledHttpTasks;
using Newtonsoft.Json;

try
{
    Console.WriteLine("=== 测试 CURL 解析功能 ===");
    
    // 创建任务对话框实例（用于测试解析功能）
    var task = new ScheduledTask();
    var dialog = new TaskDialog(task);
    
    // 测试解析您提供的 CURL 命令
    string curlCommand = @"curl -X POST 'https://api.coze.cn/v1/workflow/run' -H 'Authorization: Bearer sat_7T62ZNmq7ZnXgVnUSWzAYaP8GrudVLuO2lI6NTkRoZs73e9syGibSK7jYnrzc9Gm' -H 'Content-Type: application/json' -d '{ ""workflow_id"": ""7629385548395167784"" }'";
    
    dialog.CurlText = curlCommand;
    dialog.ParseCurl();
    
    Console.WriteLine($"\n解析结果:");
    Console.WriteLine($"URL: {task.Url}");
    Console.WriteLine($"方法: {task.Method}");
    Console.WriteLine($"请求头: {task.Headers}");
    Console.WriteLine($"请求体: {task.Body}");
    
    Console.WriteLine("\n=== 测试 Cron 表达式计算功能 ===");
    
    // 测试 Cron 表达式计算
    task.CronExpression = "0 0/10 * * * ?"; // 每 10 分钟执行一次
    
    Console.WriteLine($"Cron 表达式: {task.CronExpression}");
    Console.WriteLine($"下次执行时间: {task.NextFireTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "无效表达式"}");
    
    Console.WriteLine("\n=== 功能测试成功 ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n错误: {ex}");
    Console.WriteLine($"\n堆栈跟踪: {ex.StackTrace}");
}
