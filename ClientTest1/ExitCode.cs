namespace ClientTest1
{
    //to return the Status code of the Client
    public enum ExitCode : int
    {
        Ok = 0,
        ErrorClientNotStarted = 0x80,
        ErrorClientRunning = 0x81,
        ErrorClientException = 0x82,
        ErrorInvalidCommandLine = 0x100
    }
}