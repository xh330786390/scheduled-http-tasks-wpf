
# 定时任务调度器 - WPF 应用

一个基于 WPF 的定时任务调度器，用于定期调用 HTTP API 并将数据保存到本地 SQLite 数据库中。

## 功能特点

- ✅ **定时任务调度**：支持使用 Cron 表达式设置任意时间的任务
- ✅ **HTTP API 调用**：支持 GET、POST、PUT、DELETE、PATCH 等方法
- ✅ **CURL 解析**：可以解析 CURL 命令并转换为 HTTP 请求
- ✅ **任务管理**：添加、编辑、删除、启动、停止任务
- ✅ **执行日志**：详细记录每个任务的执行结果
- ✅ **本地存储**：使用 SQLite 数据库存储任务和执行日志
- ✅ **任务测试**：支持手动测试任务执行
- ✅ **实时状态**：显示任务状态和下次执行时间

## 技术架构

- **框架**：.NET 8.0 WPF
- **数据库**：SQLite（System.Data.SQLite）
- **调度器**：Quartz.NET（企业级调度框架）
- **HTTP 客户端**：System.Net.Http
- **ORM**：Dapper（轻量级 ORM）
- **JSON**：Newtonsoft.Json（Json.NET）

## 项目结构

```
scheduled-http-tasks-wpf/
├── ScheduledHttpTasks/
│   ├── Assets/              # 资源文件
│   │   └── app.ico         # 应用程序图标
│   ├── Properties/         # 项目属性
│   ├── App.xaml           # 应用程序入口
│   ├── App.xaml.cs        # 应用程序逻辑
│   ├── MainWindow.xaml    # 主窗口
│   ├── MainWindow.xaml.cs # 主窗口逻辑
│   ├── TaskDialog.xaml    # 任务对话框
│   ├── TaskDialog.xaml.cs # 任务对话框逻辑
│   ├── ScheduledTask.cs   # 任务数据模型
│   ├── TaskRepository.cs  # 数据访问层
│   ├── ApiCaller.cs       # API 调用器
│   ├── TaskScheduler.cs   # 定时任务调度器
│   ├── DatabaseInitializer.cs # 数据库初始化
│   └── RelayCommand.cs    # 命令类
├── ScheduledHttpTasks.csproj # 项目文件
└── tasks.db              # SQLite 数据库（运行时生成）
```

## 数据库文件位置

SQLite 数据库文件 `tasks.db` 将存储在项目根目录下。

## 使用方法

### 1. 启动应用

- 编译并运行 WPF 应用程序
- 应用程序会自动创建并初始化数据库文件 `tasks.db`

### 2. 解析 CURL 命令

支持解析格式类似这样的 CURL 命令：

```bash
curl -X POST 'https://api.coze.cn/v1/workflow/run' \
  -H "Authorization: Bearer sat_7T62ZNmq7ZnXgVnUSWzAYaP8GrudVLuO2lI6NTkRoZs73e9syGibSK7jYnrzc9Gm" \
  -H "Content-Type: application/json" \
  -d '{ "workflow_id": "7629385548395167784" }'
```

### 3. 常用的 Cron 表达式

| 表达式 | 说明 |
|--------|------|
| 0 0 0 * * ? | 每天午夜 0 点执行 |
| 0 30 8 * * ? | 每天早上 8:30 执行 |
| 0 0 0,12 * * ? | 每天午夜 0 点和中午 12 点执行 |
| 0 0 0 * * MON-FRI | 每周一到周五午夜 0 点执行 |
| 0 0 0 1 * ? | 每月 1 号午夜 0 点执行 |
| 0 0 0 L * ? | 每月最后一天午夜 0 点执行 |
| 0 0/10 * * * ? | 每 10 分钟执行一次 |

## 任务执行流程

1. 调度器在指定时间触发任务
2. 执行 HTTP 请求
3. 记录响应状态和时间
4. 保存到数据库
5. 更新任务状态

## 开发环境

- **IDE**：Visual Studio 2022 或更高版本
- **SDK**：.NET 8.0 SDK 或更高版本
- **操作系统**：Windows 10 或更高版本

## 构建项目

```bash
# 恢复 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行应用程序
dotnet run --project ScheduledHttpTasks
```

## 注意事项

- 确保网络连接正常
- 任务执行可能会受到网络延迟影响
- 数据库文件将在应用程序目录下生成
- 任务执行日志会自动保存到数据库中

## 版本历史

- **v1.0.0** - 初始版本

## 许可证

MIT License
