using WebServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:7296");

WebApplication app = builder.Build();

app.UseWebSockets();
app.UseWebSocketHandler();
app.UseRouting();
app.UseStaticFiles();
app.UseDefaultFiles();
app.MapFallbackToFile("index.html");

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/ReceivedMessages", context =>
    {
        string messages = WebSocketMiddleware.GetReceivedMessages();
        return context.Response.WriteAsync(messages);
    });

    endpoints.MapPost("/SendMessage", async context =>
    {
        if (context.Request.Body != null)
        {
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                string message = await reader.ReadToEndAsync();
                await WebSocketMiddleware.BroadcastMessage(message);
            }
            context.Response.StatusCode = 200;
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    });
});

await app.RunAsync();