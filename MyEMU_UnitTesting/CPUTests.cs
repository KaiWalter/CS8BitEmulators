using System;
using System.Text;
using System.IO;
using MyEMU;
using MyEMU.CPU;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace MyEMU_UnitTesting
{
    [TestClass]
    public class CPUTests
    {
        [TestMethod]
        /// <summary>
        /// test MOS6502
        /// http://codegolf.stackexchange.com/questions/12844/emulate-a-mos-6502-cpu
        /// </summary>
        public void MOS6502_AllSuiteA()
        {
            MOS6502 cpu = new MOS6502();

            cpu.installRAMBank(64);

            cpu.loadProgram("ROMs\\MOS6502_AllSuiteA.bin", 0x4000);

            // point reset vector to test routine
            cpu.writeMemByte(0xFFFC, 0x00);
            cpu.writeMemByte(0xFFFD, 0x40);

            cpu.Reset();

            cpu.Flag_B = true;

            cpu.runUntil(0x45C0);

            byte bResult = cpu.readMemByte(0x0210);

            Assert.AreEqual(0xFF, bResult);
            Assert.AreEqual(0x45C0, cpu.PC);
        }

        [TestMethod]
        public void Z80_ZEXALL()
        {
            Z80_ZEX("Binaries\\zexall.com");
        }

        [TestMethod]
        public void Z80_ZEXDOC()
        {
            Z80_ZEX("Binaries\\zexdoc.com");
        }

        private void Z80_ZEX(string sFilename)
        {
            Z80 cpu = new Z80();

            cpu.installRAMBank(0, 64);

            // SET CPM PSP
            cpu.writeMemWord(0x0006, 0x8000); // SP to 0x8000

            cpu.loadProgram(sFilename, 0x100);

            cpu.Reset();
            cpu.PC = 0x100;


            // PATCH START AND END TEST CASES ------------------------
            int iTestNo = 12;
            int iTestCases = 25;
            ushort uiFirstTest = (ushort)(0x13A + (iTestNo * 2));
            cpu.writeMemWord(0x120, uiFirstTest);
            ushort uiStopTest = (ushort)(uiFirstTest + (iTestCases * 2));
            cpu.writeMemWord(uiStopTest, 0);
            //// PATCH START AND END TEST CASES ------------------------

            bool bRun = true;

            int iCurrentTest = iTestNo;
            int iOKCounter = 0;
            int iERRORCounter = 0;

            while (bRun)
            {
                if (cpu.PC == 0) // assume WARM RESTART
                {
                    bRun = false;
                }

                if (cpu.PC == 0x0005)
                    switch (cpu.BC & 0x00FF)
                    {
                        case 0x02: // fake CPM console char output
                            Debug.Write((char)(cpu.DE & 0x00FF));
                            cpu.PC = cpu.readMemWord(cpu.SP); // simulate RET
                            cpu.SP += 2;
                            break;

                        case 0x09: // fake CPM console string output
                            int iStringStart = cpu.DE;

                            StringBuilder sb = new StringBuilder();

                            int index = 0;
                            byte b = cpu.readMemByte((ushort)(iStringStart + index));

                            while (b != 0x24)
                            {
                                sb.Append((char)b);
                                b = cpu.readMemByte((ushort)(iStringStart + ++index));
                            }

                            string sMessage = sb.ToString();

                            // evaluate message
                            if(!String.IsNullOrEmpty(sMessage) &&
                               ( sMessage.StartsWith("  OK") || sMessage.StartsWith("  ERROR")))
                            {
                                Debug.Write(String.Format("{0}:", iCurrentTest++));
                                if (sMessage.StartsWith("  OK")) iOKCounter++;
                                if (sMessage.StartsWith("  ERROR")) iERRORCounter++;
                            }

                            Debug.Write(sMessage);

                            cpu.PC = cpu.readMemWord(cpu.SP); // simulate RET
                            cpu.SP += 2;
                            break;
                    }

                cpu.emulateCycle();

#if CPU_TRACE
                if (cpu.State == CPUState.fetchopcode)
                {
                    string Test = "DEBUG INSTRUCTIONS HERE";
                }
#endif
            }


            Assert.AreEqual(0, iERRORCounter, "ZEXALL - error occured");
        }

    }
}
