using ExceptionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewWPF
{
    public class C3
    {
        D4 list = new D4();
        public string HZ()
        {
            Ex.Throw("state=c3",()=>list.get());
            return list.get().ToString();
        }
    }
}
