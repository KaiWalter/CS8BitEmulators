using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEMU.CPU;

namespace MyEMU.I_O
{
    /// <summary>
    /// Universal implementation of MOS6520 (derived 1:1 from MC6820)
    /// </summary>
    class MOS6520 : MC6820
    {
        public MOS6520(string Designation, MOS6502 cpu, UInt16 BaseAdress) : base(Designation, cpu, BaseAdress) { }
    }
}
