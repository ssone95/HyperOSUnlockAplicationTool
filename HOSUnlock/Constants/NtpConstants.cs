using System;
using GuerrillaNtp;

namespace HOSUnlock.Constants;

public class NtpConstants
{
    public static readonly string[] NtpServers = [
        "ntp0.ntp-servers.net", 
        "ntp1.ntp-servers.net", 
        "ntp2.ntp-servers.net",
        "ntp3.ntp-servers.net", 
        "ntp4.ntp-servers.net", 
        "ntp5.ntp-servers.net",
        "ntp6.ntp-servers.net"
    ];

    public const string DefaultTimeLabelText = "Time Details will appear here.";
    public const string TimeLabelText_RequestsCompletedCloseApp = "Close the application using Ctrl+Q";
    public const string TimeLabelText_WaitingForRequestsCompletion = "All thresholds reached, waiting for requests to be done...";

    public const int NtpRefreshIntervalMilliseconds = 100;

    public const int UITimerRefreshIntervalMilliseconds = 100;
}
