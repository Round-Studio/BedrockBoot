
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Models;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Entity
{
    /// <summary>
    /// 错误信息记录类
    /// </summary>
    public class ErrorReport
    {
        [JsonPropertyName("bbVersion")]
        public string BBVersion { get; set; }
        
        [JsonPropertyName("errorTitle")]
        public string ErrorTitle { get; set; }
        
        [JsonPropertyName("exceptionCanInfo")]
        public ExceptionCanInfo ExceptionCanInfo { get; set; }
        
        [JsonPropertyName("exceptionInfo")]
        public ExceptionInfo ExceptionInfo { get; set; }
        
        [JsonPropertyName("networkInfo")]
        public NetworkInfo NetworkInfo { get; set; }
        
        [JsonPropertyName("configInfo")]
        public object ConfigInfo { get; set; }
        
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// 创建错误报告
        /// </summary>
        /// <param name="configData">配置数据对象</param>
        /// <param name="title">错误标题</param>
        /// <param name="message">异常消息</param>
        /// <param name="exception">异常对象</param>
        /// <param name="customData">自定义数据（可选）</param>
        public static ErrorReport Create(
            object configData, 
            string title, 
            Exception exception)
        {
            return new ErrorReport
            {
                BBVersion = $"{GlobalModel.BodyVersion}",
                ErrorTitle = title,
                ExceptionCanInfo = ExceptionCanInfo.Create(),
                ExceptionInfo = ExceptionInfo.Create(exception.Message, exception),
                NetworkInfo = NetworkInfo.Create(),
                ConfigInfo = configData
            };
        }

        /// <summary>
        /// 将错误报告转换为 JSON 字符串
        /// </summary>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// 将错误报告保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveToFile(string filePath)
        {
            var json = ToJson();
            System.IO.File.WriteAllText(filePath, json);
            FileName = filePath;
        }
    }

    /// <summary>
    /// 异常基本信息
    /// </summary>
    public class ExceptionCanInfo
    {
        [JsonPropertyName("errorTime")]
        public string ErrorTime { get; set; }

        public static ExceptionCanInfo Create()
        {
            return new ExceptionCanInfo
            {
                ErrorTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            };
        }
    }

    /// <summary>
    /// 异常详细信息
    /// </summary>
    public class ExceptionInfo
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("innerException")]
        public string InnerException { get; set; }
        
        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; }
        
        [JsonPropertyName("source")]
        public string Source { get; set; }

        public static ExceptionInfo Create(string message, Exception exception)
        {
            var info = new ExceptionInfo
            {
                Message = message
            };

            if (exception != null)
            {
                info.InnerException = exception.InnerException?.ToString();
                info.StackTrace = exception.StackTrace;
                info.Source = exception.Source;
                
                if (string.IsNullOrEmpty(message))
                {
                    info.Message = exception.Message;
                }
            }
            else
            {
                info.InnerException = "-";
                info.StackTrace = "-";
            }

            return info;
        }
    }

    /// <summary>
    /// 网络信息
    /// </summary>
    public class NetworkInfo
    {
        [JsonPropertyName("networkStatus")]
        public bool NetworkStatus { get; set; }
        
        [JsonPropertyName("pcNetworkInterface")]
        public string PCNetworkInterface { get; set; }
        
        [JsonPropertyName("interfaceName")]
        public string InterfaceName { get; set; }
        
        [JsonPropertyName("interfaceDescription")]
        public string InterfaceDescription { get; set; }
        
        [JsonPropertyName("wireless")]
        public string Wireless { get; set; }
        
        [JsonPropertyName("proxyStatus")]
        public bool ProxyStatus { get; set; }

        public static NetworkInfo Create()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var activeInterface = networkInterfaces.FirstOrDefault(n => 
                    n.OperationalStatus == OperationalStatus.Up && 
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                return new NetworkInfo
                {
                    NetworkStatus = activeInterface != null,
                    PCNetworkInterface = activeInterface?.NetworkInterfaceType.ToString() ?? "Unknown",
                    InterfaceName = activeInterface?.Name ?? "-",
                    InterfaceDescription = activeInterface?.Description ?? "-",
                    Wireless = "-",
                    ProxyStatus = false
                };
            }
            catch
            {
                return new NetworkInfo
                {
                    NetworkStatus = false,
                    PCNetworkInterface = "Unknown",
                    InterfaceName = "-",
                    InterfaceDescription = "-",
                    Wireless = "-",
                    ProxyStatus = false
                };
            }
        }
    }
}
