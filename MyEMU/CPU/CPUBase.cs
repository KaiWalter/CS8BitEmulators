using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MyEMU.CPU
{
    public enum Signal
    {
        Fall = 0,
        Rise = 1
    };

    public enum CPUState
    {
        fetchopcode,
        addressing,
        operation,
        interrupt
    }

    public enum InterruptSignal
    {
        None = 0,
        IRQ = 1,
        NMI = 2,
        BRK = 4
    }

    /// <summary>
    /// base class for all CPUs
    /// common characteristics
    /// - 16 bit address space
    /// - 16 bit program counter
    /// </summary>
    public class CPUBase
    {
        protected const byte b11111111 = 0xFF;
        protected const byte b10000000 = 0x80;
        protected const byte b01000000 = 0x40;
        protected const byte b00100000 = 0x20;
        protected const byte b00010000 = 0x10;
        protected const byte b00001000 = 0x08;
        protected const byte b00000100 = 0x04;
        protected const byte b00000010 = 0x02;
        protected const byte b00000001 = 0x01;

        protected UInt16 m_PC;                   // Program Counter
        protected UInt16 m_PC_Start;             // Program Counter (start of current OpCode)

        protected OperationCode[] m_OpCodes;     // table of OpCodes

        protected byte[] m_ram;                  // RAM
        protected byte[] m_ramIndirect;          // flag for indirect ram positions
        protected byte m_MemAccIndex;            // last used memory access index
        protected int m_ramOffset;               // address offset / start of RAM
        protected SortedDictionary<byte, MemoryAccess> m_MemAcc; // indirect memory access control structure


        public CPUBase()
        {
            m_ram = null;                      // no RAM installed
            m_ramOffset = 0;

            m_ramIndirect = new byte[0x10000]; // allocate 64k of address space
            Array.Clear(m_ramIndirect, 0, m_ramIndirect.Length);

            m_MemAcc = new SortedDictionary<byte, MemoryAccess>();
            m_MemAccIndex = 0;
        }

        #region MEMORY HANDLING
        public void installRAMBank(int Offset, int SizeKB)
        {
            m_ramOffset = Offset;

            int iNewSize = 0;

            if (m_ram != null) iNewSize = m_ram.Length;

            iNewSize += (SizeKB * 1024);

            m_ram = null; // dereference
            m_ram = new byte[iNewSize];

            Array.Clear(m_ram, 0, m_ram.Length);

        }

        public void installRAMBank(int SizeKB)
        {
            installRAMBank(0,SizeKB);
        }

        public void loadProgram(string sFilename, UInt16 address)
        {
            byte[] ProgramCode = Utilities.loadBinaryFile(sFilename);
            loadMemfromByteArray(ref ProgramCode, address);
        }

        public virtual void loadMemfromByteArray(ref byte[] bBytes, UInt16 address)
        {
            int iTargetAddress = address;

            for (int i = 0; i < bBytes.Length; i++)
                if ((iTargetAddress-m_ramOffset) < m_ram.Length)
                    m_ram[(iTargetAddress++)-m_ramOffset] = bBytes[i];
        }

        public void registerMemoryAccess(UInt16 address, MemReadHandler hRead, MemWriteHandler hWrite)
        {
            registerMemoryAccess(address, address, hRead, hWrite);
        }

        public void registerMemoryAccess(UInt16 addrStart, UInt16 addrEnd, MemReadHandler hRead, MemWriteHandler hWrite)
        {
            m_MemAccIndex++;

            MemoryAccess ma = new MemoryAccess();

            ma.addrStart = addrStart;
            ma.addrEnd = addrEnd;
            ma.doRead = hRead;
            ma.doWrite = hWrite;

            for (int i = addrStart; i <= addrEnd; i++)
                m_ramIndirect[i] = m_MemAccIndex;

            m_MemAcc.Add(m_MemAccIndex, ma);
        }

        public byte readMemByte(UInt16 address)
        {
            if (m_ramIndirect[address] == 0) // check for indirect memory access
            {
                int ram_address = address - m_ramOffset;
                if (ram_address >= 0 && ram_address < m_ram.Length)
                    return (byte)m_ram[address - m_ramOffset];
                else
                    return 0;
            }
            else
                return m_MemAcc[m_ramIndirect[address]].doRead(address);
        }

        public UInt16 readMemWord(UInt16 address)
        {
            byte l = readMemByte(address);
            byte h = readMemByte((UInt16)(address + 1));
            UInt16 w = (UInt16)(h << 8 | l);
            return w;
        }

        public void writeMemByte(UInt16 address, byte value)
        {
            if (m_ramIndirect[address] == 0) // check for indirect memory access
            {
                int ram_address = address - m_ramOffset;
                if (ram_address >= 0 && ram_address < m_ram.Length)
                    m_ram[address - m_ramOffset] = value;
            }
            else
                m_MemAcc[m_ramIndirect[address]].doWrite(address, value);
        }

        public void writeMemWord(UInt16 address, UInt16 w)
        {
            byte l = (byte)(w & 0x00FF);
            byte h = (byte)(w >> 8);
            writeMemByte(address, l);
            writeMemByte((UInt16)(address + 1), h);
        }

        /// <summary>
        /// getNextMemByte - get the next byte and then move the program counter forward
        /// </summary>
        /// <returns>next byte at program counter ram address</returns>
        internal byte getNextMemByte()
        {
            return readMemByte(m_PC++);;
        }

        /// <summary>
        /// getNextMemWord - get the next word (LLHH) and then move the program counter forward
        /// </summary>
        /// <returns>next byte at program counter ram address</returns>
        internal UInt16 getNextMemWord()
        {
            UInt16 w = readMemWord(m_PC);
            m_PC += 2;
            return w;
        }

        #endregion
    }

    public class CPU8Bit : CPUBase
    {
        protected CPUState m_State;              // current state of the CPU

        public CPU8Bit() : base()
        {
            m_PC = 0x0000;            // Program counter starts at $0

            m_OpCodes = new OperationCode[0x100];
        }

        public CPUState State
        {
            get { return m_State; }
        }

        public virtual void Reset() { }

        public virtual void Run() { }


    }

    #region RAM extension
    public class RAMextension
    {
        byte[] m_RAM;
        UInt16 m_Offset;

        public RAMextension(CPUBase cpu, UInt16 uiAddress, UInt16 SizeKB)
        {
            m_RAM = new byte[SizeKB * 1024];
            m_Offset = uiAddress;

            UInt16 uiEnd = uiAddress;
            uiEnd += (UInt16)m_RAM.Length;
            uiEnd--;

            cpu.registerMemoryAccess(uiAddress, uiEnd, read, write);
        }

        private byte read(UInt16 address)
        {
            return m_RAM[address - m_Offset];
        }

        private void write(UInt16 address, byte b)
        {
            m_RAM[address - m_Offset] = b;
        }
    }
    #endregion

    #region ROM
    public class ROMmodule
    {
        byte[] m_ROM;
        UInt16 m_Offset;

        public ROMmodule(CPUBase cpu, string sFilename, UInt16 uiAddress)
        {
            m_ROM = Utilities.loadBinaryFile(sFilename);
            m_Offset = uiAddress;

            UInt16 uiEnd = uiAddress;
            uiEnd += (UInt16)m_ROM.Length;
            uiEnd--;

            cpu.registerMemoryAccess(uiAddress,uiEnd,read,write);
        }

        private byte read(UInt16 address)
        {
            return m_ROM[address-m_Offset];
        }
        public void Patch(UInt16 address, byte b)
        {
            m_ROM[address - m_Offset] = b;
        }
        private void write(UInt16 address, byte b)
        {
            m_ROM[address - m_Offset] = b;
        }
    }
    #endregion

    #region Memory Access Handler
    public delegate void MemWriteHandler(UInt16 address, byte b);
    public delegate byte MemReadHandler(UInt16 address);

    public struct MemoryAccess
    {
        public UInt16 addrStart;
        public UInt16 addrEnd;

        public MemReadHandler doRead;
        public MemWriteHandler doWrite;
    }
    #endregion

    #region Adressing and Operation Handler
    public delegate void AddressingHandler();
    public delegate void OperationHandler();

    public struct OperationCode
    {
        public OperationCode(string sOpCode, int iBytes, int iCycles, AddressingHandler ha, OperationHandler ho)
        {
            this.OpCode = sOpCode;
            this.Bytes = iBytes;
            this.Cycles = iCycles;
            this.executeAddressing = ha;
            this.executeOperation = ho;
        }

        public string OpCode;
        public int Bytes;
        public int Cycles;
        public AddressingHandler executeAddressing;
        public OperationHandler executeOperation;
    }
    #endregion

    #region Send & Receive Handler
    public delegate void SendHandler(byte b);
    public delegate byte ReceiveHandler();
    public delegate void SendInterruptHandler(InterruptSignal intsig);
    #endregion


}
