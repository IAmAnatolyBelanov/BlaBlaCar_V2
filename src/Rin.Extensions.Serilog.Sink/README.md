> [!WARNING]
> 
> Это локальная копия [Rin+Serilog](https://github.com/IAmAnatolyBelanov/Rin/tree/serilog-support). Так сделано, потому что я не хочу возиться с сабмодулями гита. И так оно и останется, пока Rin не обновится с принятием этого [пул-реквеста](https://github.com/mayuki/Rin/pull/76) и выпуском соответствующего nuget-пакета. Ну или пока аналогичный функционал не появится по иным причинам.

Serilog sink for writings trace info to Rin.

# Syntax description

There are 2 ways to add Serilog support. Both ways give the same result.

## Code
```csharp
Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.WriteTo.Rin(LogEventLevel.Information)
	.CreateLogger();
```

## Configuration
```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Rin.Extensions.Serilog.Sink"
    ],
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Rin",
        "Args": {
          "restrictedToMinimumLevel": "Information"
        }
      }
    ]
  }
}
```

```csharp
var builder = WebApplication.CreateBuilder();
Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.CreateLogger();
```