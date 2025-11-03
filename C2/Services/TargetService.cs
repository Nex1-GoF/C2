using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace C2.Services
{
    class TargetService
    {
        private static TargetService _instance;
        public static TargetService Instance => _instance ??= new TargetService();

        private TargetService()
        {
        }
    }
}
