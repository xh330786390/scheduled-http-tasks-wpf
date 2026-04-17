
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace TestParser
{
    class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("=== 测试 CURL 解析功能 ===");
                
                string curlCommand = @"curl -X POST 'https://api.coze.cn/v1/workflow/run' -H 'Authorization: Bearer sat_7T62ZNmq7ZnXgVnUSWzAYaP8GrudVLuO2lI6NTkRoZs73e9syGibSK7jYnrzc9Gm' -H 'Content-Type: application/json' -d '{ ""workflow_id"": ""7629385548395167784"" }'";
                
                var curlData = ParseCurlInternal(curlCommand);
                
                Console.WriteLine($"\n解析结果:");
                Console.WriteLine($"URL: {curlData.Url}");
                Console.WriteLine($"方法: {curlData.Method}");
                Console.WriteLine($"请求头: {JsonConvert.SerializeObject(curlData.Headers, Newtonsoft.Json.Formatting.Indented)}");
                Console.WriteLine($"请求体: {curlData.Body}");
                
                Console.WriteLine("\n=== 功能测试成功 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex}");
                Console.WriteLine($"\n堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        // 复制 TaskDialog.xaml.cs 中的解析方法
        static (string Url, string Method, dynamic Headers, string Body) ParseCurlInternal(string curl)
        {
            // 清理 CURL 字符串：移除换行符和连接符
            var cleaned = curl.Trim()
                .Replace("\\", "")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
            if (!cleaned.StartsWith("curl "))
                throw new ArgumentException("不是有效的 CURL 命令");

            // 解析 URL
            var urlMatch = Regex.Match(cleaned, @"'([^']+)'|""([^""]+)""|(\S+)");
            if (!urlMatch.Success)
                throw new ArgumentException("无法解析 URL");
            var url = urlMatch.Groups[1].Value;
            if (string.IsNullOrEmpty(url))
                url = urlMatch.Groups[2].Value;
            if (string.IsNullOrEmpty(url))
                url = urlMatch.Groups[3].Value;

            // 解析请求方法
            var methodMatch = Regex.Match(cleaned, @"-X\s+(\S+)");
            var method = methodMatch.Success ? methodMatch.Groups[1].Value.ToUpper() : "GET";

            // 解析请求头
            var headers = new System.Dynamic.ExpandoObject() as System.Collections.Generic.IDictionary<string, object>;
            var headerMatches = Regex.Matches(cleaned, @"-H\s+'([^']+)'|-H\s+""([^""]+)""|-H\s+(\S+)");
            foreach (Match match in headerMatches)
            {
                string header;
                if (match.Groups[1].Success)
                    header = match.Groups[1].Value;
                else if (match.Groups[2].Success)
                    header = match.Groups[2].Value;
                else
                    header = match.Groups[3].Value;

                if (string.IsNullOrEmpty(header)) continue;

                var parts = header.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    headers[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // 解析请求体
            var bodyMatch = Regex.Match(cleaned, @"-d\s+'([^']+)'|-d\s+""([^""]+)""|-d\s+(\S+)");
            string body = "{}";
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
                    dynamic jsonBody = JsonConvert.DeserializeObject(bodyContent);
                    body = JsonConvert.SerializeObject(jsonBody, Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    body = bodyContent;
                }
            }

            return (url, method, headers, body);
        }
    }
}
