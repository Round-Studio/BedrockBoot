using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace BedrockBoot.Base.Entry.Pack.WebServer;

public class HttpContext
{
    public HttpListenerRequest Request { get; }
    public HttpListenerResponse Response { get; }
    public NameValueCollection QueryString => Request.QueryString;

    public HttpContext(HttpListenerRequest request, HttpListenerResponse response)
    {
        Request = request;
        Response = response;
    }

    public void SendResponse(string content, string contentType = "text/plain", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        Response.StatusCode = (int)statusCode;
        Response.ContentType = contentType;
        Response.ContentLength64 = buffer.Length;
        Response.OutputStream.Write(buffer, 0, buffer.Length);
        Response.OutputStream.Close();
    }
}