
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
        
        static (string Url, string Method, dynamic Headers, string Body) ParseCurlInternal(string curl)
        {
            var cleaned = curl.Trim()
                .Replace("\\", "")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
            if (!cleaned.StartsWith("curl "))
                throw new ArgumentException("不是有效的 CURL 命令");

            string url = null;
            string method = "GET";
            var headers = new System.Dynamic.ExpandoObject() as System.Collections.Generic.IDictionary<string, object>;
            string body = "{}";

            // 移除 curl 前缀
            var curlCommand = cleaned.Substring(5).Trim();

            // 解析方法
            var methodMatch = Regex.Match(curlCommand, @"-X\s+(\S+)");
            if (methodMatch.Success)
            {
                method = methodMatch.Groups[1].Value.ToUpper();
                curlCommand = curlCommand.Replace(methodMatch.Value, "").Trim();
            }

            // 解析请求头
            var headerMatches = Regex.Matches(curlCommand, @"-H\s+'([^']+)'|-H\s+""([^""]+)""|-H\s+(\S+)");
            foreach (Match match in headerMatches)
            {
                string header;
                if (match.Groups[1].Success)
                    header = match.Groups[1].Value;
                else if (match.Groups[2].Success)
                    header = match.Groups[2].Value;
                else
                    header = match.Groups[3].Value;

                if (!string.IsNullOrEmpty(header))
                {
                    var parts = header.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        headers[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                curlCommand = curlCommand.Replace(match.Value, "").Trim();
            }

            // 解析请求体
            var bodyMatch = Regex.Match(curlCommand, @"-d\s+'([^']+)'|-d\s+""([^""]+)""|-d\s+(\S+)");
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
                curlCommand = curlCommand.Replace(bodyMatch.Value, "").Trim();
            }

            // 解析 URL - 使用更可靠的方法
            var urlMatch = Regex.Match(curlCommand, @"'([^']+)'|""([^""]+)""|(\S+)");
            if (urlMatch.Success)
            {
                if (urlMatch.Groups[1].Success)
                    url = urlMatch.Groups[1].Value;
                else if (urlMatch.Groups[2].Success)
                    url = urlMatch.Groups[2].Value;
                else
                    url = urlMatch.Groups[3].Value;
            }

            if (string.IsNullOrEmpty(url) || url.Contains("-") || url.Contains("-H") || url.Contains("-d"))
            {
                var rawUrlMatch = Regex.Match(cleaned, @"'([^']+)'|""([^""]+)""");
                if (rawUrlMatch.Success)
                {
                    if (rawUrlMatch.Groups[1].Success)
                        url = rawUrlMatch.Groups[1].Value;
                    else
                        url = rawUrlMatch.Groups[2].Value;
                }
                else
                {
                    throw new ArgumentException("无法解析 URL");
                }
            }

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("无法解析 URL");

            return (url, method, headers, body);
        }
    }
}
