# Changelog

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
