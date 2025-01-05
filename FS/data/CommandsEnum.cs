using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS.data
{
    // Defining the name of the command and the parameters needed to be run successfully
    public enum CommandsEnum
    {
        cpin,
        ls = 1,
        rm = 2,
        cpout,
        cp = 3, // for cpin and cpout which has the same value
        md,
        cd,
        rd
    }
}
