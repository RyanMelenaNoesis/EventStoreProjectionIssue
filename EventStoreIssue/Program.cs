using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventStoreIssue
{
    class Program
    {
        protected const string PROJECTION_DEST_STREAM_NAME = "projection-destination";
        protected const string PROJECTION_SOURCE_STREAM_NAME = "projection-source";
        protected const string TEST_PROJECTION_NAME = "test-projection";

        static void Main(string[] args)
        {
            string eventStoreHost = "asadoDev.internal.noesislabs.com";
            string eventStoreUsername = "admin";
            string eventStorePassword = "changeit";
            int eventStoreTcpPort = 1113;
            int eventStoreHttpPort = 2113;

            var addresses = Dns.GetHostAddressesAsync(eventStoreHost).Result;

            var projectionManager = new ProjectionsManager(new ConsoleLogger(), new IPEndPoint(addresses.First(), eventStoreHttpPort), TimeSpan.FromSeconds(5));

            var testProjectionQuery = String.Format(@"
                    fromAll()
                    .when({{
                        DummyEventA: function(s, e) {{
                            linkTo('{0}', e);
                        }},
                        DummyEventB: function(s, e) {{
                            linkTo('{0}', e);
                        }}
                    }});
                ", PROJECTION_DEST_STREAM_NAME);

            projectionManager.CreateContinuousAsync(TEST_PROJECTION_NAME, testProjectionQuery, new UserCredentials(eventStoreUsername, eventStorePassword)).Wait();

            Thread.Sleep(3000);

            var connectionSettings = ConnectionSettings.Create()
                                        .SetReconnectionDelayTo(TimeSpan.FromSeconds(1))
                                        .KeepReconnecting();

            var uriBuilder = new UriBuilder
            {
                Scheme = "tcp",
                UserName = eventStoreUsername,
                Password = eventStorePassword,
                Host = eventStoreHost,
                Port = eventStoreTcpPort
            };

            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, uriBuilder.Uri);

            eventStoreConnection.ConnectAsync().Wait();

            List<object> events = new List<object>
            {
                new DummyEventA(Guid.NewGuid()),
                new DummyEventA(Guid.NewGuid()),
                new DummyEventA(Guid.NewGuid()),
                new DummyEventA(Guid.NewGuid()),
                new DummyEventA(Guid.NewGuid()),
                new DummyEventB(Guid.NewGuid()),
                new DummyEventB(Guid.NewGuid()),
                new DummyEventB(Guid.NewGuid()),
                new DummyEventB(Guid.NewGuid()),
                new DummyEventB(Guid.NewGuid())
            };

            int expectedVersion = -1;
            foreach (var e in events)
            {
                var eventData = ToEventData(Guid.NewGuid(), e, new Dictionary<string, string>());
                var result = eventStoreConnection.AppendToStreamAsync(PROJECTION_SOURCE_STREAM_NAME, expectedVersion, new EventData[] { eventData }).Result;
                expectedVersion = result.NextExpectedVersion;
            }
        }

        private static EventData ToEventData(Guid eventId, object evnt, IDictionary<string, string> headers)
        {
            var serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evnt, serializerSettings));
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(headers, serializerSettings));
            var typeName = evnt.GetType().Name;

            return new EventData(eventId, typeName, true, data, metadata);
        }
    }
}
