using Delly.Refer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReferDemo.Entity
{
    [UseRefer]
    public partial class NameValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
