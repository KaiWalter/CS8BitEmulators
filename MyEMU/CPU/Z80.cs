using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyEMU.CPU
{
#if CPU_TRACE
    internal struct CPUExecutionInfo
    {
        internal int Counter;
        internal UInt16 PC;
        internal string Bytes;
        internal string OpCode;
        internal string Op1;
        internal string Op2;
    }
#endif

    [Flags]
    public enum ZFLAGS : byte
    {
        S = 0x80,
        Z = 0x40,
        F5 = 0x20,
        H = 0x10,
        F3 = 0x08,
        PV = 0x04,
        N = 0x02,
        C = 0x01
    }

    public partial class Z80 : CPU8Bit
    {
        //http://www.bamafolks.com/randy/students/embedded/Z80_intro.html

        #region Registers
        // http://en.wikipedia.org/wiki/Zilog_Z80#Registers
        // Main registers
        byte m_reg_A;
        byte m_reg_B;
        byte m_reg_C;
        byte m_reg_D;
        byte m_reg_E;
        byte m_reg_H;
        byte m_reg_L;

        // Alternate registers
        byte m_reg_A_Alt;
        byte m_reg_Flags_Alt;
        byte m_reg_B_Alt;
        byte m_reg_C_Alt;
        byte m_reg_D_Alt;
        byte m_reg_E_Alt;
        byte m_reg_H_Alt;
        byte m_reg_L_Alt;

        // Index registers
        UInt16 m_reg_IX;
        UInt16 m_reg_IY;
        UInt16 m_reg_SP;

        bool m_Flag_S;
        bool m_Flag_Z;
        bool m_Flag_F5;
        bool m_Flag_H;
        bool m_Flag_F3;
        bool m_Flag_PV;
        bool m_Flag_N;
        bool m_Flag_C;

        // Other registers
        byte m_reg_I;       // interrupt vector
        byte m_reg_R;       // refresh counter
        bool m_IFF1;        // interupt enable maskable interupts
        bool m_IFF2;        // interupt enable non-maskable interupts
        byte m_IM;          // interrupt mode 0 / 1 / 2
        #endregion

        #region state / processing
        private InterruptSignal m_Interrupt;             // interrupt is requested
        private int m_OpCodeIndex;
        private int m_OpCodePrefix;
        private OperationCode m_oc;   // current Operation Code
        private Dictionary<int, int> m_dictOpCodePrefix;

        private int m_Cycle;          // current CPU cycle processed
        private int m_MaxCycles;      // max. cycles to process for OpCode
        private bool m_ExtraCycles;   // extra cycles are executed

        private byte m_byteValue;
        private UInt16 m_wordValue;
        private byte m_byteDisplacement;
        #endregion

        #region IN / OUT
        Dictionary<byte, ReceiveHandler> m_INHandler;
        Dictionary<byte, SendHandler> m_OUTHandler;
        #endregion

#if CPU_TRACE
        private CPUExecutionInfo m_ei;
        private Z80_Disassembler m_dis;
        private string[] m_regNames = { /*   0 */ "B", "C", "D", "E", "H", "L", "(HL)", "A",
                                        /*   8 */ "B", "C", "D", "E", "IX/l", "IY/l", "(IX+d)", "A",
                                        /*  16 */ "B", "C", "D", "E", "IY/l", "IY/l", "(IY+d)", "A",
                                        /*  24 */ "B'", "C'", "D'", "E'", "H'", "L'", "(HL')", "A'",
                                        /*  32 */ "B'", "C'", "D'", "E'", "IX/l", "IY/l", "(IX)", "A'",
                                        /*  40 */ "B'", "C'", "D'", "E'", "IY/l", "IY/l", "(IY)", "A'",
                                        /*  48 */ "BC", "DE", "HL", "SP",
                                        /*  52 */ "BC", "DE", "IX", "SP",
                                        /*  56 */ "BC", "DE", "IY", "SP",
                                      };
        private bool m_TraceActive = true;
#endif


        public Z80() : base()
        {
            initOpCodes();

            m_INHandler = new Dictionary<byte, ReceiveHandler>(10);
            m_OUTHandler = new Dictionary<byte, SendHandler>(10);

            precalcADCFlags();
            precalcSBCFlags();
            precalcLogFlags();
            precalcRotFlags(); 

#if CPU_TRACE
            m_ei = new CPUExecutionInfo();
            m_dis = new Z80_Disassembler();
#endif            

        }

        public void registerIOHandler(byte port,ReceiveHandler rcv,SendHandler snd)
        {
            if(rcv!=null) m_INHandler.Add(port,rcv);
            if(snd!=null) m_OUTHandler.Add(port,snd);
        }

        private void initOpCodes()
        {
            // ----------------------------------------------------------------------------------------------
            // reinitialize OpCode table for additional 4 Z80 prefixes - none, CB, DD, ED, FD 
            //                                                         - combinations DD, FD, CB
            m_OpCodes = new OperationCode[0x100 * 7];
            m_dictOpCodePrefix = new Dictionary<int, int>(6);
            m_dictOpCodePrefix.Add(0xCB, 0x100);
            m_dictOpCodePrefix.Add(0xDD, 0x200);
            m_dictOpCodePrefix.Add(0xED, 0x300);
            m_dictOpCodePrefix.Add(0xFD, 0x400);
            m_dictOpCodePrefix.Add(0xDDCB, 0x500);
            m_dictOpCodePrefix.Add(0xFDCB, 0x600);

            // ----------------------------------------------------------------------------------------------
            // OpCode initialization (ordered according to Zilog Z80 CPU Specifications by Sean Young)

            initOpCodes8BitLoadGroup();         // 8 bit Load group
            initOpCodes16BitLoadGroup();        // 16 bit Load group
            initOpCodesExchangeGroup();         // Exchange group
            initOpCodesBlockTransferGroup();    // Block Transfer group
            initOpCodesSearchGroup();           // Search group
            initOpCodes8BitArithLogGroup();     // 8 bit Arithmetic & Logical group
            initOpCodes16BitArithLogGroup();    // 16 bit Arithmetic & Logical group
            initOpCodesJumpGroup();             // Jump group
            initOpCodesCallAndReturnGroup();    // Call and Return group
            initOpCodesGenPurposeArithGroup();  // General Purpose Arithmetic group
            initOpCodesRotateAndShiftGroup();   // Rotate and Shift group
            initOpCodesCPUControlGroup();       // CPU control group
            initOpCodesBITManipulationGroup();  // Bit Manipulation group
            initOpCodesInputAndOutputGroup();   // Input and Output group

        }

        public override void Reset()
        {
            m_State = CPUState.fetchopcode;

            m_Interrupt = InterruptSignal.None;

            m_IFF1 = m_IFF2 = true;
            m_IM = 0;

            base.Reset();
        }

        public void signalInterrupt(InterruptSignal intsig)
        {
            if(intsig == InterruptSignal.NMI)
            {
                m_IFF1 = false;
                m_Interrupt = intsig;
            }
            if(intsig == InterruptSignal.IRQ && m_IFF1)
            {
                m_Interrupt = intsig;
            }
        }


        public void emulateCycle()
        {
            if (m_Interrupt != InterruptSignal.None)
                m_State = CPUState.interrupt;

            switch(m_State)
            {

                case CPUState.fetchopcode:
                    m_PC_Start = m_PC;
                    m_Cycle = 1;
                    m_ExtraCycles = false;
                    m_byteDisplacement = 0;

                    m_OpCodePrefix = 0;
                    m_OpCodeIndex = getNextMemByte();
                    m_oc = m_OpCodes[m_OpCodeIndex];

                    int iIndexOffset = 0;

                    if( m_OpCodeIndex == 0xCB ||   // evaluate prefix
                        m_OpCodeIndex == 0xDD ||
                        m_OpCodeIndex == 0xED ||
                        m_OpCodeIndex == 0xFD )
                    {
                        m_OpCodePrefix = m_OpCodeIndex;
                        iIndexOffset = m_dictOpCodePrefix[m_OpCodePrefix];
                        m_OpCodeIndex = getNextMemByte();

                        if(m_OpCodeIndex == 0xCB) // DD CB or FD CB combination
                        {
                            m_OpCodePrefix <<= 8;
                            m_OpCodePrefix |= m_OpCodeIndex;
                            iIndexOffset = m_dictOpCodePrefix[m_OpCodePrefix];
                            m_byteDisplacement = getNextMemByte(); // contains displacement before OpCode
                            m_OpCodeIndex = getNextMemByte();
                        }

                        m_oc = m_OpCodes[iIndexOffset + m_OpCodeIndex];
                    }

                    // check if OpCode is defined
                    if (String.IsNullOrEmpty(m_oc.OpCode) || m_oc.executeOperation == null)
                    {
                        string sMessage;
                        if(m_OpCodePrefix>0)
                            sMessage = String.Format("PC {0:X4} : OpCode {1:X2} {2:X2} unknown!!!", 
                                                        m_PC_Start, (byte)m_OpCodePrefix, (byte)m_OpCodeIndex);
                        else
                            sMessage = String.Format("PC {0:X4} : OpCode {1:X2} unknown!!!", 
                                                        m_PC_Start, (byte)m_OpCodeIndex);
                        throw new NotImplementedException(sMessage);
                    }

#if CPU_TRACE
                    m_ei.Counter++;
                    m_ei.PC = m_PC_Start;
                    m_ei.Bytes = "";
                    if (m_OpCodePrefix != 0) m_ei.Bytes += String.Format("{0:X2} ", m_OpCodePrefix);
                    m_ei.Bytes += String.Format("{0:X2} ", m_OpCodeIndex);
                    m_ei.OpCode = m_oc.OpCode;
                    m_ei.Op1 = "";
                    m_ei.Op2 = "";
#endif
                    // increase R register (only bits 6-0)
                    m_reg_R = (byte)((m_reg_R & 0x80) | ((m_reg_R + 1) & 0x7F));

                    // set for next processing state
                    m_wordValue = 0;
                    m_byteValue = 0;
#if NO_CYCLE_FILLUP
                    m_MaxCycles = m_PC - m_PC_Start;
#else
                    m_MaxCycles = m_oc.Cycles;
#endif
                    m_State = CPUState.addressing;
                    break;

                case CPUState.interrupt: // interrupt
                    switch(m_Interrupt)
                    {
                        case InterruptSignal.NMI:
                            // perform a RST 66h
                            m_reg_SP -= 2; ;
                            writeMemWord(m_reg_SP, m_PC);
                            m_PC = 0x0066;
                            m_State = CPUState.fetchopcode;
                            m_Interrupt = InterruptSignal.None;
                            break;

                        case InterruptSignal.IRQ:
                            switch(m_IM)
                            {
                                case 0:
                                    // TODO Z80 implement IM 0
                                    throw new NotImplementedException("Z80 IM 0 not implemented!");

                                case 1: // IM 1 - perform a RST 38h
                                    m_reg_SP -= 2; ;
                                    writeMemWord(m_reg_SP, m_PC);
                                    m_PC = 0x0038;
                                    m_State = CPUState.fetchopcode;
                                    m_Interrupt = InterruptSignal.None;
                                    break;

                                case 2: // IM 2 - jump to interrupt vector
                                    m_reg_SP -= 2; ;
                                    writeMemWord(m_reg_SP, m_PC);
                                    m_PC = m_reg_I;
                                    m_State = CPUState.fetchopcode;
                                    m_Interrupt = InterruptSignal.None;
                                    break;
                            }
                            break;
                    }
                    break;

                case CPUState.addressing: // addressing
                    m_Cycle++;
                    m_oc.executeAddressing();

                    break;

                case CPUState.operation:
                    if (m_Cycle >= m_MaxCycles) // emulate cycle exactness by making operation effective on last cycle
                    {
                        m_oc.executeOperation();
                        if (m_Cycle < m_MaxCycles)
                            m_Cycle++; // operation executed induced another cycle (e.g. branch operations)
                        else
                            m_State = CPUState.fetchopcode;  // operation finished -> get next opcode

#if CPU_TRACE
                        //if (m_PC_Start == 0x188) { m_TraceActive = true; m_ei.Counter = 1; }
                        //if (m_PC_Start == 0x18B) m_TraceActive = false;

                        if (m_State == CPUState.fetchopcode && m_TraceActive)
                        {
                            StringBuilder sb = new StringBuilder();

                            if ((m_ei.Counter % 20) == 1)
                            {
                                sb.Clear();
                                sb.Append("PC-- ");
                                sb.Append("----------- OpCo Op1,Op2---|");
                                sb.Append("A  F  B  C  D  E  H  L  ");
                                //sb.Append("A' F' B' C' D' E' H' L' ");
                                sb.Append("IX   IY   SP   I  R  |");
                                sb.Append("SZ5H3vNC|");
                                sb.Append("Disassembly Check---|");
                                sb.Append("STACK ---------------");
                                Debug.WriteLine(sb.ToString());
                            }
                            sb.Clear();
                            sb.AppendFormat("{0:X4} ", m_ei.PC);
                            sb.Append(m_ei.Bytes.PadRight(12));
                            sb.Append(m_ei.OpCode.PadRight(5));
                            if (!String.IsNullOrEmpty(m_ei.Op1) && !String.IsNullOrEmpty(m_ei.Op2))
                                sb.Append((m_ei.Op1 + "," + m_ei.Op2).PadRight(10));
                            else
                                sb.Append((m_ei.Op1 + m_ei.Op2).PadRight(10));
                            sb.Append("|");

                            sb.AppendFormat("{0:X2} ", m_reg_A);
                            sb.AppendFormat("{0:X2} ", Flags);
                            sb.AppendFormat("{0:X2} ", m_reg_B);
                            sb.AppendFormat("{0:X2} ", m_reg_C);
                            sb.AppendFormat("{0:X2} ", m_reg_D);
                            sb.AppendFormat("{0:X2} ", m_reg_E);
                            sb.AppendFormat("{0:X2} ", m_reg_H);
                            sb.AppendFormat("{0:X2} ", m_reg_L);

                            //sb.AppendFormat("{0:X2} ", m_reg_A_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_Flags_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_B_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_C_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_D_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_E_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_H_Alt);
                            //sb.AppendFormat("{0:X2} ", m_reg_L_Alt);

                            sb.AppendFormat("{0:X4} ", m_reg_IX);
                            sb.AppendFormat("{0:X4} ", m_reg_IY);
                            sb.AppendFormat("{0:X4} ", m_reg_SP);
                            sb.AppendFormat("{0:X2} ", m_reg_I);
                            sb.AppendFormat("{0:X2} ", m_reg_R);
                            sb.Append("|");

                            sb.Append(Convert.ToString(Flags, 2).PadLeft(8, '0'));
                            sb.Append("|");

                            string strDisassemblyCheck = m_dis.Disassemble(() => readMemByte(m_ei.PC++));
                            sb.Append(strDisassemblyCheck.PadRight(20));
                            sb.Append("|");

                            for (int spDump = m_reg_SP; spDump <= m_reg_SP + 5; spDump++ )
                                sb.AppendFormat("{0:X2} ", readMemByte((UInt16)spDump));

                            Debug.WriteLine(sb.ToString());
                            sb = null;
                        }
#endif
                    }
                    else
                        m_Cycle++;
                    break;
            }
        }

        #region Register
        public UInt16 AF
        {
            get
            {
                return (UInt16)(m_reg_A << 8 | Flags);
            }
            set
            {
                m_reg_A = (byte)(value >> 8);
                Flags = (byte)(value & 0xFF);
            }
        }

        UInt16 AF_Alt
        {
            get
            {
                return (UInt16)(m_reg_A_Alt << 8 | m_reg_Flags_Alt);
            }
            set
            {
                m_reg_A_Alt = (byte)(value >> 8);
                m_reg_Flags_Alt = (byte)(value & 0xFF);
            }
        }

        public UInt16 BC
        {
            get
            {
                return (UInt16)(m_reg_B << 8 | m_reg_C);
            }
            set
            {
                m_reg_B = (byte)(value >> 8);
                m_reg_C = (byte)(value & 0xFF);
            }
        }

        UInt16 BC_Alt
        {
            get
            {
                return (UInt16)(m_reg_B_Alt << 8 | m_reg_C_Alt);
            }
            set
            {
                m_reg_B_Alt = (byte)(value >> 8);
                m_reg_C_Alt = (byte)(value & 0xFF);
            }
        }

        public UInt16 DE
        {
            get
            {
                return (UInt16)(m_reg_D << 8 | m_reg_E);
            }
            set
            {
                m_reg_D = (byte)(value >> 8);
                m_reg_E = (byte)(value & 0xFF);
            }
        }

        UInt16 DE_Alt
        {
            get
            {
                return (UInt16)(m_reg_D_Alt << 8 | m_reg_E_Alt);
            }
            set
            {
                m_reg_D_Alt = (byte)(value >> 8);
                m_reg_E_Alt = (byte)(value & 0xFF);
            }
        }

        public UInt16 HL
        {
            get
            {
                return (UInt16)(m_reg_H << 8 | m_reg_L);
            }
            set
            {
                m_reg_H = (byte)(value >> 8);
                m_reg_L = (byte)(value & 0xFF);
            }
        }

        UInt16 HL_Alt
        {
            get
            {
                return (UInt16)(m_reg_H_Alt << 8 | m_reg_L_Alt);
            }
            set
            {
                m_reg_H_Alt = (byte)(value >> 8);
                m_reg_L_Alt = (byte)(value & 0xFF);
            }
        }

        byte IX_H
        {
            get
            {
                return (byte)((m_reg_IX >> 8) & 0xFF);
            }
            set
            {
                m_reg_IX &= 0x00FF;
                m_reg_IX |= (UInt16)(value << 8);
            }
        }

        byte IX_L
        {
            get
            {
                return (byte)(m_reg_IX & 0xFF);
            }
            set
            {
                m_reg_IX &= 0xFF00;
                m_reg_IX |= (UInt16)value;
            }
        }
        byte IY_H
        {
            get
            {
                return (byte)((m_reg_IY >> 8) & 0xFF);
            }
            set
            {
                m_reg_IY &= 0x00FF;
                m_reg_IY |= (UInt16)(value << 8);
            }
        }

        byte IY_L
        {
            get
            {
                return (byte)(m_reg_IY & 0xFF);
            }
            set
            {
                m_reg_IY &= 0xFF00;
                m_reg_IY |= (UInt16)value;
            }
        }

        public UInt16 PC
        {
            get { return m_PC; }
            set { m_PC = value; }
        }
        public UInt16 SP
        {
            get { return m_reg_SP; }
            set { m_reg_SP = value; }
        }
        #endregion

        #region Flags
        private void setUnusedFlags(byte b)
        {
            m_Flag_F5 = (b & b00100000) == b00100000;
            m_Flag_F3 = (b & b00001000) == b00001000;
        }

        private void setFlagS(byte b)
        {
            m_Flag_S = (b & b10000000) == b10000000;
        }

        private void setFlagZ(byte b)
        {
            m_Flag_Z = b == 0;
        }

        private void checkParity(byte b)
        {
            byte[] numberAsByte = new byte[] { b };
            BitArray bits = new System.Collections.BitArray(numberAsByte);

            int sumBit1 = 0;
            for (int i = 0; i < 8; i++)
                if (bits[i]) sumBit1++;

            m_Flag_PV = (sumBit1 % 2) == 0;
        }

        private byte Flags
        {
            get
            {
                byte fl = 0;
                if (m_Flag_S) fl += (byte)ZFLAGS.S;
                if (m_Flag_Z) fl += (byte)ZFLAGS.Z;
                if (m_Flag_F5) fl += (byte)ZFLAGS.F5;
                if (m_Flag_H) fl += (byte)ZFLAGS.H;
                if (m_Flag_F3) fl += (byte)ZFLAGS.F3;
                if (m_Flag_PV) fl += (byte)ZFLAGS.PV;
                if (m_Flag_N) fl += (byte)ZFLAGS.N;
                if (m_Flag_C) fl += (byte)ZFLAGS.C;
                return fl;
            }

            set
            {
                m_Flag_S = (value & (byte)ZFLAGS.S) != 0;
                m_Flag_Z = (value & (byte)ZFLAGS.Z) != 0;
                m_Flag_F5 = (value & (byte)ZFLAGS.F5) != 0;
                m_Flag_H = (value & (byte)ZFLAGS.H) != 0;
                m_Flag_F3 = (value & (byte)ZFLAGS.F3) != 0;
                m_Flag_PV = (value & (byte)ZFLAGS.PV) != 0;
                m_Flag_N = (value & (byte)ZFLAGS.N) != 0;
                m_Flag_C = (value & (byte)ZFLAGS.C) != 0;
            }
        }
        #endregion

        #region Addressing Modes
        private void _ADDR_IMP() // implied
        {
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_DISP_REG() // register (fetch displacement)
        {
            m_byteDisplacement = getNextMemByte();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} ", m_byteDisplacement);
#endif
            m_oc.executeAddressing = _ADDR_REG;
        }

        private void _ADDR_REG() // register
        {
            int reg = m_OpCodeIndex & 7;

            switch (reg)
            {
                case 0: m_byteValue = m_reg_B; break;
                case 1: m_byteValue = m_reg_C; break;
                case 2: m_byteValue = m_reg_D; break;
                case 3: m_byteValue = m_reg_E; break;
                case 4: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            m_byteValue = IX_H;
                            break;
                        case 0xFD:
                            m_byteValue = IY_H;
                            break;
                        default:
                            m_byteValue = m_reg_H;
                            break;
                    }
                    break;
                case 5: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            m_byteValue = IX_L;
                            break;
                        case 0xFD:
                            m_byteValue = IY_L;
                            break;
                        default:
                            m_byteValue = m_reg_L;
                            break;
                    }
                    break;
                case 7: m_byteValue = m_reg_A; break;
                default:
                    string sMessage = String.Format("illegal addressing {0:X1}", reg);
                    throw new ApplicationException(sMessage);
            }

#if CPU_TRACE
            int offset = 0;
            if (m_OpCodePrefix == 0xDD) offset += 8;
            if (m_OpCodePrefix == 0xFD) offset += 16;
            m_ei.Op2 = m_regNames[offset + reg];
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_DISP_IMM() // immediate (fetch displacement)
        {
            m_byteDisplacement = getNextMemByte();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} ", m_byteDisplacement);
#endif
            m_oc.executeAddressing = _ADDR_IMM;
        }

        private void _ADDR_IMM() // immediate
        {
            m_byteValue = getNextMemByte();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} ", m_byteValue);
            m_ei.Op2 = String.Format("{0:X2}", m_byteValue);
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }
        private void _ADDR_IMMEXT() // immediate extended
        {
            m_wordValue = getNextMemWord();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} {1:X2}", (byte)(m_wordValue & 0xFF), (m_wordValue >> 8));
            m_ei.Op2 = String.Format("{0:X4}", m_wordValue);
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }
        private void _ADDR_EXT1_8Bit() // extended
        {
            m_wordValue = getNextMemWord();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} {1:X2}", (byte)(m_wordValue & 0xFF), (m_wordValue >> 8));
            m_ei.Op2 = String.Format("({0:X4})", m_wordValue);
#endif
            m_oc.executeAddressing = _ADDR_EXT2_8Bit;
        }

        private void _ADDR_EXT2_8Bit() // extended
        {
            m_byteValue = readMemByte(m_wordValue);
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_EXT1_16Bit() // extended
        {
            m_wordValue = getNextMemWord();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} {1:X2}", (byte)(m_wordValue & 0xFF), (m_wordValue >> 8));
            m_ei.Op2 = String.Format("({0:X4})", m_wordValue);
#endif
            m_oc.executeAddressing = _ADDR_EXT2_16Bit;
        }

        private void _ADDR_EXT2_16Bit() // extended
        {
            m_wordValue = readMemWord(m_wordValue);
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_REGIND_HL() // register indirect HL
        {
            m_byteValue = readMemByte(HL);
#if CPU_TRACE
            m_ei.Op2 = "(HL)";
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_REGIND_BC() // register indirect BC
        {
            m_byteValue = readMemByte(BC);
#if CPU_TRACE
            m_ei.Op2 = "(BC)";
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_REGIND_DE() // register indirect DE
        {
            m_byteValue = readMemByte(DE);
#if CPU_TRACE
            m_ei.Op2 = "(DE)";
#endif
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_IDX() // indexed
        {
            UInt16 address = 0;
            m_byteDisplacement = getNextMemByte();

#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} ", m_byteDisplacement);
#endif

            switch (m_OpCodePrefix)
            {
                case 0xDD:
                    address = m_reg_IX;
#if CPU_TRACE
                    m_ei.Op2 = "(IX+d)";
#endif
                    break;
                case 0xFD:
                    address = m_reg_IY;
#if CPU_TRACE
                    m_ei.Op2 = "(IY+d)";
#endif
                    break;
                default:
                    throw new ApplicationException(String.Format("illegal addressing {0:X2}", m_OpCodePrefix));
            }

            address += m_byteDisplacement;
            m_byteValue = readMemByte(address);
            m_State = CPUState.operation; // addressing finished -> execute operation
        }
        #endregion

    }
}
