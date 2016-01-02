using System.Timers;
using System;
using System.Diagnostics;
using MyEMU.CPU;

/*
         Control

 7    CA1 active transition flag. 1= 0->1, 0= 1->0
 6    CA2 active transition flag. 1= 0->1, 0= 1->0
 5    CA2 direction           1 = out        | 0 = in
                    ------------+------------+---------------------
 4    CA2 control   Handshake=0 | Manual=1   | Active: High=1 Low=0
 3    CA2 control   On Read=0   | CA2 High=1 | IRQ on=1, IRQ off=0
                    Pulse  =1   | CA2 Low=0  |

 2    Port A control: DDRA = 0, IORA = 1
 1    CA1 control: Active High = 1, Low = 0
 0    CA1 control: IRQ on=1, off = 0
*/

namespace MyEMU.I_O
{
    /// <summary>
    /// Universal implementation of MC6820
    /// </summary>
    public class MC6820
    {
        protected string m_Designation;

        protected byte m_ORA; // Output register A
        protected byte m_IRA; // Input register A
        protected byte m_DDRA; // data direction register A                 (Output=1, Input=0)
        protected byte m_DDRA_neg; // negative data direction register A    (Output=0, Input=1)
        protected Signal m_CA1; // control line A1
        protected Signal m_CA2; // control line A2

        protected byte m_CRA; // control register A
        protected bool m_CRA_Bit0_EnableIRQA1;
        protected bool m_CRA_Bit1_CA1_PositiveTrans;
        protected bool m_CRA_Bit2_WritePort;
        protected bool m_CRA_Bit3_EnableIRQA2;
        protected bool m_CRA_Bit3_PulseOutput;
        protected bool m_CRA_Bit3_CA2_set_high;
        protected bool m_CRA_Bit4_CA2_PositiveTrans;
        protected bool m_CRA_Bit4_ManualOutput;
        protected bool m_CRA_Bit5_OutputMode;

        protected byte m_ORB; // Output register B
        protected byte m_IRB; // Input register B 
        protected byte m_DDRB; // data direction register B                 (Output=1, Input=0)
        protected byte m_DDRB_neg; // negative data direction register B    (Output=0, Input=1)
        protected Signal m_CB1; // control line B1
        protected Signal m_CB2; // control line B2

        protected byte m_CRB; // control register B
        protected bool m_CRB_Bit0_EnableIRQB1;
        protected bool m_CRB_Bit1_CB1_PositiveTrans;
        protected bool m_CRB_Bit2_WritePort;
        protected bool m_CRB_Bit3_EnableIRQB2;
        protected bool m_CRB_Bit3_PulseOutput;
        protected bool m_CRB_Bit3_CB2_set_high;
        protected bool m_CRB_Bit4_CB2_PositiveTrans;
        protected bool m_CRB_Bit4_ManualOutput;
        protected bool m_CRB_Bit5_OutputMode;

        protected SendHandler m_sendOutputA;
        protected SendHandler m_sendOutputB;
        protected SendInterruptHandler m_sendInterrupt;

        public MC6820(string Designation, CPU8Bit cpu, UInt16 BaseAddress)
        {
            m_Designation = Designation;

            cpu.registerMemoryAccess(BaseAddress, (UInt16)(BaseAddress + 0x0F), read, write);

            m_sendOutputA = null;
            m_sendOutputB = null;
            m_sendInterrupt = null;

            reset();
        }

        public virtual void reset()
        {
            m_IRA = 0xFF;
            m_IRB = 0;

            m_CA1 = Signal.Rise;
            m_CA2 = Signal.Rise;

            m_CRA = m_CRB = m_ORA = m_ORB = 0;

            m_DDRA = m_DDRB = 0;
            m_DDRA_neg = m_DDRB_neg = 0xFF;

            updateControlRegisters();
        }

        /// <summary>
        /// update switches with status of control registers
        /// </summary>
        private void updateControlRegisters()
        {
            // section A -----------------------------------------
            m_CRA_Bit0_EnableIRQA1 = (m_CRA & 0x01) == 0x01;
            m_CRA_Bit1_CA1_PositiveTrans = (m_CRA & 0x02) == 0x02;
            m_CRA_Bit2_WritePort = (m_CRA & 0x04) == 0x04;
            m_CRA_Bit5_OutputMode = (m_CRA & 0x20) == 0x20;

            m_CRA_Bit3_EnableIRQA2 = false;
            m_CRA_Bit3_PulseOutput = false;
            m_CRA_Bit3_CA2_set_high = false;
            m_CRA_Bit4_CA2_PositiveTrans = false;
            m_CRA_Bit4_ManualOutput = false;

            if (m_CRA_Bit5_OutputMode)
            {
                m_CRA_Bit4_ManualOutput = (m_CRA & 0x10) == 0x10;
                if(m_CRA_Bit4_ManualOutput)
                {
                    m_CRA_Bit3_CA2_set_high = (m_CRA & 0x08) == 0x08;
                    m_CA2 = m_CRA_Bit3_CA2_set_high ? Signal.Rise : Signal.Fall;
                }
                else
                    m_CRA_Bit3_PulseOutput = (m_CRA & 0x08) == 0x08;
            }
            else
            {
                m_CRA_Bit3_EnableIRQA2 = (m_CRA & 0x08) == 0x08;
                m_CRA_Bit4_CA2_PositiveTrans = (m_CRA & 0x10) == 0x10;
            }

            // section B -----------------------------------------
            m_CRB_Bit0_EnableIRQB1 = (m_CRB & 0x01) == 0x01;
            m_CRB_Bit1_CB1_PositiveTrans = (m_CRB & 0x02) == 0x02;
            m_CRB_Bit2_WritePort = (m_CRB & 0x04) == 0x04;
            m_CRB_Bit5_OutputMode = (m_CRB & 0x20) == 0x20;

            m_CRB_Bit3_EnableIRQB2 = false;
            m_CRB_Bit3_PulseOutput = false;
            m_CRB_Bit3_CB2_set_high = false;
            m_CRB_Bit4_CB2_PositiveTrans = false;
            m_CRB_Bit4_ManualOutput = false;

            if (m_CRB_Bit5_OutputMode)
            {
                m_CRB_Bit4_ManualOutput = (m_CRB & 0x10) == 0x10;
                if (m_CRB_Bit4_ManualOutput)
                {
                    m_CRB_Bit3_CB2_set_high = (m_CRB & 0x08) == 0x08;
                    m_CB2 = m_CRB_Bit3_CB2_set_high ? Signal.Rise : Signal.Fall;
                }
                else
                    m_CRB_Bit3_PulseOutput = (m_CRB & 0x08) == 0x08;
            }
            else
            {
                m_CRB_Bit3_EnableIRQB2 = (m_CRB & 0x08) == 0x08;
                m_CRB_Bit4_CB2_PositiveTrans = (m_CRB & 0x10) == 0x10;
            }
        }

        public void write(UInt16 address, byte b)
        {
            byte Reg = (byte)(address & 0x03);

#if PIA_TRACE
            string[] asRegs = { "PA", "CRA", "PB", "CRB"};
            Debug.WriteLine("{0} (from CPU):{1}:={2:X2}", m_Designation, asRegs[Reg], b);
#endif

            switch (Reg)
            {
                case 0: // DDRA / PA
                    if (m_CRA_Bit2_WritePort)
                    {
                        m_ORA = b; // into output register A
                        if (m_sendOutputA != null)
                        {
                            // mix input and output
                            byte bOut = 0;
                            bOut |= (byte)(m_ORA & m_DDRA);
                            bOut |= (byte)(m_IRA & m_DDRA_neg);
                            m_sendOutputA(bOut);
                        }
                    }
                    else
                    {
                        m_DDRA = b; // into data direction register A
                        m_DDRA_neg = (byte)~b;
                    }
                    break;

                case 1: // CRA
                    m_CRA = (byte)((m_CRA & 0xC0) | (b & 0x3F)); // do not change IRQ flags
                    updateControlRegisters();
                    updateIRQ();
                    break;

                case 2: // DDRB / PB
                    if (m_CRB_Bit2_WritePort)
                    {
                        m_ORB = b; // into output register B
                        if (m_sendOutputB != null)
                        {
                            // mix input and output
                            byte bOut = 0;
                            bOut |= (byte)(m_ORB & m_DDRB);
                            bOut |= (byte)(m_IRB & m_DDRB_neg);
                            m_sendOutputB(bOut);

                            if( m_CRB_Bit5_OutputMode && !m_CRB_Bit4_ManualOutput ) // handshake on write mode
                            {
                                m_CB2 = Signal.Fall;
                                if (m_CRB_Bit3_PulseOutput) m_CB2 = Signal.Rise;
                            }
                        }
                    }
                    else
                    {
                        m_DDRB = b; // into data direction register B
                        m_DDRB_neg = (byte)~b;
                    }
                    break;

                case 3: // CRB
                    m_CRB = (byte)((m_CRB & 0xC0) | (b & 0x3F)); // do not change IRQ flags
                    updateControlRegisters();
                    updateIRQ();
                    break;

                default:
                    string sMessage = String.Format("{0}: Invalid write address {1:X4}", this.GetType().ToString(), address);
                    throw new ApplicationException(sMessage);
            }
        }

        public byte read(UInt16 address)
        {
            byte Reg = (byte)(address & 0x03);

            byte b;

            switch (Reg)
            {
                case 0: // PA
                    
                    m_CRA &= 0x3F;  // IRQ flags implicitly cleared by a read

                    // mix input and output
                    b = 0;
                    b |= (byte)(m_ORA & m_DDRA);
                    b |= (byte)(m_IRA & m_DDRA_neg);

                    break;

                case 1: // CRA
                    b = m_CRA;
                    break;

                case 2: // PB
                    
                    m_CRB &= 0x3F; // IRQ flags implicitly cleared by a read

                    // mix input and output
                    b = 0;
                    b |= (byte)(m_ORB & m_DDRB);
                    b |= (byte)(m_IRB & m_DDRB_neg);

                    break;

                case 3: // CRB
                    b = m_CRB;
                    break;

                default:
                    string sMessage = String.Format("{0}: Invalid read address {1:X4}", this.GetType().ToString(), address);
                    throw new ApplicationException(sMessage);
            }

            return b;
        }

        public void InputA(byte b)
        {
            m_IRA = b;
        }

        public void InputB(byte b)
        {
            m_IRB = b;

        }

        public SendHandler OutputA
        {
            set { m_sendOutputA = value; }
        }

        public SendHandler OutputB
        {
            set { m_sendOutputB = value; }
        }

        public SendInterruptHandler InterruptLine
        {
            set { m_sendInterrupt = value; }
        }

        public string StatusString
        {
            get
            {
                return String.Format("{0} CRA:{1:X2} CRB:{2:X2} DDRA:{3:X2} DDRB:{4:X2}", m_Designation, m_CRA, m_CRB, m_DDRA, m_DDRB);
            }
        }

        /// <summary>
        /// send an interrupt signal (to the CPU) if one of the IRQs is enabled and one of the IRQs (A/B 1/2) is flagged
        /// </summary>
        private void updateIRQ()
        {
            if (m_sendInterrupt != null &&
                (
                    (m_CRA_Bit0_EnableIRQA1 && (m_CRA & 0x80) == 0x80) ||
                    (m_CRA_Bit3_EnableIRQA2 && (m_CRA & 0x40) == 0x40) ||
                    (m_CRB_Bit0_EnableIRQB1 && (m_CRB & 0x80) == 0x80) ||
                    (m_CRB_Bit3_EnableIRQB2 && (m_CRB & 0x40) == 0x40)
                )
                )
                    m_sendInterrupt(InterruptSignal.IRQ);
        }

        public Signal CA1
        {
            get { return m_CA1; }
            set
            {
                // flag interrupt 
                if (m_CA1 != value && (m_CRA_Bit1_CA1_PositiveTrans ? Signal.Rise : Signal.Fall) == value)
                {
                    m_CRA |= 0x80; // set bit 7 IRQA1
                    updateIRQ();
                    if (m_CRA_Bit5_OutputMode && !m_CRA_Bit4_ManualOutput && !m_CRA_Bit3_PulseOutput) // handshake mode
                        m_CA2 = Signal.Rise;
                }
                m_CA1 = value;
            }
        }

        public Signal CA2
        {
            get { return m_CA2; }
            set
            {
                if (m_CA2 != value && (m_CRA_Bit4_CA2_PositiveTrans ? Signal.Rise : Signal.Fall) == value)
                {
                    m_CRA |= 0x40; // set bit 6 IRQA2
                    updateIRQ();
                }
                m_CA2 = value;
            }
        }

        public Signal CB1
        {
            get { return m_CB1; }
            set
            {
                if (m_CB1 != value && (m_CRB_Bit1_CB1_PositiveTrans ? Signal.Rise : Signal.Fall) == value)
                {
                    m_CRB |= 0x80; // set bit 7 IRQB1
                    updateIRQ();
                    if (m_CRB_Bit5_OutputMode && !m_CRB_Bit4_ManualOutput && !m_CRB_Bit3_PulseOutput) // handshake mode
                        m_CB2 = Signal.Rise;
                }
                m_CB1 = value;
            }
        }

        public Signal CB2
        {
            get { return m_CB2; }
            set
            {
                if (m_CB2 != value && (m_CRB_Bit4_CB2_PositiveTrans ? Signal.Rise : Signal.Fall) == value)
                {
                    m_CRB |= 0x40; // set bit 6 IRQB2
                    updateIRQ();
                }
                m_CB2 = value;
            }

        }
    }

}
