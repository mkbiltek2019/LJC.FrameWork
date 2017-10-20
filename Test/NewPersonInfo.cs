﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    public class NewPersonInfo
    {
        public String Name
        {
            get;
            set;
        }

        public int Age
        {
            get;
            set;
        }

        public DateTime Birth
        {
            get;
            set;
        }

        public String[] Friends
        {
            get;
            set;
        }

        public Dictionary<String, NewPersonInfo> FriendsInfo
        {
            get;
            set;
        }

        public List<String> Schools
        {
            get;
            set;
        }
    }
}