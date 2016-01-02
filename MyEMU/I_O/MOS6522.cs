using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEMU.CPU;

namespace MyEMU.I_O
{
    internal enum InterruptFlags
    {
        INT_CA2 = 0x01,
        INT_CA1 = 0x02,
        INT_SR  = 0x04,
        INT_CB2 = 0x08,
        INT_CB1 = 0x10,
        INT_T2  = 0x20,
        INT_T1  = 0x40,
        INT_ANY = 0x80
    }

    public class MOS6522
    {
        protected string m_Designation;

        protected byte m_ORB; // Output register B
        protected byte m_IRB; // Input register B 
        protected byte m_DDRB; // data direction register B                 (Output=1, Input=0)
        protected byte m_DDRB_neg; // negative data direction register B    (Output=0, Input=1)
        protected Signal m_CB1; // control line B1
        protected Signal m_CB2; // control line B2

        protected byte m_ORA; // Output register A
        protected byte m_IRA; // Input register A
        protected byte m_DDRA; // data direction register A                 (Output=1, Input=0)
        protected byte m_DDRA_neg; // negative data direction register A    (Output=0, Input=1)
        protected Signal m_CA1; // control line A1
        protected Signal m_CA2; // control line A2

        private bool m_T1_active;
        private byte m_T1CL;
        private byte m_T1CH;
        private byte m_T1LL;
        private byte m_T1LH;

        private bool m_T2_active;
        //private bool m_T2;
        private byte m_T2CL;
        private byte m_T2CH;

        private byte m_SR;

        private byte m_ACR;
        private bool m_ACR_set_PB7;
        private bool m_ACR_T1_continuous;

        private byte m_PCR;
        private bool m_PCR_set_PB7;
        private bool m_PCR_CA1_PositiveTrans;
        private bool m_PCR_CA2_Output;
        private bool m_PCR_CA2_PositiveTrans;
        private bool m_PCR_CA2_Independant_IRQ;
        private bool m_PCR_CB1_PositiveTrans;
        private bool m_PCR_CB2_Output;
        private bool m_PCR_CB2_PositiveTrans;
        private bool m_PCR_CB2_Independant_IRQ;

        private byte m_IFR;
        private byte m_IER;

        private byte m_ANH; // register A - no handshake

        protected SendHandler m_sendOutputA;
        protected SendHandler m_sendOutputB;
        protected SendInterruptHandler m_sendInterrupt;

        public MOS6522(string Designation, CPU8Bit cpu, UInt16 BaseAddress)
        {
            m_Designation = Designation;

            cpu.registerMemoryAccess(BaseAddress,(UInt16)(BaseAddress + 0x0F), read, write);

            m_sendOutputA = null;
            m_sendOutputB = null;
            m_sendInterrupt = null;

            reset();
        }

        public virtual void reset()
        {
            m_IRB = m_ORB = 0;
            m_IRA = m_ORA = 0;

            m_DDRB = 0;
            m_DDRA = 0;

            m_T1_active = false;
            m_T1CL = 0;
            m_T1CH = 0;
            m_T1LL = 0;
            m_T1LH = 0;

            m_T2_active = false;
            m_T2CL = 0;
            m_T2CH = 0;

            m_SR = 0;
            m_ACR = 0;
            m_PCR = 0;
            m_IFR = 0;
            m_IER = 0;
            m_ANH = 0;

            updateControlRegisters();
        }

        private void updateControlRegisters()
        {
            // ACR : Auxiliary Control Register
            m_ACR_set_PB7 = (m_ACR & 0x80) == 0x80;

            // PCR : Peripheral Control Register
            m_PCR_set_PB7 = (m_PCR & 0x80) == 0x80;

            m_PCR_CA1_PositiveTrans = (m_PCR & 0x01) == 0x01;
            m_PCR_CB1_PositiveTrans = (m_PCR & 0x10) == 0x10;

            m_ACR_T1_continuous = (m_PCR & 0x40) == 0x40;

            // PCR-CA2 control
            m_PCR_CA2_Output = (m_PCR & 0x08) == 0x08;
            m_PCR_CA2_Independant_IRQ = (m_PCR & 0x0A) == 0x02;

            if(m_PCR_CA2_Output)
            {
                m_PCR_CA2_PositiveTrans = false;
            }
            else
            {
                m_PCR_CA2_PositiveTrans = (m_PCR & 0x02) == 0x02;
            }

            // PCR-CB2 control
            m_PCR_CB2_Output = (m_PCR & 0x80) == 0x80;
            m_PCR_CB2_Independant_IRQ = (m_PCR & 0xA0) == 0x20;

            if (m_PCR_CB2_Output)
            {
                m_PCR_CB2_PositiveTrans = false;
            }
            else
            {
                m_PCR_CB2_PositiveTrans = (m_PCR & 0x20) == 0x20;
            }

        }

        private void clearInterrupt(InterruptFlags IF)
        {
            m_IFR = (byte)((m_IFR & (byte)~IF) & 0x7F);
            if ((m_IFR & m_IER) > 0) m_IFR |= 0x80;

        }

        private void setInterrupt(InterruptFlags IF)
        {
            m_IFR |= (byte)IF;
            if ((m_IFR & m_IER) > 0) m_IFR |= 0x80;

        }

        private void clear_PA_Interrupts()
        {
            clearInterrupt(InterruptFlags.INT_CA1);
            if (!m_PCR_CA2_Independant_IRQ) clearInterrupt(InterruptFlags.INT_CA2);

        }
        private void clear_PB_Interrupts()
        {
            clearInterrupt(InterruptFlags.INT_CB1);
            if (!m_PCR_CB2_Independant_IRQ) clearInterrupt(InterruptFlags.INT_CB2);

        }

        public void emulateCycle()
        {
            if(m_T1_active)
            {
                if (m_T1CL > 0)
                    m_T1CL--;
                else
                {
                    if (m_T1CH > 0)
                    {
                        m_T1CL = 0xFF;
                        m_T1CH--;
                    }
                    else T1(); // timer 1 finished
                }
            }

            if (m_T2_active)
            {
                if (m_T2CL > 0)
                    m_T2CL--;
                else
                {
                    if (m_T2CH > 0)
                    {
                        m_T2CL = 0xFF;
                        m_T2CH--;
                    }
                    else T2(); // timer 2 finished
                }
            }

        }

        private void updateIRQ()
        {
            if ((m_IFR & (byte)InterruptFlags.INT_ANY) > 0)
            {
                m_sendInterrupt(InterruptSignal.IRQ);
                m_IFR = 0;
            }
        }

        private void T1()
        {
            if (m_PCR_set_PB7)
            {
                m_ORB |= 0x80; // set PB7 high on timer finish
                if (m_sendOutputB != null)
                {
                    // mix input and output
                    byte bOut = 0;
                    bOut |= (byte)(m_ORB & m_DDRB);
                    bOut |= (byte)(m_IRB & m_DDRB_neg);
                    m_sendOutputB(bOut);
                }
            }

            if(m_ACR_T1_continuous)
            {
                m_T1CL = m_T1LL;
                m_T1CH = m_T1LH;
            }
            else
                m_T1_active = false;

            setInterrupt(InterruptFlags.INT_T1);
            updateIRQ();
        }

        private void T2()
        {
            m_T2_active = false;
            setInterrupt(InterruptFlags.INT_T2);
            updateIRQ();
        }

        public void write(UInt16 address, byte b)
        {
            byte Reg = (byte)(address & 0x0F);

            switch (Reg)
            {
                case 0: 
                    if(m_ACR_set_PB7)
                        m_ORB = (byte)((m_ORB & 0x80) | (b & 0x7F));
                    else
                        m_ORB = b; 

                    if (m_DDRB > 0 && m_sendOutputB != null)
                    {
                        // mix input and output
                        byte bOut = 0;
                        bOut |= (byte)(m_ORB & m_DDRB);
                        bOut |= (byte)(m_IRB & m_DDRB_neg);
                        m_sendOutputB(bOut);
                    }

                    clear_PB_Interrupts();

                    // TODO 6522 - implement pulse / handshake mode

                    break;

                case 1: 
                    m_ORA = b; 
                    if (m_DDRA > 0 && m_sendOutputA != null)
                    {
                        // mix input and output
                        byte bOut = 0;
                        bOut |= (byte)(m_ORA & m_DDRA);
                        bOut |= (byte)(m_IRA & m_DDRA_neg);
                        m_sendOutputA(bOut);
                    }

                    clear_PA_Interrupts();

                    // TODO 6522 - implement pulse / handshake mode

                    break;

                case 2:
                    m_DDRB = b;
                    m_DDRB_neg = (byte)~b;
                    break;

                case 3: 
                    m_DDRA = b;
                    m_DDRA_neg = (byte)~b;
                    break;

                case 4:
                case 6:
                    m_T1LL = b;
                    break;

                case 5:
                    m_T1CH = m_T1LH = b;
                    m_T1CL = m_T1LL;

                    clearInterrupt(InterruptFlags.INT_T1);

                    if(m_PCR_set_PB7)
                    {
                        m_ORB &= 0x7F; // set PB7 low on timer start
                        if (m_sendOutputB != null)
                        {
                            // mix input and output
                            byte bOut = 0;
                            bOut |= (byte)(m_ORB & m_DDRB);
                            bOut |= (byte)(m_IRB & m_DDRB_neg);
                            m_sendOutputB(bOut);
                        }
                    }

                    m_T1_active = true;

                    break;

                case 7:
                    m_T1LH = b;
                    clearInterrupt(InterruptFlags.INT_T1);
                    break;

                case 8:
                    m_T2CL = b;
                    break;

                case 9:
                    m_T2CH = b;

                    clearInterrupt(InterruptFlags.INT_T2);

                    m_T2_active = true;

                    break;

                case 10:
                    m_SR = b;
                    updateControlRegisters();
                    break;

                case 11:
                    m_ACR = b;
                    updateControlRegisters();
                    break;

                case 12:
                    m_PCR = b;
                    updateControlRegisters();
                    break;

                case 13:
                    m_IFR = b;
                    updateControlRegisters();
                    break;

                case 14:
                    m_IER = b;
                    updateControlRegisters();
                    break;

                case 15: m_ANH = b; break;
            }
            
        }

        public byte read(UInt16 address)
        {
            byte Reg = (byte)(address & 0x0F);
            byte b = 0;

            switch (Reg)
            {
                case 0: b = m_IRB; break;
                case 1: b = m_IRA; break;
                case 2: b = m_DDRB; break;
                case 3: b = m_DDRA; break;
                case 4:
                    clearInterrupt(InterruptFlags.INT_T1);
                    b = m_T1CL;
                    break;

                case 5: b = m_T1CH; break;
                case 6: b = m_T1LL; break;
                case 7: b = m_T1LH; break;
                case 8:
                    clearInterrupt(InterruptFlags.INT_T2);
                    b = m_T2CL;
                    break;

                case 9: b = m_T2CH; break;
                case 10: b = m_SR; break;
                case 11: b = m_ACR; break;
                case 12: b = m_PCR; break;
                case 13: b = m_IFR; break;
                case 14: b = m_IER; break;
                case 15: b = m_ANH; break;
            }
            return b;
        }

        public void InputA(byte b)
        {
            m_IRA = b;
        }

        public void InputA(int bit, bool value)
        {
            if(bit >= 0 && bit <= 7)
            {
                byte mask = (byte)(1 << bit);
                if (value)
                    m_IRA |= mask;
                else
                    m_IRA &= (byte)~mask;
            }
            else
                throw new ApplicationException("VIA PA bit not 0..7");
        }

        public void InputB(byte b)
        {
            m_IRB = b;

        }

        public void InputB(int bit, bool value)
        {
            if (bit >= 0 && bit <= 7)
            {
                byte mask = (byte)(1 << bit);
                if (value)
                    m_IRA |= mask;
                else
                    m_IRA &= (byte)~mask;
            }
            else
                throw new ApplicationException("VIA PA bit not 0..7");
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

    }
}
