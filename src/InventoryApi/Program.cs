using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string mongoConn = Environment.GetEnvironmentVariable("MONGO_CONN") ?? "";
string redisConn = Environment.GetEnvironmentVariable("REDIS_CONN") ?? "";
string rabbitConn = Environment.GetEnvironmentVariable("RABBITMQ_CONN") ?? "";

app.MapGet("/hi", () => Results.Json(new
{
	message = "hi",
	version = "v1",
	time = DateTime.UtcNow,
	connections = new
	{
		mongo = string.IsNullOrEmpty(mongoConn) ? "unset" : "set",
		redis = string.IsNullOrEmpty(redisConn) ? "unset" : "set",
		rabbitmq = string.IsNullOrEmpty(rabbitConn) ? "unset" : "set"
	}
}));

app.MapGet("/", () => Results.Text("Inventory API - minimal."));

app.Run();
