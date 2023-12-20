using Grpc.Core;
using ServiceApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<SyncMqService>();

var app = builder.Build();
app.UseGrpcWeb();

// Configure the HTTP request pipeline.
app.MapGrpcService<SyncMqService>().EnableGrpcWeb() ;
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.Run();

//var factory = new StaticResolverFactory(addr => new[]
//{
//    new BalancerAddress("localhost", 80),
//    new BalancerAddress("localhost", 81)
//});