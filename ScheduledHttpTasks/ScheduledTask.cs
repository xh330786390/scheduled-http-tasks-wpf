
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Quartz;

namespace ScheduledHttpTasks
{
    public class ScheduledTask : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _url;
        private string _method;
        private string _headers;
        private string _body;
        private string _cronExpression;
        private string _status;
        private DateTime? _nextFireTime;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    // 名称变更时触发下拉列表更新
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string Method
        {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        public string Headers
        {
            get => _headers;
            set => SetProperty(ref _headers, value);
        }

        public string Body
        {
            get => _body;
            set => SetProperty(ref _body, value);
        }

        public string CronExpression
        {
            get => _cronExpression;
            set
            {
                if (SetProperty(ref _cronExpression, value))
                {
                    OnPropertyChanged(nameof(NextFireTime));
                }
            }
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime? NextFireTime
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(CronExpression))
                        return null;

                    var trigger = Quartz.TriggerBuilder.Create()
                        .WithCronSchedule(CronExpression)
                        .Build();

                    var nextFireTime = trigger.GetNextFireTimeUtc();
                    if (nextFireTime.HasValue)
                    {
                        // 转换为北京时间 (UTC+8)
                        var beijingTime = nextFireTime.Value.DateTime.AddHours(8);
                        return beijingTime;
                    }
                    else
                        return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析Cron表达式失败: {ex.Message}");
                    return null;
                }
            }
            set { }
        }

        // 辅助属性
        [JsonIgnore]
        public dynamic HeadersObject
        {
            get
            {
                if (string.IsNullOrEmpty(Headers))
                    return new System.Dynamic.ExpandoObject();
                try
                {
                    return JsonConvert.DeserializeObject(Headers);
                }
                catch
                {
                    return new System.Dynamic.ExpandoObject();
                }
            }
        }

        [JsonIgnore]
        public string BodyString
        {
            get => Body ?? "{}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public override string ToString()
        {
            return $"{Id}: {Name} - {CronExpression}";
        }
    }
}
