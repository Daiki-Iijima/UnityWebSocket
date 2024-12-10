using System;
using UnityEngine;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Collections;
using System.Net.Sockets;

#if NETFX_CORE
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

public class NetworkUtils
{

    public static string GetLocalIPAddress()
    {
#if UNITY_EDITOR
        return GetEditorIPAddress();
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WSA
        return GetDeviceIPAddress();
#else
        return "Unsupported platform";
#endif
    }

    private static string GetEditorIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new Exception("No IPv4 address found in Editor!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error retrieving IP address in Editor: {e.Message}");
            return "Error";
        }
    }

    private static string GetDeviceIPAddress()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530); // Google DNSを利用してIPを決定
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "No IP";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error retrieving IP address on device: {e.Message}");
            return "Error";
        }
    }
}
