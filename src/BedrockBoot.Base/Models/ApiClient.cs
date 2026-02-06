using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Linq;

namespace BedrockBoot.Base.Models
{
    /// <summary>
    /// API客户端配置
    /// </summary>
    public class ApiClientOptions
    {
        /// <summary>
        /// 基础URL
        /// </summary>
        public string BaseUrl { get; set; }
        
        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int Timeout { get; set; } = 30;
        
        /// <summary>
        /// 默认请求头
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; } = new();
        
        /// <summary>
        /// 是否自动处理重定向
        /// </summary>
        public bool AllowAutoRedirect { get; set; } = true;
        
        /// <summary>
        /// JSON序列化选项
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
    }

    /// <summary>
    /// API响应模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 状态码
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
        
        /// <summary>
        /// 响应数据
        /// </summary>
        public T Data { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 原始响应内容
        /// </summary>
        public string RawContent { get; set; }
        
        /// <summary>
        /// 响应头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    /// <summary>
    /// 通用API客户端
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class ApiClient<T> : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        /// <summary>
        /// 基础URL
        /// </summary>
        public Uri BaseUrl { get; set; }

        /// <summary>
        /// 默认请求头
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; } = new();

        /// <summary>
        /// 请求/响应拦截器
        /// </summary>
        public event Func<HttpRequestMessage, Task> OnRequestSending;
        public event Func<HttpResponseMessage, Task> OnResponseReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">客户端配置</param>
        public ApiClient(ApiClientOptions options = null)
        {
            options ??= new ApiClientOptions();
            
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = options.AllowAutoRedirect,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(options.Timeout)
            };

            if (!string.IsNullOrEmpty(options.BaseUrl))
            {
                BaseUrl = new Uri(options.BaseUrl);
            }

            if (options.DefaultHeaders != null)
            {
                foreach (var header in options.DefaultHeaders)
                {
                    DefaultHeaders[header.Key] = header.Value;
                }
            }

            _jsonOptions = options.JsonSerializerOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// 构造函数（直接传入HttpClient）
        /// </summary>
        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        /// <param name="endpoint">API端点</param>
        /// <param name="queryParams">查询参数</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>API响应</returns>
        public async Task<ApiResponse<T>> GetAsync(
            string endpoint, 
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint, queryParams);
            using var request = CreateRequest(HttpMethod.Get, url, headers);
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<ApiResponse<T>> PostAsync(
            string endpoint, 
            object data = null,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(HttpMethod.Post, url, headers);
            
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 发送PUT请求
        /// </summary>
        public async Task<ApiResponse<T>> PutAsync(
            string endpoint, 
            object data = null,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(HttpMethod.Put, url, headers);
            
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public async Task<ApiResponse<T>> DeleteAsync(
            string endpoint,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(HttpMethod.Delete, url, headers);
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 发送PATCH请求
        /// </summary>
        public async Task<ApiResponse<T>> PatchAsync(
            string endpoint, 
            object data = null,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(new HttpMethod("PATCH"), url, headers);
            
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 发送表单数据（application/x-www-form-urlencoded）
        /// </summary>
        public async Task<ApiResponse<T>> PostFormAsync(
            string endpoint,
            Dictionary<string, string> formData,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(HttpMethod.Post, url, headers);
            
            if (formData != null && formData.Count > 0)
            {
                request.Content = new FormUrlEncodedContent(formData);
            }
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public async Task<ApiResponse<T>> UploadFileAsync(
            string endpoint,
            string filePath,
            string formFieldName = "file",
            Dictionary<string, string> formData = null,
            Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            using var request = CreateRequest(HttpMethod.Post, url, headers);
            
            using var content = new MultipartFormDataContent();
            
            // 添加文件
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, formFieldName, fileName);
            
            // 添加其他表单数据
            if (formData != null)
            {
                foreach (var kvp in formData)
                {
                    content.Add(new StringContent(kvp.Value), kvp.Key);
                }
            }
            
            request.Content = content;
            
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// 设置Bearer Token认证
        /// </summary>
        public void SetBearerToken(string token)
        {
            DefaultHeaders["Authorization"] = $"Bearer {token}";
        }

        /// <summary>
        /// 设置基础认证
        /// </summary>
        public void SetBasicAuth(string username, string password)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            DefaultHeaders["Authorization"] = $"Basic {credentials}";
        }

        /// <summary>
        /// 清除所有默认请求头
        /// </summary>
        public void ClearDefaultHeaders()
        {
            DefaultHeaders.Clear();
        }

        /// <summary>
        /// 构建完整URL
        /// </summary>
        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null)
        {
            if (BaseUrl == null)
            {
                return endpoint;
            }

            var baseUrl = BaseUrl.ToString().TrimEnd('/');
            var endpointPath = endpoint.TrimStart('/');
            var url = $"{baseUrl}/{endpointPath}";

            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams.Select(kvp => 
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                url += $"?{queryString}";
            }

            return url;
        }

        /// <summary>
        /// 创建HTTP请求
        /// </summary>
        private HttpRequestMessage CreateRequest(HttpMethod method, string url, Dictionary<string, string> customHeaders = null)
        {
            var request = new HttpRequestMessage(method, url);
            
            // 添加默认请求头
            foreach (var header in DefaultHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            
            // 添加自定义请求头
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            
            return request;
        }

        /// <summary>
        /// 发送HTTP请求并处理响应
        /// </summary>
        private async Task<ApiResponse<TResponse>> SendRequestAsync<TResponse>(HttpRequestMessage request)
        {
            var response = new ApiResponse<TResponse>();
            
            try
            {
                // 触发请求前事件
                if (OnRequestSending != null)
                {
                    await OnRequestSending.Invoke(request);
                }

                // 发送请求
                var httpResponse = await _httpClient.SendAsync(request);
                response.StatusCode = httpResponse.StatusCode;
                
                // 收集响应头
                foreach (var header in httpResponse.Headers)
                {
                    response.Headers[header.Key] = string.Join(", ", header.Value);
                }
                
                // 读取响应内容
                var content = await httpResponse.Content.ReadAsStringAsync();
                response.RawContent = content;
                
                // 检查响应状态
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.IsSuccess = true;
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            // 尝试反序列化响应内容
                            var data = JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
                            response.Data = data;
                        }
                        catch (JsonException)
                        {
                            // 如果无法反序列化为TResponse，可能是其他类型或错误
                            response.ErrorMessage = "Failed to deserialize response";
                        }
                    }
                }
                else
                {
                    response.IsSuccess = false;
                    response.ErrorMessage = $"HTTP Error: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}";
                    
                    // 尝试解析错误信息
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            // 尝试将错误内容反序列化为字典
                            var errorData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
                            if (errorData != null && errorData.ContainsKey("message"))
                            {
                                response.ErrorMessage = errorData["message"].ToString();
                            }
                        }
                        catch
                        {
                            // 如果无法解析，直接使用原始内容
                            response.ErrorMessage = content;
                        }
                    }
                }
                
                // 触发响应后事件
                if (OnResponseReceived != null)
                {
                    await OnResponseReceived.Invoke(httpResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = $"Network error: {ex.Message}";
            }
            catch (TaskCanceledException ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = $"Request timeout: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            
            return response;
        }

        /// <summary>
        /// 获取原始的HttpClient（用于高级操作）
        /// </summary>
        public HttpClient GetHttpClient() => _httpClient;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}