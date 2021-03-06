﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LJC.FrameWork.SocketApplication
{
    public class SendMsgManualResetEventSlim : ManualResetEventSlim
    {
        public long BagId
        {
            get;
            set;
        }

        public long SegmentId
        {
            get;
            set;
        }

        private bool _isTimeOut = true;
        public bool IsTimeOut
        {
            get
            {
                return _isTimeOut;
            }
            private set
            {
                _isTimeOut = value;
            }
        }

        public new void Reset()
        {
            _isTimeOut = true;
            base.Reset();
        }

        public new void Set()
        {
            IsTimeOut = false;
            base.Set();
        }
    }
}
