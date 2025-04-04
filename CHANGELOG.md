# Changelog

## 0.5.6

* Added back `logLevel` parameter as a legacy option for setting minimum log level. When both `logLevel` and `restrictedToMinimumLevel` are set, `restrictedToMinimumLevel` takes precedence.

## 0.5.5

* Fix version incompatibility with PostAsync retry logic.

## 0.5.4

* Better payload fragmentation to avoid creating payload even slightly bigger than 5MB.

## 0.5.3
* Disable `keep-alive` in `HttpClient`.
* Bump dependency on Serilog.Sinks.PeriodicBatching to 3.1.0.
* Renamed `logLevel` to `restrictedMinimumLogLevel` in LoggerConfiguration to be
  consistent with the serilog recommendation. Thanks @tsackey.
* Makes the number of retries configurable before the `IDataDogClient` gives up
  logging by adding a new DataDogConfiguration.MaxRetries property. Thanks @dmitrynovik.

## 0.5.2
* Improve the exception thrown when a request fails or retry times out. Thanks @cdhunt

## 0.5.1
* Allow using custom HttpClient and Url. Thanks @tomasz-soltysik

## 0.5.0
* Split logs larger than the max message size
* Make max message size configurable
* Raise max payload size to `5MB`

## 0.4.0
* Add support for custom `ITextFormatter`.

## 0.3.8
* Update Serilog.Sinks.PeriodicBatching to `3.0.0`. Thanks @dominikus1993 
  
## 0.3.7
* Bump Newtonsoft.Json to `13.0.1`

## 0.3.6

* Remove unused UTF8 property #66. Thanks @jnyrup  
* Support Datadog V2 intake API 
* .NET 5 and .NET 6 support #68. Thanks @AndrewMD5  
* Remove remaining traces of net6.0 #72. Thanks @jnyrup 

## 0.3.5

* Handle TCP disconnection leading to missing logs #64. Thanks @lokalan.

## 0.3.4

* Add more net framework targets to solve incompatibility issues with NuGet version resolving to "nearest" .net framework vs compiler targets

## 0.3.3

* Add a handler called when an exception occurs (#51).
* Minor refactoring (#42). Thanks @SychevIgor

## 0.3.2

* Allow batch size, period and queue limit to be set (#41). Thanks @benfoster.

## 0.3.1

* Remove excessive selflog
* Cleanup dependencies

## 0.3.0

* HTTP is now the default sending method for logs (#24). This is a **breaking change** for
users that were using a custom endpoint. TCP client can still be used by adding the `useTCP: true`
in the `DatadogConfiguration` object.

## 0.2.0

* Strong named the assembly (#22)
* Allow configuration from `appsettings.json` file (#11)

## 0.1.5

* Change severity field from "Level" to "level" (#15)
* Bug fix: Make sure the socket gets reopen when an exception is triggered (#14)

## 0.1.4

* Enabled custom source, service, host and tags

## 0.1.3

* Created a Serilog Sink to forward logs to Datadog (#2)
