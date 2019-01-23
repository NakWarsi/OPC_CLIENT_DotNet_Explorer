using System;
namespace ClientTest1
{
    class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine(".Net Core OPC UA Testing Client");
            bool autoAccept = false;

            ClientOPC Client = new ClientOPC(autoAccept);
            Client.Run();

            return (int)ClientOPC.ExitCode;
        }
    }
 }