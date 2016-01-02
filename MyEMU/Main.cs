using MyEMU.CPU;
using System.Runtime.InteropServices;
using MyEMU.Systems;
using System;
using System.Diagnostics;

namespace MyEMU
{
    class EmulatorHost
    {
        static void Main(string[] args)
        {
            string sSystem; 
            if (args.Length == 0)
            {
                ISystemBase system = new MCP();
                EmulatorScreen screen = new EmulatorScreen(system);
                return;
            }
            else
            {
                sSystem = args[0].ToUpper();
            }

            Emulator(sSystem);
        }

        /// <summary>
        /// Apple1:
        /// http://myapplecomputer.net/Apple-1/Apple-1_Specs.html
        /// http://www.applefritter.com/book/export/html/22
        /// 
        /// PET:
        /// source of ROMs:
        /// http://www.zimmers.net/anonftp/pub/cbm/firmware/computers/pet/
        /// 
        /// EHBASIC 6502 - simple basic interpreter
        /// http://codegolf.stackexchange.com/questions/12844/emulate-a-mos-6502-cpu
        /// </summary>
        static void Emulator(string sSystem)
        {
            ISystemBase system = null;

            switch(sSystem)
            {
                case "APPLE1":
                    system = new Apple1();
                    break;

                case "CBM2001N_B1":
                    system = new CBM2001N_B1();
                    break;

                case "CBM2001N":
                case "CBM2001N_B2":
                    system = new CBM2001N_B2();
                    break;

                case "EHBASIC":
                    system = new EHBASIC6502();
                    break;

                case "TRS80M1":
                    system = new TRS80M1();
                    break;

                case "ZX80":
                    system = new ZX80();
                    break;

                case "SIMPLEZ80":
                    system = new SimpleZ80();
                    break;

                default:
                    throw new ApplicationException("Emulator not implemented:" + sSystem);
            }

            EmulatorScreen screen = new EmulatorScreen(system);

        }

    }
}
