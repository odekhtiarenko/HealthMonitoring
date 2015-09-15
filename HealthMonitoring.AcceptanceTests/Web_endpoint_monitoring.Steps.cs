﻿using System;
using System.Net;
using HealthMonitoring.AcceptanceTests.Helpers;
using HealthMonitoring.AcceptanceTests.Helpers.Entities;
using LightBDD;
using RestSharp;
using Xunit;
using Xunit.Abstractions;

namespace HealthMonitoring.AcceptanceTests
{
    public partial class Web_endpoint_monitoring : FeatureFixture, IDisposable
    {
        private Guid _identifier;
        private RestClient _client;
        private MockWebEndpoint _restEndpoint;
        private EndpointEntity _details;

        public Web_endpoint_monitoring(ITestOutputHelper output)
            : base(output)
        {
        }

        public void Dispose()
        {
            if (_restEndpoint == null)
                return;
            _restEndpoint.Dispose();
            _restEndpoint = null;
        }

        private void Given_a_monitor_api_client()
        {
            _client = ClientHelper.Build();
        }

        private void Given_a_rest_endpoint()
        {
            _restEndpoint = MockWebEndpointFactory.CreateNew();
        }

        private void When_client_registers_the_endpoint()
        {
            _identifier = _client.RegisterEndpoint(Protocols.Web, _restEndpoint.StatusAddress, "group", "name");
        }

        private void Then_monitor_should_start_monitoring_the_endpoint()
        {
            Wait.Until(
                TimeSpan.FromSeconds(10),
                () => _client.GetEndpointDetails(_identifier),
                e => e.LastResponseTime.GetValueOrDefault() > TimeSpan.Zero,
                "Endpoint monitoring did not started");
        }

        private void When_client_requests_endpoint_details()
        {
            _details = _client.GetEndpointDetails(_identifier);
        }

        private void Then_the_last_check_time_should_be_provided()
        {
            Assert.True(_details.LastCheckUtc != null, "Last check time is not provided");
        }

        private void Then_the_response_time_should_be_provided()
        {
            Assert.True(_details.LastResponseTime != null, "Last response time is not provided");
        }

        private void Given_an_endpoint_that_has_not_been_deployed_yet()
        {
            Given_a_rest_endpoint();
            _restEndpoint.SetupStatusResponse(HttpStatusCode.NotFound);
        }

        private void Then_monitor_should_observe_endpoint_status_being_STATUS(EndpointStatus status)
        {
            Wait.Until(
                TimeSpan.FromSeconds(10),
                () => _client.GetEndpointDetails(_identifier),
                e => e.Status == status,
                "Endpoint status did not changed");
        }

        private void Then_the_endpoint_additional_details_should_be_not_available()
        {
            Assert.Empty(_details.Details);
        }

        private void Then_the_endpoint_additional_details_should_contain_error_information()
        {
            Assert.NotEmpty(_details.Details);
            Assert.True(_details.Details.ContainsKey("code"), "Code missing");
        }

        private void Given_a_healthy_rest_endpoint()
        {
            Given_a_rest_endpoint();
            _restEndpoint.SetupStatusPlainResponse(HttpStatusCode.OK, "hello world!");
        }

        private void Given_an_unhealthy_rest_endpoint()
        {
            Given_a_rest_endpoint();
            _restEndpoint.SetupStatusResponse(HttpStatusCode.InternalServerError);
        }

        private void Then_the_endpoint_status_should_be_provided(EndpointStatus status)
        {
            Assert.Equal(status, _details.Status);
        }
    }
}