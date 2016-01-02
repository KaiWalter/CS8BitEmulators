using System;
using System.Diagnostics;
using System.Text;

namespace MyEMU.CPU
{
    public class MOS6502 : MOS65xxx
    {
        private byte m_reg_A;         // reg_A Register
        private byte m_reg_X;         // reg_X Register
        private byte m_reg_Y;         // reg_Y Register
        private byte m_reg_SR;        // Status Register
        private UInt16 m_StackPtr;    // Stack Pointer (absolute address x0100 - x01FF)

        private OperationCode m_oc;   // current Operation Code
        private bool m_Interrupt;     // process interrupt
        private int m_Cycle;          // current CPU cycle processed
        private int m_MaxCycles;      // max. cycles to process for OpCode
        private Addressing m_addr;    // current Addressing Mode
        private UInt16 m_address;     // current absolute address determined
        private byte m_OperandValue;  // current OperandValue determined

#if CPU_TRACE
        private StringBuilder m_sbDebug;  // build operations debug string
#endif

        public MOS6502() : base()
        {
            // initialize OpCode table
            // reference see http://homepage.ntlworld.com/cyborgsystems/CS_Main/6502/6502.htm
            m_OpCodes[0x00] = new OperationCode("BRK", 1, 7, _ADDR_IMP, _BRK);
            m_OpCodes[0x01] = new OperationCode("ORA", 2, 6, _ADDR_INDX_1, _ORA);
            m_OpCodes[0x05] = new OperationCode("ORA", 2, 3, _ADDR_ZP, _ORA);
            m_OpCodes[0x06] = new OperationCode("ASL", 2, 5, _ADDR_ZP, _ASL);
            m_OpCodes[0x08] = new OperationCode("PHP", 1, 3, _ADDR_IMP, _PHP);
            m_OpCodes[0x09] = new OperationCode("ORA", 2, 2, _ADDR_IMM, _ORA);
            m_OpCodes[0x0a] = new OperationCode("ASL", 1, 2, _ADDR_ACC, _ASL);
            m_OpCodes[0x0d] = new OperationCode("ORA", 3, 4, _ADDR_ABS, _ORA);
            m_OpCodes[0x0e] = new OperationCode("ASL", 3, 6, _ADDR_ABS, _ASL);
            m_OpCodes[0x10] = new OperationCode("BPL", 2, 2, _ADDR_REL_1, _BPL);
            m_OpCodes[0x11] = new OperationCode("ORA", 2, 5, _ADDR_INDY_1, _ORA);
            m_OpCodes[0x15] = new OperationCode("ORA", 2, 4, _ADDR_ZPX_1, _ORA);
            m_OpCodes[0x16] = new OperationCode("ASL", 2, 6, _ADDR_ZPX_1, _ASL);
            m_OpCodes[0x18] = new OperationCode("CLC", 1, 2, _ADDR_IMP, _CLC);
            m_OpCodes[0x19] = new OperationCode("ORA", 3, 4, _ADDR_ABSY_1, _ORA);
            m_OpCodes[0x1d] = new OperationCode("ORA", 3, 4, _ADDR_ABSX_1, _ORA);
            m_OpCodes[0x1e] = new OperationCode("ASL", 3, 7, _ADDR_ABSX_1, _ASL);
            m_OpCodes[0x20] = new OperationCode("JSR", 3, 6, _ADDR_ABS, _JSR);
            m_OpCodes[0x21] = new OperationCode("AND", 2, 6, _ADDR_INDX_1, _AND);
            m_OpCodes[0x24] = new OperationCode("BIT", 2, 3, _ADDR_ZP, _BIT);
            m_OpCodes[0x25] = new OperationCode("AND", 2, 3, _ADDR_ZP, _AND);
            m_OpCodes[0x26] = new OperationCode("ROL", 2, 5, _ADDR_ZP, _ROL);
            m_OpCodes[0x28] = new OperationCode("PLP", 1, 4, _ADDR_IMP, _PLP);
            m_OpCodes[0x29] = new OperationCode("AND", 2, 2, _ADDR_IMM, _AND);
            m_OpCodes[0x2a] = new OperationCode("ROL", 1, 2, _ADDR_ACC, _ROL);
            m_OpCodes[0x2c] = new OperationCode("BIT", 3, 4, _ADDR_ABS, _BIT);
            m_OpCodes[0x2d] = new OperationCode("AND", 3, 4, _ADDR_ABS, _AND);
            m_OpCodes[0x2e] = new OperationCode("ROL", 3, 6, _ADDR_ABS, _ROL);
            m_OpCodes[0x30] = new OperationCode("BMI", 2, 2, _ADDR_REL_1, _BMI);
            m_OpCodes[0x31] = new OperationCode("AND", 2, 5, _ADDR_INDY_1, _AND);
            m_OpCodes[0x35] = new OperationCode("AND", 2, 4, _ADDR_ZPX_1, _AND);
            m_OpCodes[0x36] = new OperationCode("ROL", 2, 6, _ADDR_ZPX_1, _ROL);
            m_OpCodes[0x38] = new OperationCode("SEC", 1, 2, _ADDR_IMP, _SEC);
            m_OpCodes[0x39] = new OperationCode("AND", 3, 4, _ADDR_ABSY_1, _AND);
            m_OpCodes[0x3d] = new OperationCode("AND", 3, 4, _ADDR_ABSX_1, _AND);
            m_OpCodes[0x3e] = new OperationCode("ROL", 3, 7, _ADDR_ABSX_1, _ROL);
            m_OpCodes[0x40] = new OperationCode("RTI", 1, 6, _ADDR_IMP, _RTI);
            m_OpCodes[0x41] = new OperationCode("EOR", 2, 6, _ADDR_INDX_1, _EOR);
            m_OpCodes[0x45] = new OperationCode("EOR", 2, 3, _ADDR_ZP, _EOR);
            m_OpCodes[0x46] = new OperationCode("LSR", 2, 5, _ADDR_ZP, _LSR);
            m_OpCodes[0x48] = new OperationCode("PHA", 1, 3, _ADDR_IMP, _PHA);
            m_OpCodes[0x49] = new OperationCode("EOR", 2, 2, _ADDR_IMM, _EOR);
            m_OpCodes[0x4a] = new OperationCode("LSR", 1, 2, _ADDR_ACC, _LSR);
            m_OpCodes[0x4c] = new OperationCode("JMP", 3, 3, _ADDR_ABS, _JMP);
            m_OpCodes[0x4d] = new OperationCode("EOR", 3, 4, _ADDR_ABS, _EOR);
            m_OpCodes[0x4e] = new OperationCode("LSR", 3, 6, _ADDR_ABS, _LSR);
            m_OpCodes[0x50] = new OperationCode("BVC", 2, 2, _ADDR_REL_1, _BVC);
            m_OpCodes[0x51] = new OperationCode("EOR", 2, 5, _ADDR_INDY_1, _EOR);
            m_OpCodes[0x55] = new OperationCode("EOR", 2, 4, _ADDR_ZPX_1, _EOR);
            m_OpCodes[0x56] = new OperationCode("LSR", 2, 6, _ADDR_ZPX_1, _LSR);
            m_OpCodes[0x58] = new OperationCode("CLI", 1, 2, _ADDR_IMP, _CLI);
            m_OpCodes[0x59] = new OperationCode("EOR", 3, 4, _ADDR_ABSY_1, _EOR);
            m_OpCodes[0x5d] = new OperationCode("EOR", 3, 4, _ADDR_ABSX_1, _EOR);
            m_OpCodes[0x5e] = new OperationCode("LSR", 3, 7, _ADDR_ABSX_1, _LSR);
            m_OpCodes[0x60] = new OperationCode("RTS", 1, 6, _ADDR_IMP, _RTS);
            m_OpCodes[0x61] = new OperationCode("ADC", 2, 6, _ADDR_INDX_1, _ADC);
            m_OpCodes[0x65] = new OperationCode("ADC", 2, 3, _ADDR_ZP, _ADC);
            m_OpCodes[0x66] = new OperationCode("ROR", 2, 5, _ADDR_ZP, _ROR);
            m_OpCodes[0x68] = new OperationCode("PLA", 1, 4, _ADDR_IMP, _PLA);
            m_OpCodes[0x69] = new OperationCode("ADC", 2, 2, _ADDR_IMM, _ADC);
            m_OpCodes[0x6a] = new OperationCode("ROR", 1, 2, _ADDR_ACC, _ROR);
            m_OpCodes[0x6c] = new OperationCode("JMP", 3, 5, _ADDR_IND_1, _JMP);
            m_OpCodes[0x6d] = new OperationCode("ADC", 3, 4, _ADDR_ABS, _ADC);
            m_OpCodes[0x6e] = new OperationCode("ROR", 3, 6, _ADDR_ABS, _ROR);
            m_OpCodes[0x70] = new OperationCode("BVS", 2, 2, _ADDR_REL_1, _BVS);
            m_OpCodes[0x71] = new OperationCode("ADC", 2, 5, _ADDR_INDY_1, _ADC);
            m_OpCodes[0x75] = new OperationCode("ADC", 2, 4, _ADDR_ZPX_1, _ADC);
            m_OpCodes[0x76] = new OperationCode("ROR", 2, 6, _ADDR_ZPX_1, _ROR);
            m_OpCodes[0x78] = new OperationCode("SEI", 1, 2, _ADDR_IMP, _SEI);
            m_OpCodes[0x79] = new OperationCode("ADC", 3, 4, _ADDR_ABSY_1, _ADC);
            m_OpCodes[0x7d] = new OperationCode("ADC", 3, 4, _ADDR_ABSX_1, _ADC);
            m_OpCodes[0x7e] = new OperationCode("ROR", 3, 7, _ADDR_ABSX_1, _ROR);
            m_OpCodes[0x81] = new OperationCode("STA", 2, 6, _ADDR_INDX_1, _STA);
            m_OpCodes[0x84] = new OperationCode("STY", 2, 3, _ADDR_ZP, _STY);
            m_OpCodes[0x85] = new OperationCode("STA", 2, 3, _ADDR_ZP, _STA);
            m_OpCodes[0x86] = new OperationCode("STX", 2, 3, _ADDR_ZP, _STX);
            m_OpCodes[0x88] = new OperationCode("DEY", 1, 2, _ADDR_IMP, _DEY);
            m_OpCodes[0x8a] = new OperationCode("TXA", 1, 2, _ADDR_IMP, _TXA);
            m_OpCodes[0x8c] = new OperationCode("STY", 3, 4, _ADDR_ABS, _STY);
            m_OpCodes[0x8d] = new OperationCode("STA", 3, 4, _ADDR_ABS, _STA);
            m_OpCodes[0x8e] = new OperationCode("STX", 3, 4, _ADDR_ABS, _STX);
            m_OpCodes[0x90] = new OperationCode("BCC", 2, 2, _ADDR_REL_1, _BCC);
            m_OpCodes[0x91] = new OperationCode("STA", 2, 6, _ADDR_INDY_1, _STA);
            m_OpCodes[0x94] = new OperationCode("STY", 2, 4, _ADDR_ZPX_1, _STY);
            m_OpCodes[0x95] = new OperationCode("STA", 2, 4, _ADDR_ZPX_1, _STA);
            m_OpCodes[0x96] = new OperationCode("STX", 2, 4, _ADDR_ZPY_1, _STX);
            m_OpCodes[0x98] = new OperationCode("TYA", 1, 2, _ADDR_IMP, _TYA);
            m_OpCodes[0x99] = new OperationCode("STA", 3, 5, _ADDR_ABSY_1, _STA);
            m_OpCodes[0x9a] = new OperationCode("TXS", 1, 2, _ADDR_IMP, _TXS);
            m_OpCodes[0x9d] = new OperationCode("STA", 3, 5, _ADDR_ABSX_1, _STA);
            m_OpCodes[0xa0] = new OperationCode("LDY", 2, 2, _ADDR_IMM, _LDY);
            m_OpCodes[0xa1] = new OperationCode("LDA", 2, 6, _ADDR_INDX_1, _LDA);
            m_OpCodes[0xa2] = new OperationCode("LDX", 2, 2, _ADDR_IMM, _LDX);
            m_OpCodes[0xa4] = new OperationCode("LDY", 2, 3, _ADDR_ZP, _LDY);
            m_OpCodes[0xa5] = new OperationCode("LDA", 2, 3, _ADDR_ZP, _LDA);
            m_OpCodes[0xa6] = new OperationCode("LDX", 2, 3, _ADDR_ZP, _LDX);
            m_OpCodes[0xa8] = new OperationCode("TAY", 1, 2, _ADDR_IMP, _TAY);
            m_OpCodes[0xa9] = new OperationCode("LDA", 2, 2, _ADDR_IMM, _LDA);
            m_OpCodes[0xaa] = new OperationCode("TAX", 1, 2, _ADDR_IMP, _TAX);
            m_OpCodes[0xac] = new OperationCode("LDY", 3, 4, _ADDR_ABS, _LDY);
            m_OpCodes[0xad] = new OperationCode("LDA", 3, 4, _ADDR_ABS, _LDA);
            m_OpCodes[0xae] = new OperationCode("LDX", 3, 4, _ADDR_ABS, _LDX);
            m_OpCodes[0xB0] = new OperationCode("BCS", 2, 2, _ADDR_REL_1, _BCS);
            m_OpCodes[0xb1] = new OperationCode("LDA", 2, 5, _ADDR_INDY_1, _LDA);
            m_OpCodes[0xb4] = new OperationCode("LDY", 2, 4, _ADDR_ZPX_1, _LDY);
            m_OpCodes[0xb5] = new OperationCode("LDA", 2, 4, _ADDR_ZPX_1, _LDA);
            m_OpCodes[0xb6] = new OperationCode("LDX", 2, 4, _ADDR_ZPY_1, _LDX);
            m_OpCodes[0xb8] = new OperationCode("CLV", 1, 2, _ADDR_IMP, _CLV);
            m_OpCodes[0xb9] = new OperationCode("LDA", 3, 4, _ADDR_ABSY_1, _LDA);
            m_OpCodes[0xba] = new OperationCode("TSX", 1, 2, _ADDR_IMP, _TSX);
            m_OpCodes[0xbc] = new OperationCode("LDY", 3, 4, _ADDR_ABSX_1, _LDY);
            m_OpCodes[0xbd] = new OperationCode("LDA", 3, 4, _ADDR_ABSX_1, _LDA);
            m_OpCodes[0xbe] = new OperationCode("LDX", 3, 4, _ADDR_ABSY_1, _LDX);
            m_OpCodes[0xc0] = new OperationCode("CPY", 2, 2, _ADDR_IMM, _CPY);
            m_OpCodes[0xc1] = new OperationCode("CMP", 2, 6, _ADDR_INDX_1, _CMP);
            m_OpCodes[0xc4] = new OperationCode("CPY", 2, 3, _ADDR_ZP, _CPY);
            m_OpCodes[0xc5] = new OperationCode("CMP", 2, 3, _ADDR_ZP, _CMP);
            m_OpCodes[0xc6] = new OperationCode("DEC", 2, 5, _ADDR_ZP, _DEC);
            m_OpCodes[0xc8] = new OperationCode("INY", 1, 2, _ADDR_IMP, _INY);
            m_OpCodes[0xc9] = new OperationCode("CMP", 2, 2, _ADDR_IMM, _CMP);
            m_OpCodes[0xca] = new OperationCode("DEX", 1, 2, _ADDR_IMP, _DEX);
            m_OpCodes[0xcc] = new OperationCode("CPY", 3, 4, _ADDR_ABS, _CPY);
            m_OpCodes[0xcd] = new OperationCode("CMP", 3, 4, _ADDR_ABS, _CMP);
            m_OpCodes[0xce] = new OperationCode("DEC", 3, 6, _ADDR_ABS, _DEC);
            m_OpCodes[0xD0] = new OperationCode("BNE", 2, 2, _ADDR_REL_1, _BNE);
            m_OpCodes[0xd1] = new OperationCode("CMP", 2, 5, _ADDR_INDY_1, _CMP);
            m_OpCodes[0xd5] = new OperationCode("CMP", 2, 4, _ADDR_ZPX_1, _CMP);
            m_OpCodes[0xd6] = new OperationCode("DEC", 2, 6, _ADDR_ZPX_1, _DEC);
            m_OpCodes[0xd8] = new OperationCode("CLD", 1, 2, _ADDR_IMP, _CLD);
            m_OpCodes[0xd9] = new OperationCode("CMP", 3, 4, _ADDR_ABSY_1, _CMP);
            m_OpCodes[0xdd] = new OperationCode("CMP", 3, 4, _ADDR_ABSX_1, _CMP);
            m_OpCodes[0xde] = new OperationCode("DEC", 3, 7, _ADDR_ABSX_1, _DEC);
            m_OpCodes[0xe0] = new OperationCode("CPX", 2, 2, _ADDR_IMM, _CPX);
            m_OpCodes[0xe1] = new OperationCode("SBC", 2, 6, _ADDR_INDX_1, _SBC);
            m_OpCodes[0xe4] = new OperationCode("CPX", 2, 3, _ADDR_ZP, _CPX);
            m_OpCodes[0xe5] = new OperationCode("SBC", 2, 3, _ADDR_ZP, _SBC);
            m_OpCodes[0xe6] = new OperationCode("INC", 2, 5, _ADDR_ZP, _INC);
            m_OpCodes[0xe8] = new OperationCode("INX", 1, 2, _ADDR_IMP, _INX);
            m_OpCodes[0xe9] = new OperationCode("SBC", 2, 2, _ADDR_IMM, _SBC);
            m_OpCodes[0xea] = new OperationCode("NOP", 1, 2, _ADDR_IMP, _NOP);
            m_OpCodes[0xec] = new OperationCode("CPX", 3, 4, _ADDR_ABS, _CPX);
            m_OpCodes[0xed] = new OperationCode("SBC", 3, 4, _ADDR_ABS, _SBC);
            m_OpCodes[0xee] = new OperationCode("INC", 3, 6, _ADDR_ABS, _INC);
            m_OpCodes[0xF0] = new OperationCode("BEQ", 2, 2, _ADDR_REL_1, _BEQ);
            m_OpCodes[0xf1] = new OperationCode("SBC", 2, 5, _ADDR_INDY_1, _SBC);
            m_OpCodes[0xf5] = new OperationCode("SBC", 2, 4, _ADDR_ZPX_1, _SBC);
            m_OpCodes[0xf6] = new OperationCode("INC", 2, 6, _ADDR_ZPX_1, _INC);
            m_OpCodes[0xf8] = new OperationCode("SED", 1, 2, _ADDR_IMP, _SED);
            m_OpCodes[0xf9] = new OperationCode("SBC", 3, 4, _ADDR_ABSY_1, _SBC);
            m_OpCodes[0xfd] = new OperationCode("SBC", 3, 4, _ADDR_ABSX_1, _SBC);
            m_OpCodes[0xfe] = new OperationCode("INC", 3, 7, _ADDR_ABSX_1, _INC);
        }

        public override void Reset()
        {
            // set registers to its initial values
            m_reg_A = m_reg_X = m_reg_Y = 0;
            m_StackPtr = 0x01FF;
            Flag_Z = true;
            Flag_R = true; // always 1
            m_State = CPUState.fetchopcode;
            m_Cycle = 0;

            // jump to RESET routine
            m_PC = readMemWord(0xFFFC); // reset vector
            base.Reset();
        }

        public void runUntil(UInt16 UntilPC)
        {
            bool bFinish = false;

            do
            {
                if (m_PC == UntilPC && m_State == CPUState.fetchopcode)
                    bFinish = true;
                else
                    emulateCycle();
            } while (!bFinish);
        }

        public void emulateCycle()
        {
            switch(m_State)
            {
                case CPUState.fetchopcode:

                    // check and process Interrupt (only when in OpCode fetch)
                    if(m_Interrupt)
                    {
                        m_MaxCycles = 7;
                        m_Cycle = 1;
                        m_address = 0;
                        m_addr = Addressing._not_set_;
                        m_OperandValue = 0;
                        m_State = CPUState.interrupt;
                        break;
                    }

                    // save current PC for debugging / tracing
                    m_PC_Start = m_PC;

                    // get the next byte and then move the program counter forward
                    byte opCode = getNextMemByte();


                    // get from OpCode table
                    m_oc = m_OpCodes[opCode];

                    // check if OpCode is defined
                    if (String.IsNullOrEmpty(m_oc.OpCode) || m_oc.executeOperation == null || m_oc.executeAddressing == null)
                    {
                        string sMessage = String.Format("PC {0:X4} : OpCode {1:X2} unknown!!!", m_PC_Start, opCode);
                        throw new NotImplementedException(sMessage);
                    }

#if CPU_TRACE
                    m_sbDebug = new StringBuilder();
                    m_sbDebug.AppendFormat("{0:X4}:{1:X2} {2} ", m_PC_Start, opCode, m_oc.OpCode);
#endif
                    
                    m_MaxCycles = m_oc.Cycles;
                    m_Cycle = 1;
                    m_address = 0;
                    m_addr = Addressing._not_set_;
                    m_OperandValue = 0;
                    m_State = CPUState.addressing;

                    break;

                case CPUState.addressing: // addressing
                    m_oc.executeAddressing();
                    m_Cycle++;

                    break;

                case CPUState.operation:
                    if (m_Cycle == m_MaxCycles) // emulate cycle exactness by making operation effective on last cycle
                    {
                        m_oc.executeOperation();
                        if (m_Cycle < m_MaxCycles)
                            m_Cycle++; // operation executed induced another cycle (e.g. branch operations)
                        else
                        {
                            m_State = CPUState.fetchopcode;  // operation finished -> get next opcode
#if CPU_TRACE
                            m_sbDebug.AppendFormat("\tPC:{0:X4} A:{1:X2} X:{2:X2} Y:{3:X2} SP:{4:X4} |{5}| STACK: {6:X2} {7:X2} {8:X2} {9:X2}",
                                    m_PC,
                                    m_reg_A,
                                    m_reg_X,
                                    m_reg_Y,
                                    Reg_SP,
                                    StatusString,
                                    m_ram[m_StackPtr - m_ramOffset],
                                    m_ram[m_StackPtr + 1 - m_ramOffset],
                                    m_ram[m_StackPtr + 2 - m_ramOffset],
                                    m_ram[m_StackPtr + 3 - m_ramOffset]);

                            Debug.WriteLine(m_sbDebug.ToString());
#endif
                        }
                    }
                    else
                        m_Cycle++;

                    break;

                case CPUState.interrupt:
                    if (m_Cycle == m_MaxCycles) // emulate cycle exactness by making operation effective on last cycle
                    {
                        push_stack( (byte)(m_PC >> 8) );
                        push_stack( (byte)m_PC );
                        push_stack(m_reg_SR);
                        Flag_I = true;
                        m_PC = readMemWord(0xFFFE); // fetch IRQ vector
                        m_State = CPUState.fetchopcode;
                        m_Interrupt = false;
                    }
                    else
                        m_Cycle++;
                    break;

            }
        }

        #region Addressing
        private void _ADDR_ABS()
        {
            m_address = getNextMemWord();
#if CPU_TRACE
            m_sbDebug.AppendFormat("${0:X4}", m_address);
#endif

            m_addr = Addressing.absolute;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ABSX_1()
        {
            m_address = getNextMemWord();
#if CPU_TRACE
            m_sbDebug.AppendFormat("${0:X4},X", m_address);
#endif

            if (((m_address & 0x00ff) + m_reg_X) > 0x100) m_MaxCycles++; // add 1 cycle when page boundary is crossed
            m_addr = Addressing.absoluteX;
            m_oc.executeAddressing = _ADDR_ABSX_2;
        }

        private void _ADDR_ABSX_2()
        {
            m_address += m_reg_X;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ABSY_1()
        {
            m_address = getNextMemWord();
#if CPU_TRACE
            m_sbDebug.AppendFormat("${0:X4},Y", m_address);
#endif

            if (((m_address & 0x00ff) + m_reg_Y) > 0x100) m_MaxCycles++; // add 1 cycle when page boundary is crossed
            m_addr = Addressing.absoluteY;
            m_oc.executeAddressing = _ADDR_ABSY_2;
        }

        private void _ADDR_ABSY_2()
        {
            m_address += m_reg_Y;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ACC()
        {
#if CPU_TRACE
            m_sbDebug.Append("A");
#endif
            m_addr = Addressing.accumulator;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_IND_1()
        {
            m_address = getNextMemWord();
#if CPU_TRACE
            m_sbDebug.AppendFormat("(${0:X4})", m_address);
#endif
            m_addr = Addressing.indirect;
            m_oc.executeAddressing = _ADDR_IND_2;
        }
        private void _ADDR_IND_2()
        {
            m_address = readMemWord(m_address);
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_INDX_1()
        {
            m_address = getNextMemByte();
#if CPU_TRACE
            byte b = (byte)m_address;
            m_sbDebug.AppendFormat("${0:X2},X", b);
#endif
            m_addr = Addressing.indirectX;
            m_oc.executeAddressing = _ADDR_INDX_2;
        }

        private void _ADDR_INDX_2()
        {
            m_address += m_reg_X;
            m_address &= 0x00FF;
            m_address = readMemWord(m_address);
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_INDY_1()
        {
            m_address = getNextMemByte();
#if CPU_TRACE
            byte b = (byte)m_address;
            m_sbDebug.AppendFormat("(${0:X2}),Y", b);
#endif
            m_addr = Addressing.indirectY;
            m_oc.executeAddressing = _ADDR_INDY_2;
        }

        private void _ADDR_INDY_2()
        {
            m_address = readMemWord(m_address);
            if (((m_address & 0x00ff) + m_reg_Y) > 0x100) m_MaxCycles++; // add 1 cycle when page boundary is crossed
            m_address += m_reg_Y;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_IMM()
        {
            m_OperandValue = getNextMemByte();
#if CPU_TRACE
            m_sbDebug.AppendFormat("#${0:X2}", m_OperandValue);
#endif
            m_addr = Addressing.immidiate;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_IMP()
        {
            m_addr = Addressing.implied;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_REL_1()
        {
            m_address = m_PC;
            byte value = getNextMemByte();

            if (value >= b10000000)
                m_address -= (byte)(b11111111 - value);
            else
            {
                m_address += value;
                m_address++; // compensate byte with relative address
            }
            if ((m_PC & 0xFF00) == (m_address & 0xFF00))
            {
                m_State = CPUState.operation; // addressing finished -> execute operation
            }
            else
            {
                m_MaxCycles++; // add 1 cycle when page boundary is crossed
                m_oc.executeAddressing = _ADDR_REL_2;
            }
#if CPU_TRACE
            m_sbDebug.AppendFormat("${0:X4}", m_address);
#endif
            m_addr = Addressing.relative;
        }

        private void _ADDR_REL_2()
        {
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ZP()
        {
            m_address = getNextMemByte();
#if CPU_TRACE
            byte b = (byte)m_address;
            m_sbDebug.AppendFormat("${0:X2}", b);
#endif
            m_addr = Addressing.zeropage;
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ZPX_1()
        {
            m_address = getNextMemByte();
#if CPU_TRACE
            byte b = (byte)m_address;
            m_sbDebug.AppendFormat("${0:X2},X", b);
#endif
            m_addr = Addressing.zeropageX;
            m_oc.executeAddressing = _ADDR_ZPX_2;
        }

        private void _ADDR_ZPX_2()
        {
            m_address += m_reg_X;
            m_address &= 0xFF; // wrapping
            m_State = CPUState.operation; // addressing finished -> execute operation
        }

        private void _ADDR_ZPY_1()
        {
            m_address = getNextMemByte();
#if CPU_TRACE
            byte b = (byte)m_address;
            m_sbDebug.AppendFormat("${0:X2},Y", b);
#endif
            m_addr = Addressing.zeropageY;
            m_oc.executeAddressing = _ADDR_ZPY_2;
        }

        private void _ADDR_ZPY_2()
        {
            m_address += m_reg_Y;
            m_address &= 0xFF; // wrapping
            m_State = CPUState.operation; // addressing finished -> execute operation
        }
        #endregion

        #region Operations
        private void _ADC()
        {
            byte b;

            if (m_addr == Addressing.immidiate)
                b = m_OperandValue;
            else
                b = readMemByte(m_address);

            byte CarryValue = (byte)(Flag_C ? 1 : 0);

            if (Flag_D) // Decimal mode
            {
                UInt16 al, ah;
                
                al = (UInt16)((Reg_A & 0x0f) + (b & 0x0f) + CarryValue);    // Calculate lower nybble
                if (al > 9) al += 6;                                        // BCD fixup for lower nybble

                ah = (UInt16)((Reg_A >> 4) + (b >> 4));                     // Calculate upper nybble
                if (al > 0x0f) ah++;

                setFlagZFromValue((byte)(Reg_A + b + CarryValue));          // Set flags
                setFlagNFromValue((byte)(ah << 4));                         // Only highest bit used
                Flag_C = (((ah << 4) ^ Reg_A) & 0x80) != 0 && !(((Reg_A ^ b) & 0x80) != 0);

                if (ah > 9) ah += 6;                                        // BCD fixup for upper nybble
                Flag_C = ah > 0x0f;                                         // Set carry flag
                Reg_A = (byte)((ah << 4) | (al & 0x0f));                    // Compose result
            }
            else // Binary mode
            {
                UInt16 w;

                w = Reg_A;
                w += b;
                w += CarryValue;

                Flag_C = w > 0xFF;
                Flag_V = !(((Reg_A ^ b) & 0x80) != 0) && ((Reg_A ^ w) & 0x80) != 0;

                Reg_A = (byte)(w & 0xFF);
            }
        }
        
        private void _AND()
        {
            if (m_addr == Addressing.immidiate)
                Reg_A &= m_OperandValue;
            else
                Reg_A &= readMemByte(m_address);
        }

        private void _ASL()
        {
            UInt16 w;
            byte b;

            if( m_addr == Addressing.accumulator )
                w = m_reg_A;
            else
                w = readMemByte(m_address);

            Flag_C = ((w & 0x80) != 0);

            w <<= 1;

            b = (byte)(w & 0xFF);

            if (m_addr == Addressing.accumulator)
            {
                Reg_A = b;
            }
            else
            {
                setFlagsNZFromValue(b);
                writeMemByte(m_address,b);
            }

        }

        private void _branch_cycle()
        {
            m_PC = m_address;
        }

        private void _BCC()
        {
            if (!Flag_C)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BCS()
        {
            if (Flag_C)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BEQ()
        {
            if (Flag_Z)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BIT()
        {
            byte b;
            byte result;

            if( m_addr == Addressing.immidiate )
                b = m_OperandValue;
            else
                b = readMemByte(m_address);

            result = b;
            result &= m_reg_A;

            setFlagZFromValue(result);

            setFlagNFromValue(b);
            Flag_V = (b & b01000000) == b01000000;

        }

        private void _BMI()
        {
            if (Flag_N)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BNE()
        {
            if (!Flag_Z)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BPL()
        {
            if (!Flag_N)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BRK()
        {
            throw new ApplicationException("BRK");
        }

        private void _BVC()
        {
            if (!Flag_V)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _BVS()
        {
            if (Flag_V)
            {
                m_MaxCycles++;
                m_oc.executeOperation = _branch_cycle;
            }
        }

        private void _CLI()
        {
            Flag_I = false;
        }

        private void _CLC()
        {
            Flag_C = false;
        }

        private void _CLD()
        {
            Flag_D = false;
        }

        private void _CLV()
        {
            Flag_V = false;
        }

        private void doCMP(byte RegValue)
        {
            UInt16 wResult = RegValue;
            byte bCompare;
            if (m_addr == Addressing.immidiate)
                bCompare = m_OperandValue;
            else
            {
                bCompare = readMemByte(m_address);
            }
            wResult -= bCompare;
            Flag_C = wResult < 0x100;
            setFlagsNZFromValue((byte)wResult);
        }

        private void _CMP()
        {
            doCMP(Reg_A);
        }

        private void _CPX()
        {
            doCMP(Reg_X);
        }

        private void _CPY()
        {
            doCMP(Reg_Y);
        }

        private void _DEC()
        {
            byte b = readMemByte(m_address);

            b--;

            setFlagsNZFromValue(b);

            writeMemByte(m_address, b);
        }

        private void _DEX()
        {
            Reg_X--;
        }

        private void _DEY()
        {
            Reg_Y--;
        }

        private void _EOR()
        {
            if (m_addr == Addressing.immidiate)
                Reg_A ^= m_OperandValue;
            else
                Reg_A ^= readMemByte(m_address);
        }

        private void _INC()
        {
            byte b = readMemByte(m_address);

            b++;

            setFlagsNZFromValue(b);

            writeMemByte(m_address, b);
        }

        private void _INX()
        {
            Reg_X++;
        }

        private void _INY()
        {
            Reg_Y++;
        }

        private void _JMP()
        {
#if CPU_CALLSTACK
            Debug.WriteLine("JMP {0:X4}->{1:X4}", m_PC_Start, m_address);
#endif
            m_PC = m_address;
        }

        private void _JSR()
        {
#if CPU_CALLSTACK
            Debug.WriteLine("JSR {0:X4}->{1:X4}", m_PC_Start, m_address);
#endif
            UInt16 returnaddress = m_PC;
            returnaddress--;
            push_stack((byte)(returnaddress >> 8));
            push_stack((byte)(returnaddress & 0xFF));
            m_PC = m_address;
        }

        private void _LDA()
        {
            if (m_addr == Addressing.immidiate)
                Reg_A = m_OperandValue;
            else
                Reg_A = readMemByte(m_address);
        }

        private void _LDX()
        {
            if (m_addr == Addressing.immidiate)
                Reg_X = m_OperandValue;
            else
                Reg_X = readMemByte(m_address);
        }

        private void _LDY()
        {
            if (m_addr == Addressing.immidiate)
                Reg_Y = m_OperandValue;
            else
                Reg_Y = readMemByte(m_address);
        }

        private void _LSR()
        {
            UInt16 w;
            byte b;

            if (m_addr == Addressing.accumulator)
                w = m_reg_A;
            else
                w = readMemByte(m_address);

            Flag_C = ((w & b00000001) == b00000001); // transfer status of bit 0 for carry

            w >>= 1;

            b = (byte)(w & 0xFF);

            if (m_addr == Addressing.accumulator)
            {
                Reg_A = b;
            }
            else
            {
                setFlagZFromValue(b);
                writeMemByte(m_address, (byte)(w & 0xFF));
            }
        }

        private void _NOP()
        {
        }

        private void _ORA()
        {
            if (m_addr == Addressing.immidiate)
                Reg_A |= m_OperandValue;
            else
                Reg_A |= readMemByte(m_address);
        }

        private void _PHA()
        {
            push_stack(m_reg_A);  // Push the contents of the accumulator onto the stack
        }

        private void _PHP()
        {
            push_stack(m_reg_SR);     // Push the contents of the SR onto the stack
        }

        private void _PLA()
        {
            Reg_A = pop_stack();
        }

        private void _PLP()
        {
            m_reg_SR = pop_stack();
        }

        private void _ROL()
        {
            UInt16 w;
            byte b;

            if (m_addr == Addressing.accumulator)
                w = m_reg_A;
            else
                w = readMemByte(m_address);

            w <<= 1;

            if (Flag_C) w |= b00000001; // fill bit 0 with carry

            Flag_C = ((w & 0x100) == 0x100); // transfer bit 0 of high byte to carry

            b = (byte)(w & 0xFF);

            if (m_addr == Addressing.accumulator)
            {
                Reg_A = b;
            }
            else
            {
                setFlagsNZFromValue(b);
                writeMemByte(m_address, (byte)(w & 0xFF));
            }
        }

        private void _ROR()
        {
            UInt16 w;
            byte b;
            bool saveCarry; 

            if (m_addr == Addressing.accumulator)
                w = m_reg_A;
            else
                w = readMemByte(m_address);

            saveCarry = ((w & b00000001) == b00000001); // save status of bit 0 for carry

            w >>= 1;

            if (Flag_C) w |= b10000000; // fill bit 7 with carry bit

            Flag_C = saveCarry;

            b = (byte)(w & 0xFF);

            if (m_addr == Addressing.accumulator)
            {
                Reg_A = b;
            }
            else
            {
                setFlagsNZFromValue(b);
                writeMemByte(m_address, (byte)(w & 0xFF));
            }
        }

        private void _RTI()
        {
            m_reg_SR = pop_stack();

            UInt16 returnaddress = pop_stack();
            returnaddress += (UInt16)(pop_stack() << 8);
            m_PC = returnaddress;
        }

        private void _RTS()
        {
            UInt16 returnaddress = pop_stack();
            returnaddress += (UInt16)(pop_stack() << 8);
            returnaddress++;
#if CPU_CALLSTACK
            Debug.WriteLine("RTS {0:X4}<-{1:X4}",returnaddress, m_PC_Start);
#endif
            m_PC = returnaddress;
        }

        private void _SBC()
        {
            byte b;

            if (m_addr == Addressing.immidiate)
                b = m_OperandValue;
            else
                b = readMemByte(m_address);

            byte CarryValue = (byte)(Flag_C ? 0 : 1);

            UInt16 w;

            w = Reg_A;
            w -= b;
            w -= CarryValue;

            if (Flag_D) // Decimal mode
            {
                UInt16 al, ah;
                
                al = (UInt16)((Reg_A & 0x0f) - (b & 0x0f) - CarryValue);	// Calculate lower nybble
                ah = (UInt16)((Reg_A >> 4) - (b >> 4));							// Calculate upper nybble
                if ((al & 0x10) != 0)
                {
                    al -= 6;											        // BCD fixup for lower nybble
                    ah--;
                }
                if ((ah & 0x10) != 0) ah -= 6;									// BCD fixup for upper nybble

                Flag_C = w < 0x100;									        // Set flags
                Flag_V = ((Reg_A ^ w) & 0x80) != 0 && ((Reg_A ^ b) & 0x80) != 0;
                setFlagsNZFromValue((byte)w);

                Reg_A = (byte)((ah << 4) | (al & 0x0f));							// Compose result
            }
            else // Binary mode
            {
                Flag_C = w < 0x100;
                Flag_V = ((Reg_A ^ w) & 0x80) > 0 && ((Reg_A ^ b) & 0x80) > 0;

                Reg_A = (byte)(w & 0xFF);
            }
        }
        

        private void _SEC()
        {
            Flag_C = true;
        }

        private void _SED()
        {
            Flag_D = true;
        }

        private void _SEI()
        {
            Flag_I = true;
        }

        private void _STA()
        {
            writeMemByte(m_address, m_reg_A);
        }

        private void _STX()
        {
            writeMemByte(m_address, m_reg_X);
        }

        private void _STY()
        {
            writeMemByte(m_address, m_reg_Y);
        }

        private void _TAX()
        {
            Reg_X = m_reg_A;
        }

        private void _TAY()
        {
            Reg_Y = m_reg_A;
        }

        private void _TSX()
        {
            m_reg_X = Reg_SP;
        }

        private void _TXA()
        {
            Reg_A = m_reg_X;
        }

        private void _TYA()
        {
            Reg_A = m_reg_Y;
        }

        private void _TXS()
        {
            Reg_SP = m_reg_X;
        }

        #endregion

        #region STACK HANDLING
        private void push_stack(byte value)
        {
            m_ram[(m_StackPtr--) - m_ramOffset] = value;   // Stack beings from $01FF and works down
            if (m_StackPtr < 0x0100)     // Push beyond 0x0100 (256 bytes)?
                m_StackPtr = 0x01FF;     // Yes, then the SP will wrap to the start 
        }

        private byte pop_stack()
        {
            byte value = m_ram[(++m_StackPtr) - m_ramOffset];
            if (m_StackPtr > 0x01FF)
                m_StackPtr = 0x01FF;     // 
            return value;
        }
        #endregion

        #region STATUS FLAGS
        // Negative Flag (bit 7)
        public bool Flag_N
        {
            set { if (value) m_reg_SR |= b10000000; else m_reg_SR &= (b11111111 - b10000000); }
            get { return ((m_reg_SR & b10000000) == b10000000); }
        }

        // Overflow Flag (bit 6)
        public bool Flag_V
        {
            set { if (value) m_reg_SR |= b01000000; else m_reg_SR &= (b11111111 - b01000000); }
            get { return ((m_reg_SR & b01000000) == b01000000); }
        }

        // Reserve Flag (bit 5)
        public bool Flag_R
        {
            set { if (value) m_reg_SR |= b00100000; else m_reg_SR &= (b11111111 - b00100000); }
            get { return ((m_reg_SR & b00100000) == b00100000); }
        }

        // Break Flag (bit 4)
        public bool Flag_B
        {
            set { if (value) m_reg_SR |= b00010000; else m_reg_SR &= (b11111111 - b00010000); }
            get { return ((m_reg_SR & b00010000) == b00010000); }
        }

        // Decimal Flag (bit 3)
        public bool Flag_D
        {
            set { if (value) m_reg_SR |= b00001000; else m_reg_SR &= (b11111111 - b00001000); }
            get { return ((m_reg_SR & b00001000) == b00001000); }
        }

        // Interupt Flag (bit 2)
        public bool Flag_I
        {
            set { if (value) m_reg_SR |= b00000100; else m_reg_SR &= (b11111111 - b00000100); }
            get { return ((m_reg_SR & b00000100) == b00000100); }
        }

        // Zero Flag (bit 1)
        public bool Flag_Z
        {
            set { if (value) m_reg_SR |= b00000010; else m_reg_SR &= (b11111111 - b00000010); }
            get { return ((m_reg_SR & b00000010) == b00000010); }
        }

        // Carry Flag (bit 0)
        public bool Flag_C
        {
            set { if (value) m_reg_SR |= b00000001; else m_reg_SR &= (b11111111 - b00000001); }
            get { return ((m_reg_SR & 1) == b00000001); }
        }

        public void setFlagsNZFromValue(byte b)
        {
            Flag_N = (b & b10000000) == b10000000;
            Flag_Z = b == 0;
        }

        public void signalInterrupt(InterruptSignal intsig)
        {
            // TODO MOS6502 - NMI interrupt handling
            if (intsig == InterruptSignal.IRQ && !Flag_I) m_Interrupt = true;
        }

        public string StatusString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(Flag_N ? "N" : "-");
                sb.Append(Flag_V ? "V" : "-");
                sb.Append(Flag_R ? "R" : "-");
                sb.Append(Flag_B ? "B" : "-");
                sb.Append(Flag_D ? "D" : "-");
                sb.Append(Flag_I ? "I" : "-");
                sb.Append(Flag_Z ? "Z" : "-");
                sb.Append(Flag_C ? "C" : "-");

                return sb.ToString();
            }
        }

        public void setFlagNFromValue(byte b)
        {
            Flag_N = (b & b10000000) == b10000000;
        }

        public void setFlagZFromValue(byte b)
        {
            Flag_Z = b == 0;
        }
        #endregion

        #region REGISTERS
        public UInt16 PC
        {
            get { return m_PC; }
        }

        public byte Reg_A
        {
            set
            {
                m_reg_A = value;
                setFlagsNZFromValue(m_reg_A);
            }
            get { return m_reg_A; }
        }

        public byte Reg_X
        {
            set
            {
                m_reg_X = value;
                setFlagsNZFromValue(m_reg_X);
            }
            get { return m_reg_X; }
        }

        public byte Reg_Y
        {
            set
            {
                m_reg_Y = value;
                setFlagsNZFromValue(m_reg_Y);
            }
            get { return m_reg_Y; }
        }

        public byte Reg_SP
        {
            set { m_StackPtr = (UInt16)(0x0100 + value); }
            get { return (byte)(m_StackPtr & 0x00FF); }
        }
        #endregion

    }
}
