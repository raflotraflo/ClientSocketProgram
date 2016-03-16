using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public interface IMapper<T, h>
    {
        h Map(T input);

        T InverseMap(h input);
    }
    //zmiana bezposrednio w repozytorium
}
