
@echo off
echo 正在构建定时任务调度器项目...
echo.

cd /d "%~dp0"

REM 检查是否已安装 .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 未找到 .NET SDK。
    echo.
    echo 请先安装 .NET 8.0 SDK 或更高版本:
    echo https://dotnet.microsoft.com/download/dotnet
    pause
    exit /b 1
)

echo .NET SDK 版本:
dotnet --version
echo.

REM 恢复 NuGet 包
echo 正在恢复 NuGet 包...
dotnet restore
if %errorlevel% neq 0 (
    echo 错误: NuGet 包恢复失败
    pause
    exit /b 1
)
echo.

REM 编译项目
echo 正在编译项目...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo 错误: 项目编译失败
    pause
    exit /b 1
)
echo.

REM 发布项目
echo 正在发布项目...
dotnet publish -c Release -o "publish" -r win-x64 --self-contained true
if %errorlevel% neq 0 (
    echo 错误: 项目发布失败
    pause
    exit /b 1
)
echo.

echo 项目构建成功!
echo.
echo 输出目录: %cd%\ScheduledHttpTasks\bin\Release\
echo 发布目录: %cd%\ScheduledHttpTasks\bin\Release\net8.0-windows\win-x64\publish\
echo 数据库文件: %cd%\tasks.db
echo.

echo 可以通过以下命令运行应用程序:
echo dotnet run --project ScheduledHttpTasks\ScheduledHttpTasks.csproj
echo 或直接运行发布后的可执行文件
echo.

pause
