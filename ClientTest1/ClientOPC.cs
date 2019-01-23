using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ClientTest1
{
    class ClientOPC
    {
        Task status;
        static bool autoAccept = false;
        static ExitCode exitCode;

        public ClientOPC(bool _autoAccept)
        {
            autoAccept = _autoAccept;
        }

        // returning Status of the Client 
        public static ExitCode ExitCode { get => exitCode; }

        // running the Client
        internal void Run()
        {
            // Loading the Client configuration
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationName = "ConsoleClient";
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "ClientTest1Configuration";
            ApplicationConfiguration config = application.LoadApplicationConfiguration(false).Result;
            Console.WriteLine("configuration loaded");

            //step 2: creating session with the OPC server
            var session = Session.Create(config,
                    new ConfiguredEndpoint(null, new EndpointDescription("opc.tcp://" + Dns.GetHostName() + ":62546/ServerTest2")),
                    true,
                    "",
                    60000,
                    null,
                    null).Result;

            try
            {
                exitCode = ExitCode.ErrorClientNotStarted;
                ConsoleClient(session);                       //client manager called
                Console.WriteLine("Client started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorClientRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorClientException;
                return;
            }


            //to keeps thread into running state manually
            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }
            // wait for timeout or Ctrl-C
            quitEvent.WaitOne();
            exitCode = ExitCode.Ok;
        }

        public void ConsoleClient(Session session)
        {
            //EndpointDescription need to be changed according to your OPC server
            {
                Console.WriteLine("session created");

                //step 3: Browsing the Server Name Space0
                ReferenceDescriptionCollection refs;
                byte[] cp;

                session.Browse(
                    null,
                    null,
                    ObjectIds.ObjectsFolder,
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                    out cp,
                    out refs);

                ////testing to read an Scalar value from server
                var node = session.ReadValue(new NodeId("ns=2;s=Scalar_Static_DateTime")).ToString();
                Console.WriteLine(".........................." + node + "...................................");

                Console.WriteLine("....DisplayName:..........BrowseName,..........NodeClass..............");
                foreach (var rd in refs)
                {
                    Console.WriteLine("{0}:    {1},   {2}", rd.DisplayName, rd.BrowseName, rd.NodeClass);
                    ReferenceDescriptionCollection nextRefs;
                    byte[] nextCp;

                    session.Browse(
                        null,
                        null,
                        ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris),
                        0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences,
                        true,
                        (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                        out nextCp,
                        out nextRefs);

                    foreach (var nextRd in nextRefs)
                    {
                        Console.WriteLine("+ {0}: {1}, {2}:{3}", nextRd.DisplayName, nextRd.BrowseName, nextRd.NodeClass, nextRd.NodeId);
                        //Console.WriteLine();

                        //Nested browsing
                        ReferenceDescriptionCollection nextRefs2;
                        byte[] nextCp2;
                        session.Browse(
                            null,
                            null,
                            ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris),
                            0u,
                            BrowseDirection.Forward,
                            ReferenceTypeIds.HierarchicalReferences,
                            true,
                            (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                            out nextCp2,
                            out nextRefs2);

                        foreach (var nextRd2 in nextRefs2)
                        {
                            Console.WriteLine("   + {0}, {1}, {2}", nextRd2.DisplayName, nextRd2.BrowseName, nextRd2.NodeClass);
                            ReferenceDescriptionCollection nextRefs3;
                            byte[] nextCp3;

                            // browsing nested to get to the leaf of the node Architecture
                            session.Browse(
                                null,
                                null,
                                ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris),
                                0u,
                                BrowseDirection.Forward,
                                ReferenceTypeIds.HierarchicalReferences,
                                true,
                                (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                                out nextCp3,
                                out nextRefs3);
                            foreach (var nextRd3 in nextRefs3)
                            {
                                Console.WriteLine("     +{0},  {1},  {2}", nextRd3.DisplayName, nextRd3.BrowseName, nextRd3.NodeId);
                            }
                        }
                    }
                }

                //step 4: create a subscription at an specific interval
                var subscription = new Subscription(session.DefaultSubscription)
                {
                    PublishingInterval = 3000           //put the intervel you want to get the data from server
                };

                //adding items to moniter into the subscription
                var list = new List<MonitoredItem> {
                    new MonitoredItem(subscription.DefaultItem)
                    {
                        DisplayName = "ServerStatusCurrentTime",
                        StartNodeId = "i=2258"
                    },
                    new MonitoredItem(subscription.DefaultItem)
                    {
                        DisplayName = "en",
                        StartNodeId = "ns=2;s=Scalar_Simulation_Double"
                    },
                    new MonitoredItem(subscription.DefaultItem)
                    {
                    DisplayName = "en",
                    StartNodeId = "ns=2;s=Scalar_Simulation_DateTime"
                }
                };

                Console.WriteLine("Moniterd Items are added to the list");


                list.ForEach(i => i.Notification += OnNotification);    //adding the event handler to invoke the method to print the monitored attribut value 
                subscription.AddItems(list);

                //adding subscription to the session
                session.AddSubscription(subscription);

                subscription.Create();
                //Console.WriteLine(subscription.NotificationCount);
            }
        }

        private static int count = 0;
        private static void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            Console.WriteLine("{0}: data recieved", count++);
            foreach (var value in item.DequeueValues())
            {
                Console.WriteLine("{0}: {1}, {2}, {3}, {4}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode, value.StatusCode);
            }
        }
    }
}


//working completely fine