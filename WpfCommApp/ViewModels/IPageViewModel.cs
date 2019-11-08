using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public interface IPageViewModel
    {
        bool Completed { get; }
        string Name { get; }
    }
}
