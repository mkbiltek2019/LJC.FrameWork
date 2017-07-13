﻿using LJC.FrameWork.SocketApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LJC.FrameWork.SocketEasyUDP.Client
{
    public class ClientBase:UDPSocketBase
    {
        private UdpClient _udpClient;
        private System.Net.IPEndPoint _serverPoint = null;
        protected volatile bool _stop = true;
        private volatile bool _isstartclient = false;

        SendMsgManualResetEventSlim _sendmsgflag = new SendMsgManualResetEventSlim();

        public ClientBase(string host,int port)
        {
            _udpClient = new UdpClient();
            _udpClient.Connect(host, port);
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1024 * 1000);
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1024 * 1000);
            _serverPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(host), port);
        }


        public override bool SendMessage(Message msg, EndPoint endpoint)
        {
            var bytes = LJC.FrameWork.EntityBuf.EntityBufCore.Serialize(msg);

            int trytimes = 0;
            foreach (var segment in SplitBytes(bytes))
            {
                trytimes = 0;
                while (true)
                {
                    lock (_udpClient.Client)
                    {
                        var segmentid = BitConverter.ToInt64(segment, 0);
                        _sendmsgflag.SegmentId = segmentid;
                        _sendmsgflag.Reset();
                        _udpClient.Send(segment, segment.Length);
                        if (_sendmsgflag.Wait(TimeOutMillSec))
                        {
                            if (trytimes > 2)
                            {
                                Console.WriteLine("花费了" + trytimes + "次");
                                //Thread.Sleep(5000);
                            }
                            break;
                        }
                        else
                        {
                            if (trytimes++ >= TimeOutTryTimes)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }
                }
            }

            return true;
        }

        protected void SendEcho(long segmentid)
        {
            //Message echo = new Message(MessageType.UDPECHO);
            //var buffer = LJC.FrameWork.EntityBuf.EntityBufCore.Serialize(echo);
            var buffer = BitConverter.GetBytes(segmentid);
            _udpClient.Send(buffer, buffer.Length);
        }

        public void StartClient()
        {
            _stop = false;
            new Action(() =>
                {
                    while (!_stop)
                    {
                        try
                        {
                            _isstartclient = true;
                            var bytes = _udpClient.Receive(ref _serverPoint);
                            var segmentid = BitConverter.ToInt64(bytes, 0);
                            if (bytes.Length > 8)
                            {
                                SendEcho(segmentid);
                                OnMessage(bytes);
                            }
                            else
                            {
                                if (_sendmsgflag.SegmentId == segmentid)
                                {
                                    _sendmsgflag.Set();
                                }
                            }
                        }
                        catch (ObjectDisposedException ex)
                        {
                            OnError(ex);
                        }
                        catch(Exception ex)
                        {
                            OnError(ex);
                        }
                    }
                }).BeginInvoke(null, null);

            while (true)
            {
                if (_isstartclient)
                {
                    break;
                }
                Thread.Sleep(10);
            }
        }

        protected virtual void OnMessage(Message message)
        {
        }
        

        private void OnMessage(byte[] data)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((o) =>
            {
                var margebytes = MargeBag(data);
                if (margebytes != null)
                {
                    var message = LJC.FrameWork.EntityBuf.EntityBufCore.DeSerialize<Message>(margebytes);
                    OnMessage(message);
                }
            }));
        }

        protected override void DisposeManagedResource()
        {
            base.DisposeManagedResource();
        }

        protected override void DisposeUnManagedResource()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
            }

            base.DisposeUnManagedResource();
        }
    }
}
