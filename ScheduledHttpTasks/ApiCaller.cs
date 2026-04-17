
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ScheduledHttpTasks
{
    public static class ApiCaller
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static ApiCaller()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public static async Task<string> CallApiAsync(ScheduledTask task)
        {
            var startTime = DateTime.Now;
            
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod(task.Method), task.Url))
                {
                    // 设置请求头
                    try
                    {
                        if (!string.IsNullOrEmpty(task.Headers))
                        {
                            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(task.Headers);
                            if (headers != null)
                            {
                                foreach (var header in headers)
                                {
                                    if (!string.IsNullOrEmpty(header.Value))
                                    {
                                        // 特殊处理 Authorization 头
                                        if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (header.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                            {
                                                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", header.Value.Substring(7));
                                            }
                                            else
                                            {
                                                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
                                            }
                                        }
                                        else if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Content-Type 会在请求体中设置
                                        }
                                        else
                                        {
                                            request.Headers.Add(header.Key, header.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析请求头失败: {ex.Message}");
                    }

                    // 设置请求体
                    if (!string.IsNullOrEmpty(task.Body) && 
                        (task.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                         task.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                         task.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                    {
                        request.Content = new StringContent(task.Body, Encoding.UTF8, "application/json");
                    }

                    // 发送请求
                    var response = await _httpClient.SendAsync(request);
                    
                    var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
                    var responseText = await response.Content.ReadAsStringAsync();

                    // 保存日志
                    TaskRepository.SaveTaskLog(
                        task.Id, 
                        (int)response.StatusCode, 
                        responseText, 
                        (int)responseTime, 
                        response.IsSuccessStatusCode ? null : response.ReasonPhrase);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP {response.StatusCode}: {response.ReasonPhrase} - {responseText}");
                    }

                    return $"成功 ({(int)response.StatusCode})\n响应时间: {responseTime:F0}ms\n响应内容: {responseText}";
                }
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                TaskRepository.SaveTaskLog(
                    task.Id, 
                    null, 
                    null, 
                    (int)responseTime, 
                    ex.Message);
                
                return $"失败\n响应时间: {responseTime:F0}ms\n错误: {ex.Message}";
            }
        }

        public static async Task<string> CallApiWithCurlAsync(string curlCommand)
        {
            // 简单的 CURL 解析
            try
            {
                string url = "", method = "GET";
                string headers = "{}", body = "{}";

                var parts = curlCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "-X" && i + 1 < parts.Length)
                    {
                        method = parts[i + 1];
                        i++;
                    }
                    else if (parts[i] == "-H" && i + 1 < parts.Length)
                    {
                        // 解析请求头
                        var headerPart = parts[i + 1].Trim('\'', '"');
                        if (headerPart.Contains(':'))
                        {
                            var headerSplit = headerPart.Split(':', 2);
                            var headerDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
                            headerDict[headerSplit[0].Trim()] = headerSplit[1].Trim();
                            headers = JsonConvert.SerializeObject(headerDict);
                        }
                        i++;
                    }
                    else if (parts[i] == "-d" && i + 1 < parts.Length)
                    {
                        body = parts[i + 1].Trim('\'', '"');
                        i++;
                    }
                    else if (parts[i].StartsWith("http"))
                    {
                        url = parts[i].Trim('\'', '"');
                    }
                }

                var tempTask = new ScheduledTask
                {
                    Id = -1,
                    Name = "临时任务",
                    Url = url,
                    Method = method,
                    Headers = headers,
                    Body = body,
                    CronExpression = "0 0 0 * * ?"
                };

                return await CallApiAsync(tempTask);
            }
            catch (Exception ex)
            {
                return $"解析或请求失败: {ex.Message}";
            }
        }
    }
}
