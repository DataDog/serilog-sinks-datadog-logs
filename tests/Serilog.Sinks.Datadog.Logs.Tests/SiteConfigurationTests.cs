// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using NUnit.Framework;
using Serilog.Sinks.Datadog.Logs;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class SiteConfigurationTests
    {
        [Test]
        public void DefaultSite_HttpUrlIsUsIntake()
        {
            var config = new DatadogConfiguration();
            Assert.AreEqual("https://http-intake.logs.datadoghq.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void EuSite_HttpUrlPointsToEuIntake()
        {
            var config = new DatadogConfiguration(site: "datadoghq.eu");
            Assert.AreEqual("https://http-intake.logs.datadoghq.eu", config.EffectiveHttpUrl);
        }

        [Test]
        public void Us3Site_HttpUrlPointsToUs3Intake()
        {
            var config = new DatadogConfiguration(site: "us3.datadoghq.com");
            Assert.AreEqual("https://http-intake.logs.us3.datadoghq.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void Us5Site_HttpUrlPointsToUs5Intake()
        {
            var config = new DatadogConfiguration(site: "us5.datadoghq.com");
            Assert.AreEqual("https://http-intake.logs.us5.datadoghq.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void Ap1Site_HttpUrlPointsToAp1Intake()
        {
            var config = new DatadogConfiguration(site: "ap1.datadoghq.com");
            Assert.AreEqual("https://http-intake.logs.ap1.datadoghq.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void GovSite_HttpUrlPointsToGovIntake()
        {
            var config = new DatadogConfiguration(site: "ddog-gov.com");
            Assert.AreEqual("https://http-intake.logs.ddog-gov.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void ExplicitUrl_WinsOverSiteForHttp()
        {
            var config = new DatadogConfiguration(
                url: "https://my.custom.intake.example.com",
                site: "datadoghq.eu");
            Assert.AreEqual("https://my.custom.intake.example.com", config.EffectiveHttpUrl);
        }

        [Test]
        public void EmptyOrWhitespaceSite_FallsBackToDefault()
        {
            var configEmpty = new DatadogConfiguration(site: "");
            Assert.AreEqual("https://http-intake.logs.datadoghq.com", configEmpty.EffectiveHttpUrl);

            var configWhitespace = new DatadogConfiguration(site: "   ");
            Assert.AreEqual("https://http-intake.logs.datadoghq.com", configWhitespace.EffectiveHttpUrl);
        }

        [Test]
        public void DefaultPort_MatchesTcpSslPort()
        {
            var config = new DatadogConfiguration();
            Assert.AreEqual(10516, DatadogConfiguration.DDPort);
            Assert.AreEqual(config.Port, DatadogConfiguration.DDPort);
        }

        [Test]
        public void DefaultSite_ConstantIsUsHost()
        {
            Assert.AreEqual("datadoghq.com", DatadogConfiguration.DefaultSite);
        }

        [Test]
        public void SitePropertySetter_OverridesPreviousValue()
        {
            var config = new DatadogConfiguration();
            Assert.AreEqual("https://http-intake.logs.datadoghq.com", config.EffectiveHttpUrl);

            config.Site = "datadoghq.eu";
            Assert.AreEqual("https://http-intake.logs.datadoghq.eu", config.EffectiveHttpUrl);
        }
    }
}
