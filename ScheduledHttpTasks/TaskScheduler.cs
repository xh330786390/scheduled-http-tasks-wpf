
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace ScheduledHttpTasks
{
    public static class TaskScheduler
    {
        private static IScheduler _scheduler;
        private static readonly Dictionary<int, IJobDetail> _runningJobs = new();
        private static readonly Dictionary<int, ITrigger> _runningTriggers = new();
        
        // 日志更新事件
        public static event Action<int> TaskExecuted;
        
        // 触发任务执行事件的方法
        public static void OnTaskExecuted(int taskId)
        {
            TaskExecuted?.Invoke(taskId);
        }

        public static async Task Initialize()
        {
            try
            {
                Console.WriteLine("开始初始化调度器...");
                
                // 配置调度器
                var properties = new System.Collections.Specialized.NameValueCollection
                {
                    ["quartz.scheduler.instanceName"] = "ScheduledHttpTasksScheduler",
                    ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
                    ["quartz.threadPool.threadCount"] = "10"
                };

                var schedulerFactory = new StdSchedulerFactory(properties);
                _scheduler = await schedulerFactory.GetScheduler();
                Console.WriteLine("调度器实例创建成功");
                
                await _scheduler.Start();
                Console.WriteLine("调度器已启动");
                Console.WriteLine($"调度器状态: {_scheduler.IsStarted}");

                // 添加一个测试任务来验证调度器工作
                Console.WriteLine("添加测试任务验证调度器...");
                var testTask = new ScheduledTask
                {
                    Id = -1,
                    Name = "调度器测试任务",
                    CronExpression = "0/10 * * * * ?", // 每10秒执行一次
                    Url = "http://localhost/test",
                    Method = "GET"
                };
                
                try
                {
                    await ScheduleTaskInternal(testTask);
                    Console.WriteLine("测试任务已调度，调度器工作正常");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"测试任务调度失败: {ex.Message}");
                }

                // 自动启动已保存的任务
                var tasks = TaskRepository.GetAllTasks();
                Console.WriteLine($"找到 {tasks.Count} 个任务");
                
                foreach (var task in tasks)
                {
                    try
                    {
                        await ScheduleTaskInternal(task);
                        Console.WriteLine($"任务 {task.Name} 已自动启动");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"启动任务 {task.Name} 失败: {ex.Message}");
                    }
                }
                
                Console.WriteLine("调度器初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度器初始化失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public static async Task Shutdown()
        {
            try
            {
                if (_scheduler != null && _scheduler.IsStarted)
                {
                    await _scheduler.Shutdown();
                    Console.WriteLine("调度器已停止");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度器停止失败: {ex.Message}");
            }
        }

        public static async Task StartTask(ScheduledTask task)
        {
            try
            {
                Console.WriteLine($"开始启动任务: {task.Name}, ID: {task.Id}");
                Console.WriteLine($"Cron表达式: {task.CronExpression}");
                
                // 检查调度器状态，如果未启动则尝试重新初始化
                if (_scheduler == null || !_scheduler.IsStarted)
                {
                    Console.WriteLine("调度器未启动，尝试重新初始化...");
                    await Initialize();
                    Console.WriteLine("调度器重新初始化完成");
                }
                else
                {
                    Console.WriteLine("调度器已启动，状态正常");
                }

                if (_runningJobs.ContainsKey(task.Id))
                {
                    Console.WriteLine($"任务 {task.Name} 已在运行中，先停止再重新启动");
                    await StopTask(task.Id);
                }

                await ScheduleTaskInternal(task);
                Console.WriteLine($"任务 {task.Name} 已启动，Cron表达式: {task.CronExpression}");
                
                // 立即检查任务是否已调度
                try
                {
                    var triggers = await _scheduler.GetTriggersOfJob(new JobKey($"job_{task.Id}", "httpTasksGroup"));
                    if (triggers.Count > 0)
                    {
                        var trigger = triggers.FirstOrDefault();
                        if (trigger != null)
                        {
                            var nextFireTime = trigger.GetNextFireTimeUtc();
                            Console.WriteLine($"任务 {task.Name} 下次执行时间: {nextFireTime?.ToLocalTime()}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"警告：任务 {task.Name} 未找到触发器");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"检查任务调度状态失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动任务 {task.Name} 失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        public static async Task StopTask(int taskId)
        {
            if (_scheduler == null || !_scheduler.IsStarted)
                throw new InvalidOperationException("调度器未启动");

            if (!_runningJobs.ContainsKey(taskId))
                throw new InvalidOperationException("任务未在运行中");

            await _scheduler.UnscheduleJob(new TriggerKey($"trigger_{taskId}", "httpTasksGroup"));
            
            _runningJobs.Remove(taskId);
            _runningTriggers.Remove(taskId);
            
            Console.WriteLine($"任务 {taskId} 已停止");
        }

        public static bool IsTaskRunning(int taskId)
        {
            return _runningJobs.ContainsKey(taskId);
        }

        public static List<ScheduledTask> GetRunningTasks()
        {
            var runningTasks = new List<ScheduledTask>();
            
            foreach (var jobEntry in _runningJobs)
            {
                var taskId = jobEntry.Key;
                var task = TaskRepository.GetTaskById(taskId);
                if (task != null)
                {
                    runningTasks.Add(task);
                }
            }
            
            return runningTasks;
        }

        private static async Task ScheduleTaskInternal(ScheduledTask task)
        {
            try
            {
                Console.WriteLine($"开始调度任务: {task.Name}, Cron: {task.CronExpression}");
                
                // 验证Cron表达式
                try
                {
                    var cronExpression = new CronExpression(task.CronExpression);
                    var nextFireTime = cronExpression.GetNextValidTimeAfter(DateTimeOffset.Now);
                    Console.WriteLine($"Cron表达式验证成功，下次执行时间: {nextFireTime?.ToLocalTime()}");
                }
                catch (Exception cronEx)
                {
                    Console.WriteLine($"Cron表达式验证失败: {cronEx.Message}");
                    throw new Exception($"Cron表达式无效: {task.CronExpression}", cronEx);
                }
                
                // 创建作业
                var job = JobBuilder.Create<HttpTaskJob>()
                    .WithIdentity($"job_{task.Id}", "httpTasksGroup")
                    .UsingJobData("TaskId", task.Id)
                    .Build();

                // 创建触发器
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger_{task.Id}", "httpTasksGroup")
                    .WithCronSchedule(task.CronExpression)
                    .ForJob(job)
                    .Build();

                // 调度作业
                await _scheduler.ScheduleJob(job, trigger);

                // 保存引用
                _runningJobs[task.Id] = job;
                _runningTriggers[task.Id] = trigger;

                Console.WriteLine($"任务 {task.Name} 已调度成功");
                
                // 验证调度结果
                var jobKey = new JobKey($"job_{task.Id}", "httpTasksGroup");
                var exists = await _scheduler.CheckExists(jobKey);
                Console.WriteLine($"任务 {task.Name} 调度验证: {exists}");
                
                // 获取触发器信息
                var triggers = await _scheduler.GetTriggersOfJob(jobKey);
                if (triggers.Count > 0)
                {
                    var nextFireTime = triggers.First().GetNextFireTimeUtc();
                    Console.WriteLine($"任务 {task.Name} 下次执行时间: {nextFireTime?.ToLocalTime()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度任务 {task.Name} 失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }
    }

    // HTTP 任务作业
    public class HttpTaskJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var taskId = 0;
            try
            {
                taskId = context.MergedJobDataMap.GetInt("TaskId");
                var task = TaskRepository.GetTaskById(taskId);
                
                if (task != null)
                {
                    Console.WriteLine($"开始执行任务: {task.Name}, ID: {taskId}");
                    Console.WriteLine($"触发时间: {context.FireTimeUtc.ToLocalTime()}");
                    Console.WriteLine($"调度时间: {context.ScheduledFireTimeUtc?.ToLocalTime()}");
                    
                    var result = await ApiCaller.CallApiAsync(task);
                    Console.WriteLine($"任务执行完成: {task.Name} - {result}");
                    
                    // 更新任务状态为成功
                    UpdateTaskStatus(taskId, "执行成功");
                    
                    // 触发日志更新事件
                    Console.WriteLine($"准备触发任务执行事件: {taskId}");
                    TaskScheduler.OnTaskExecuted(taskId);
                    Console.WriteLine($"任务 {task.Name} 执行事件已触发");
                }
                else
                {
                    Console.WriteLine($"任务不存在: ID={taskId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务执行失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 更新任务状态为失败
                UpdateTaskStatus(taskId, $"执行失败: {ex.Message}");
                
                // 触发日志更新事件
                Console.WriteLine($"准备触发任务失败事件: {taskId}");
                TaskScheduler.OnTaskExecuted(taskId);
                Console.WriteLine($"任务 {taskId} 执行失败事件已触发");
            }
        }
        
        private void UpdateTaskStatus(int taskId, string status)
        {
            try
            {
                // 这里可以添加额外的状态更新逻辑
                Console.WriteLine($"任务 {taskId} 状态更新为: {status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新任务状态失败: {ex.Message}");
            }
        }
    }
}
