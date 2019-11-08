using ExceptionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewWPF
{
    public class B2
    {
        private C3 list = new C3();
        public void SomeH()
        {
            Ex.Throw("state=b2", () => list.HZ());
        }
    }
}
