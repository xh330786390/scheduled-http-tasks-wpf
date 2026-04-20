
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;

namespace ScheduledHttpTasks
{
    public static class TaskRepository
    {
        public static List<ScheduledTask> GetAllTasks()
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    return connection.Query<ScheduledTask>("SELECT * FROM Tasks ORDER BY Id").AsList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取任务列表失败: {ex.Message}");
                return new List<ScheduledTask>();
            }
        }

        public static ScheduledTask GetTaskById(int id)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    return connection.QuerySingleOrDefault<ScheduledTask>(
                        "SELECT * FROM Tasks WHERE Id = @Id", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取任务失败: {ex.Message}");
                return null;
            }
        }

        public static int SaveTask(ScheduledTask task)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    var sql = @"
                        INSERT INTO Tasks (Name, Url, Method, Headers, Body, CronExpression, Status)
                        VALUES (@Name, @Url, @Method, @Headers, @Body, @CronExpression, @Status);
                        SELECT last_insert_rowid();";
                    
                    var id = connection.ExecuteScalar<int>(sql, task);
                    task.Id = id; // 设置任务的ID
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存任务失败: {ex.Message}");
                return -1;
            }
        }

        public static bool UpdateTask(ScheduledTask task)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    var sql = @"
                        UPDATE Tasks 
                        SET Name = @Name, Url = @Url, Method = @Method, Headers = @Headers, Body = @Body,
                            CronExpression = @CronExpression, Status = @Status, UpdatedAt = CURRENT_TIMESTAMP
                        WHERE Id = @Id";
                    
                    return connection.Execute(sql, task) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新任务失败: {ex.Message}");
                return false;
            }
        }

        public static bool UpdateTaskStatus(int taskId, string status)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    var sql = @"
                        UPDATE Tasks 
                        SET Status = @Status, UpdatedAt = CURRENT_TIMESTAMP
                        WHERE Id = @TaskId";
                    
                    return connection.Execute(sql, new { TaskId = taskId, Status = status }) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新任务状态失败: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteTask(int id)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    // 删除任务执行日志
                    connection.Execute("DELETE FROM TaskLogs WHERE TaskId = @Id", new { Id = id });
                    
                    // 删除任务
                    return connection.Execute("DELETE FROM Tasks WHERE Id = @Id", new { Id = id }) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除任务失败: {ex.Message}");
                return false;
            }
        }

        public static bool SaveTaskLog(int taskId, int? statusCode, string responseText, int responseTime, string errorMessage)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    // 获取任务名称
                    var task = GetTaskById(taskId);
                    var taskName = task?.Name ?? "测试任务";
                    
                    // 获取当前北京时间（使用本地时间）
                    var beijingTime = DateTime.Now;
                    
                    // 先尝试使用包含TaskName和ExecutionTime的SQL语句
                    try
                    {
                        var sql = @"
                            INSERT INTO TaskLogs (TaskId, TaskName, StatusCode, ResponseText, ResponseTime, ErrorMessage, ExecutionTime)
                            VALUES (@TaskId, @TaskName, @StatusCode, @ResponseText, @ResponseTime, @ErrorMessage, @ExecutionTime)";
                        
                        var result = connection.Execute(sql, new 
                        { 
                            TaskId = taskId, 
                            TaskName = taskName,
                            StatusCode = statusCode, 
                            ResponseText = responseText, 
                            ResponseTime = responseTime, 
                            ErrorMessage = errorMessage,
                            ExecutionTime = beijingTime
                        });
                        
                        Console.WriteLine($"任务日志保存成功: 任务ID={taskId}, 任务名称={taskName}, 状态码={statusCode}, 响应时间={responseTime}ms, 执行时间={beijingTime:yyyy-MM-dd HH:mm:ss}");
                        return result > 0;
                    }
                    catch (Exception sqlEx)
                    {
                        Console.WriteLine($"包含TaskName和ExecutionTime的SQL失败: {sqlEx.Message}");
                        
                        // 降级到简化SQL（使用数据库默认时间）
                        var sql = @"
                            INSERT INTO TaskLogs (TaskId, StatusCode, ResponseText, ResponseTime, ErrorMessage)
                            VALUES (@TaskId, @StatusCode, @ResponseText, @ResponseTime, @ErrorMessage)";
                        
                        var result = connection.Execute(sql, new 
                        { 
                            TaskId = taskId, 
                            StatusCode = statusCode, 
                            ResponseText = responseText, 
                            ResponseTime = responseTime, 
                            ErrorMessage = errorMessage 
                        });
                        
                        Console.WriteLine($"使用简化SQL保存日志成功");
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存任务日志失败: {ex.Message}");
                Console.WriteLine($"任务ID: {taskId}, 状态码: {statusCode}, 错误: {errorMessage}");
                
                // 尝试使用简化版本（不包含TaskName和ExecutionTime）
                try
                {
                    using (var connection = DatabaseInitializer.GetConnection())
                    {
                        connection.Open();
                        var sql = @"
                            INSERT INTO TaskLogs (TaskId, StatusCode, ResponseText, ResponseTime, ErrorMessage)
                            VALUES (@TaskId, @StatusCode, @ResponseText, @ResponseTime, @ErrorMessage)";
                        
                        var result = connection.Execute(sql, new 
                        { 
                            TaskId = taskId, 
                            StatusCode = statusCode, 
                            ResponseText = responseText, 
                            ResponseTime = responseTime, 
                            ErrorMessage = errorMessage 
                        });
                        
                        Console.WriteLine($"使用简化SQL保存日志成功");
                        return result > 0;
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"简化SQL也失败: {ex2.Message}");
                    return false;
                }
            }
        }

        public static List<dynamic> GetTaskLogs(int taskId)
        {
            try
            {
                using (var connection = DatabaseInitializer.GetConnection())
                {
                    connection.Open();
                    
                    if (taskId > 0)
                    {
                        // 获取指定任务的日志
                        return connection.Query(@"
                            SELECT * FROM TaskLogs 
                            WHERE TaskId = @TaskId 
                            ORDER BY ExecutionTime DESC", 
                            new { TaskId = taskId }).AsList();
                    }
                    else
                    {
                        // 获取所有任务的日志
                        return connection.Query(@"
                            SELECT * FROM TaskLogs 
                            ORDER BY ExecutionTime DESC").AsList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取任务日志失败: {ex.Message}");
                return new List<dynamic>();
            }
        }
    }
}
