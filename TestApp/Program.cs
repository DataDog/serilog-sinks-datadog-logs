using System;

using System.IO;
using Serilog;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Serilog.Settings.Configuration;
using Serilog.Context;
using Serilog.Templates;

namespace TestApp
{

    public class DatadogJsonNoTemplateFormatter: ExpressionTemplate
    {
        public DatadogJsonNoTemplateFormatter() : base(@"{ {
            Timestamp: @t,
            level: @l,
            message: @m, 
            Properties: {..@p},
            Renderings: @r}
        }") {}
    }
    class Program
    {
        static void Main(string[] args)
        {

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json")
                .Build();

            // configuration.GetSection("Serilog").GetSection("WriteTo").GetChildren().FirstOrDefault().GetSection("Args")["apiKey"] = "FOOBAR";
            configuration["Serilog:WriteTo:1:Args:apiKey"] = "SOME_API_KEY";
            

            // Log.Logger.d
            Log.Logger = new LoggerConfiguration()
                // .ReadFrom.Configuration(configuration)
                // .Enrich.FromLogContext()
                .WriteTo.DatadogLogs(configuration["Serilog:WriteTo:1:Args:apiKey"], exceptionHandler: (x) => {
                    System.Console.WriteLine(x);
                } )
                // .WriteTo.DatadogLogs("REDACTED", service: "foobar", host: "brian-host", tags: new string[] {"foo:bar", "apple:orange", "version:1.2.3"}, formatter: new DatadogJsonNoTemplateFormatter(), maxMessageSize: 1000 * 1000)
                .CreateLogger();



        for (var i = 0; i < 10; i++) {
            Log.Information("foo " + i);
        }

            // var bigLog = System.IO.File.ReadAllText("./log.txt");
            // for (var i = 0; i < 100; i++) {
                // Log.Information(bigLog);
            // }   
                
            // An example

//             var position = new { Latitude = 25, Longitude = 134 };
// var elapsedMs = 34;

// Log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);

//             using (LogContext.PushProperty("A", 1)) {
//                 Log.Information("abc");


            
//             }
            Log.CloseAndFlush();
        }
    }
}
