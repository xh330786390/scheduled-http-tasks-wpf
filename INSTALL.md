
# 安装说明

## 前提条件

在运行此应用程序之前，您需要安装以下软件：

### 1. .NET 8.0 SDK（推荐）

您需要安装 .NET 8.0 SDK 或更高版本。可以从以下链接下载：

**下载地址**：https://dotnet.microsoft.com/download/dotnet/8.0

**检查是否已安装**：
打开命令提示符或 PowerShell，运行：
```powershell
dotnet --version
```

如果显示版本号（如 `8.0.100`），说明已安装。

**安装步骤**：
1. 下载适合您系统的 .NET SDK（Windows 版本）
2. 运行安装程序，按照向导完成安装
3. 重新打开命令提示符或 PowerShell
4. 验证安装（运行 `dotnet --version`）

### 2. 或者使用 Visual Studio 2022

如果您已经安装了 Visual Studio 2022，可以直接打开解决方案文件 `ScheduledHttpTasks.sln`。

## 构建和运行应用程序

### 方法 1：使用批处理脚本（推荐）

双击项目根目录下的 `build.bat` 文件。这个脚本能：

1. 检查 .NET SDK 是否已安装
2. 恢复 NuGet 包
3. 编译项目
4. 发布应用程序

### 方法 2：使用命令行

#### 步骤 1：恢复 NuGet 包
```powershell
cd "D:\01-Worklist\coze\scheduled-http-tasks-wpf"
dotnet restore
```

#### 步骤 2：编译项目
```powershell
cd "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks"
dotnet build
```

#### 步骤 3：运行应用程序
```powershell
cd "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks"
dotnet run
```

#### 步骤 4：发布应用程序（可选，但推荐）
```powershell
cd "D:\01-Worklist\coze\scheduled-http-tasks-wpf\ScheduledHttpTasks"
dotnet publish -c Release -o "publish" -r win-x64 --self-contained true
```

发布后的文件会在 `ScheduledHttpTasks\bin\Release\net8.0-windows\win-x64\publish\` 目录下。

## 数据库文件位置

SQLite 数据库文件 `tasks.db` 会自动创建在应用程序所在的目录下，即：
- 开发环境：`D:\01-Worklist\coze\scheduled-http-tasks-wpf\`
- 发布环境：`ScheduledHttpTasks\bin\Release\net8.0-windows\win-x64\publish\`

## 常见问题

### 1. 找不到 dotnet 命令

**错误信息**：
`'dotnet' 不是内部或外部命令，也不是可运行的程序`

**解决方案**：
- 确保已正确安装 .NET SDK
- 尝试重新打开命令提示符或 PowerShell
- 检查环境变量是否包含 .NET SDK 路径

### 2. 无法连接到网络

**解决方案**：
- 确保网络连接正常
- 检查防火墙设置
- 如果使用代理，配置代理设置

### 3. 任务执行失败

**常见原因**：
- URL 格式不正确
- 网络连接问题
- API 接口返回错误
- 请求头或请求体格式错误

**解决方案**：
- 使用"执行测试"功能测试任务
- 检查任务配置是否正确
- 查看执行日志以获取详细错误信息

## 使用示例

### 添加任务

1. 点击"添加任务"按钮
2. 在对话框中填写：
   - 任务名称：任务的描述性名称
   - Cron 表达式：定时任务的执行时间（如 `0 0/10 * * * ?` 表示每 10 分钟执行）
   - API URL：要调用的接口地址
   - 方法：HTTP 方法（GET/POST/PUT/DELETE/PATCH）
   - 解析 CURL（可选）：可以直接粘贴 CURL 命令进行解析
3. 填写完后点击"确定"

### 解析 CURL 命令

1. 在任务对话框中，找到"解析 CURL"区域
2. 粘贴完整的 CURL 命令，如：
   ```bash
   curl -X POST 'https://api.coze.cn/v1/workflow/run' \
     -H "Authorization: Bearer sat_7T62ZNmq7ZnXgVnUSWzAYaP8GrudVLuO2lI6NTkRoZs73e9syGibSK7jYnrzc9Gm" \
     -H "Content-Type: application/json" \
     -d '{ "workflow_id": "7629385548395167784" }'
   ```
3. 点击"解析"按钮
4. 系统会自动解析并填充到相应的字段中

### 编辑任务

1. 在任务列表中选择要编辑的任务
2. 点击"编辑任务"按钮
3. 修改需要调整的字段
4. 点击"确定"保存

### 启动/停止任务

1. 在任务列表中选择任务
2. 点击"启动任务"或"停止任务"按钮
3. 任务状态会更新为"运行中"或"已停止"

### 执行测试

1. 在任务列表中选择任务
2. 点击"执行测试"按钮
3. 系统会立即执行任务并显示结果

## 卸载

要完全卸载应用程序，请删除以下内容：
1. 项目目录：`D:\01-Worklist\coze\scheduled-http-tasks-wpf\`
2. 数据库文件：`tasks.db`（如果已生成）
3. 发布的文件：`ScheduledHttpTasks\bin\` 目录

## 支持的平台

- Windows 10 及更高版本
- Windows Server 2016 及更高版本

## 技术支持

如果您在使用过程中遇到问题，可以：
1. 查看 README.md 中的详细文档
2. 检查执行日志
3. 确保网络和系统环境正常
