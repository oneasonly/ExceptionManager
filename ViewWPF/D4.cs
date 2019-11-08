using ExceptionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewWPF
{
    class D4
    {
        int count = 0;
        decimal money;
        public decimal get()
        {
            
            //throw new Exception("кидаю вручную throw");
            Ex.Throw("кидаю менеджером throw");
            return money = Decimal.Parse("3.13get");
        }
    }
}
