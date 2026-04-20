
using System;
using System.IO;
using System.Data.SQLite;

namespace ScheduledHttpTasks
{
    public static class DatabaseInitializer
    {
        private static readonly string DatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.db");

        public static void Initialize()
        {
            try
            {
                // 检查并创建数据库文件
                if (!File.Exists(DatabasePath))
                {
                    SQLiteConnection.CreateFile(DatabasePath);
                    Console.WriteLine($"数据库文件已创建: {DatabasePath}");
                }

                // 创建表
                using (var connection = GetConnection())
                {
                    connection.Open();
                    
                    // 创建任务表
                    var createTableSql = @"
                        CREATE TABLE IF NOT EXISTS Tasks (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Url TEXT NOT NULL,
                            Method TEXT NOT NULL DEFAULT 'POST',
                            Headers TEXT,
                            Body TEXT,
                            CronExpression TEXT NOT NULL,
                            Status TEXT NOT NULL DEFAULT '已启动',
                            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";
                    
                    using (var command = new SQLiteCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // 创建任务执行日志表
                    var createLogTableSql = @"
                        CREATE TABLE IF NOT EXISTS TaskLogs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            TaskId INTEGER NOT NULL,
                            TaskName TEXT NOT NULL,
                            StatusCode INTEGER,
                            ResponseText TEXT,
                            ResponseTime INTEGER,
                            ErrorMessage TEXT,
                            ExecutionTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (TaskId) REFERENCES Tasks(Id)
                        )";
                    
                    using (var command = new SQLiteCommand(createLogTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // 检查并添加缺失的TaskName列（兼容旧版本数据库）
                    try
                    {
                        var checkColumnSql = "SELECT COUNT(*) FROM TaskLogs LIMIT 1";
                        using (var command = new SQLiteCommand(checkColumnSql, connection))
                        {
                            command.ExecuteScalar();
                        }
                        
                        // 如果表存在，检查是否需要添加TaskName列
                        var alterTableSql = "ALTER TABLE TaskLogs ADD COLUMN TaskName TEXT NOT NULL DEFAULT '未知任务'";
                        using (var command = new SQLiteCommand(alterTableSql, connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("已添加TaskName列到TaskLogs表");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果表不存在或列已存在，忽略错误
                        Console.WriteLine($"数据库表检查: {ex.Message}");
                    }

                    // 检查并添加缺失的Status列到Tasks表
                    try
                    {
                        var checkStatusSql = "SELECT Status FROM Tasks LIMIT 1";
                        using (var command = new SQLiteCommand(checkStatusSql, connection))
                        {
                            command.ExecuteScalar();
                        }
                    }
                    catch (Exception)
                    {
                        // Status列不存在，需要添加
                        var alterTasksSql = "ALTER TABLE Tasks ADD COLUMN Status TEXT NOT NULL DEFAULT '已启动'";
                        using (var command = new SQLiteCommand(alterTasksSql, connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("已添加Status列到Tasks表");
                            
                            // 更新现有任务的默认状态
                            var updateStatusSql = "UPDATE Tasks SET Status = '已启动' WHERE Status IS NULL OR Status = ''";
                            using (var updateCommand = new SQLiteCommand(updateStatusSql, connection))
                            {
                                updateCommand.ExecuteNonQuery();
                                Console.WriteLine("已更新现有任务的状态");
                            }
                        }
                    }

                    Console.WriteLine("数据库初始化完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
            }
        }

        internal static SQLiteConnection GetConnection()
        {
            var connectionString = $"Data Source={DatabasePath};Version=3;";
            return new SQLiteConnection(connectionString);
        }
    }
}
