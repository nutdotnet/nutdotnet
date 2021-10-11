using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetClient
{
    /// <summary>
    /// Provides events that can occur while connected to a NUT server.
    /// </summary>
    public class NUTClientEvents
    {
        public event EventHandler ServerConnected;

        public event EventHandler ServerDisconnected;

        #region Handlers

        internal void HandleServerConnected(object sender, EventArgs e)
        {
            ServerConnected.Invoke(sender, e);
        }

        internal void HandleServerDisconnected(object sender, EventArgs e)
        {
            ServerDisconnected.Invoke(sender, e);
        }

        #endregion
    }

    #region Event args



    #endregion
}
