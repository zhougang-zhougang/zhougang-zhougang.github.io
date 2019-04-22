// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Hub.Core
{
    using System;

    public class NullDeviceConnectivityManager : IDeviceConnectivityManager
    {
        public event EventHandler DeviceConnected
        {
            add { }
            remove { }
        }

        public event EventHandler DeviceDisconnected
        {
            add { }
            remove { }
        }

        public void CallSucceeded()
        {
        }

        public void CallTimedOut()
        {
        }
    }
}
