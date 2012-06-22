﻿#region License

//     This file is part of Metin 2 Server Emulator.
// 
//     Metin 2 Server Emulator is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Metin 2 Server Emulator is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Metin 2 Server Emulator.  If not, see <http://www.gnu.org/licenses/>

#endregion

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Metin2ServerEmulatorCommon;
using Metin2ServerEmulatorCommon.LocalNetwork.Packets;
using Timer = System.Timers.Timer;

namespace Metin2AuthServerEmulator.LocalNetwork
{
    internal class LocalNetworkService
    {
        private readonly Timer _cleanUpTimer;
        private readonly TcpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Logger _log;
        private List<LocalClient> _clients = new List<LocalClient>();

        private bool _listening;

        /// <summary>
        ///   Constructor
        /// </summary>
        /// <param name="log"> Logger instance </param>
        /// <param name="port"> Port to listen on </param>
        internal LocalNetworkService(Logger log, int port)
        {
            _log = log;
            _listenerThread = new Thread(Listen) {Name = "LocalNetwork Listener"};
            _listener = new TcpListener(IPAddress.Any, port);

            _cleanUpTimer = new Timer(60*30*100) {AutoReset = true};
            _cleanUpTimer.Elapsed += CleanUpTimerTick;
        }

        /// <summary>
        ///   Starts listening service
        /// </summary>
        internal void Start()
        {
            _log.Info("Starting LocalNetwork listener");
            _listening = true;
            _listener.Start();
            _listenerThread.Start();
            _cleanUpTimer.Start();
        }

        /// <summary>
        ///   Stops Listening Service
        /// </summary>
        internal void Stop()
        {
            DropAllClients();
            _log.Info("Stopping LocalNetwork listener");
            _listening = false;
            _listenerThread.Abort();
            _listener.Stop();
            _cleanUpTimer.Stop();
        }

        private void CleanUpTimerTick(object sender, EventArgs e)
        {
            List<LocalClient> newList = new List<LocalClient>();
            for (int i = 0; i < _clients.Count; i++)
            {
                if (!_clients[i].IsConnected)
                {
                    _clients[i] = null;
                }
                else
                {
                    newList.Add(_clients[i]);
                }
            }
            _clients = newList;
            newList = null;
            GC.Collect();
        }

        internal void BroadCast(IPacket packet)
        {
            foreach (LocalClient cl in _clients)
            {
                if (cl.IsConnected)
                    cl.SendPacket(packet);
            }
        }

        /// <summary>
        ///   Drops all clients
        /// </summary>
        /// <param name="reason"> Disconnect reason </param>
        internal void DropAllClients(string reason = "Stopping Server")
        {
            foreach (LocalClient cl in _clients)
            {
                cl.Drop(reason);
            }
        }

        /// <summary>
        ///   Disconnects all clients
        /// </summary>
        /// <param name="reason"> Disconnect reason </param>
        internal void DisconnectAllClients(string reason = "Stopping Server")
        {
            foreach (LocalClient cl in _clients)
            {
                if (cl.IsConnected)
                    cl.Disconnect(reason);
            }
        }

        /// <summary>
        ///   Listening method. Start this only with _listenerThread
        /// </summary>
        private void Listen()
        {
            while (_listening)
            {
                // AcceptTcpClient() is blocker so cycle continues only when there's a connection incoming
                _clients.Add(new LocalClient(_listener.AcceptTcpClient()));
            }
        }
    }
}