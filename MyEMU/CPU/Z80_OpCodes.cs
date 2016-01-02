using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyEMU.CPU
{
    public partial class Z80 : CPU8Bit
    {
        private void initOpCodes8BitLoadGroup()
        {
            int index = 0;

            // LD r,r'
            // LD p,p'
            // LD q,q'
            for (int source = 0; source < 8; source++)
                for (int target = 0; target < 8; target++ )
                {
                    index = 0x40 | source | (target << 3);
                    if (source == 0x06)
                    {
                        // LD r,(HL)
                        // LD r,(IX+d)
                        // LD r,(IY+d)
                        m_OpCodes[index] = new OperationCode("LD", 1, 7, _ADDR_REGIND_HL, _LD_Reg_and_Ind_Helper);
                        m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 3, 19, _ADDR_IDX, _LD_Reg_and_Ind_Helper);
                        m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("LD", 3, 19, _ADDR_IDX, _LD_Reg_and_Ind_Helper);
                    }
                    else
                    {
                        m_OpCodes[index] = new OperationCode("LD", 1, 4, _ADDR_REG, _LD_Reg_and_Ind_Helper);
                        m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 2, 8, _ADDR_REG, _LD_Reg_and_Ind_Helper);
                        m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("LD", 2, 8, _ADDR_REG, _LD_Reg_and_Ind_Helper);
                    }
                }

            // LD r,n
            // LD p,n
            // LD q,n
            for (int target = 0; target <= 7; target++)
            {
                index = 0x06 | (target << 3);
                m_OpCodes[index] = new OperationCode("LD", 2, 7, _ADDR_IMM, _LD_Reg_and_Ind_Helper);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 3, 11, _ADDR_IMM, _LD_Reg_and_Ind_Helper);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 3, 11, _ADDR_IMM, _LD_Reg_and_Ind_Helper);
            }

            // LD (HL),r
            // LD (IX+d),r
            // LD (IY+d),r
            for (int source = 0; source <= 7; source++)
            {
                index = 0x70 | source;
                m_OpCodes[index] = new OperationCode("LD", 1, 7, _ADDR_REG, _LD_Reg_and_Ind_Helper);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 3, 19, _ADDR_DISP_REG, _LD_Reg_and_Ind_Helper);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("LD", 3, 19, _ADDR_DISP_REG, _LD_Reg_and_Ind_Helper);
            }

            // LD (HL),n
            // LD (IX+d),n
            // LD (IY+d),n
            m_OpCodes[0x36] = new OperationCode("LD", 2, 10, _ADDR_IMM, _LD_Reg_and_Ind_Helper);
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x36] = new OperationCode("LD", 4, 19, _ADDR_DISP_IMM, _LD_Reg_and_Ind_Helper);
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0x36] = new OperationCode("LD", 4, 19, _ADDR_DISP_IMM, _LD_Reg_and_Ind_Helper);

            // LD A,(BC)
            m_OpCodes[0x0A] = new OperationCode("LD", 1, 7, _ADDR_REGIND_BC, _LD_A_Helper);
            // LD A,(DE)
            m_OpCodes[0x1A] = new OperationCode("LD", 1, 7, _ADDR_REGIND_DE, _LD_A_Helper);
            // LD A,(nn)
            m_OpCodes[0x3A] = new OperationCode("LD", 3, 13, _ADDR_EXT1_8Bit, _LD_A_Helper);

            // LD (BC), A
            m_OpCodes[0x02] = new OperationCode("LD", 1, 7, _ADDR_IMP, _LD_indBC_A);
            // LD (DE), A
            m_OpCodes[0x12] = new OperationCode("LD", 1, 7, _ADDR_IMP, _LD_indDE_A);
            // LD (nn), A
            m_OpCodes[0x32] = new OperationCode("LD", 3, 13, _ADDR_IMP, _LD_indnn_A);

            // LD A,I
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x57] = new OperationCode("LD", 2, 9, _ADDR_IMP, _LD_A_I);
            // LD A,R
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x5F] = new OperationCode("LD", 2, 9, _ADDR_IMP, _LD_A_R);
            // LD I,A
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x47] = new OperationCode("LD", 2, 9, _ADDR_IMP, _LD_I_A);
            // LD R,A
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x4F] = new OperationCode("LD", 2, 9, _ADDR_IMP, _LD_R_A);

        }
        private void initOpCodes16BitLoadGroup()
        {
            int index = 0;

            // LD dd,nn
            for (int target = 0; target <= 3; target++)
            {
                index = 0x01 | (target << 4);
                m_OpCodes[index] = new OperationCode("LD", 3, 10, _ADDR_IMMEXT, _LD_dd_nn);
            }

            // LD IX,nn
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x21] = new OperationCode("LD", 4, 14, _ADDR_IMMEXT, _LD_IX_nn);
            // LD IY,nn
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0x21] = new OperationCode("LD", 4, 14, _ADDR_IMMEXT, _LD_IY_nn);

            // LD HL,(nn)
            m_OpCodes[0x2A] = new OperationCode("LD", 3, 16, _ADDR_EXT1_16Bit, _LD_HLIXIY_indnn);

            // LD dd,(nn)
            for (int target = 0; target <= 3; target++)
            {
                index = 0x4B | (target << 4);
                m_OpCodes[m_dictOpCodePrefix[0xED] + index] = new OperationCode("LD", 4, 20, _ADDR_EXT1_16Bit, _LD_dd_nn);
            }

            // LD IX,(nn)
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x2A] = new OperationCode("LD", 4, 20, _ADDR_EXT1_16Bit, _LD_HLIXIY_indnn);
            // LD IY,(nn)
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0x2A] = new OperationCode("LD", 4, 20, _ADDR_EXT1_16Bit, _LD_HLIXIY_indnn);

            // LD (nn),HL
            m_OpCodes[0x22] = new OperationCode("LD", 3, 16, _ADDR_IMMEXT, _LD_indnn);
            // LD (nn),dd
            for (int source = 0; source <= 3; source++)
            {
                index = 0x43 | (source << 4);
                m_OpCodes[m_dictOpCodePrefix[0xED] + index] = new OperationCode("LD", 3, 20, _ADDR_IMMEXT, _LD_indnn);
            }
            // LD (nn),IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0x22] = new OperationCode("LD", 3, 20, _ADDR_IMMEXT, _LD_indnn);
            // LD (nn),IY
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0x22] = new OperationCode("LD", 3, 20, _ADDR_IMMEXT, _LD_indnn);

            // LD SP,HL
            m_OpCodes[0xF9] = new OperationCode("LD", 1, 6, _ADDR_IMP, _LD_SP_HL);
            // LD SP,IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0xF9] = new OperationCode("LD", 2, 10, _ADDR_IMP, _LD_SP_HL);
            // LD SP,IY
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0xF9] = new OperationCode("LD", 2, 10, _ADDR_IMP, _LD_SP_HL);

            // PUSH gg
            for (int source = 0; source <= 3; source++)
            {
                index = 0xC5 | (source << 4);
                m_OpCodes[index] = new OperationCode("PUSH", 1, 11, _ADDR_IMP, _PUSH);
            }
            // PUSH IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0xE5] = new OperationCode("PUSH", 2, 15, _ADDR_IMP, _PUSH);
            // PUSH IY
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0xE5] = new OperationCode("PUSH", 2, 15, _ADDR_IMP, _PUSH);

            // POP gg
            for (int target = 0; target <= 3; target++)
            {
                index = 0xC1 | (target << 4);
                m_OpCodes[index] = new OperationCode("POP", 1, 10, _ADDR_IMP, _POP);
            }
            // POP IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0xE1] = new OperationCode("POP", 2, 14, _ADDR_IMP, _POP);
            // POP IY
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0xE1] = new OperationCode("POP", 2, 14, _ADDR_IMP, _POP);


        }
        private void initOpCodesExchangeGroup()
        {
            // EX DE,HL
            m_OpCodes[0xEB] = new OperationCode("EX", 1, 4, _ADDR_IMP, _EX_DE_HL);
            // EX AF,AF'
            m_OpCodes[0x08] = new OperationCode("EX", 1, 4, _ADDR_IMP, _EX_AF_AFAlt);
            // EXX
            m_OpCodes[0xD9] = new OperationCode("EXX", 1, 4, _ADDR_IMP, _EXX);
            // EX (SP),HL
            m_OpCodes[0xE3] = new OperationCode("EX", 1, 19, _ADDR_IMP, _EX_indSP_HLIXIY);
            // EX (SP),IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0xE3] = new OperationCode("EX", 2, 23, _ADDR_IMP, _EX_indSP_HLIXIY);
            // EX (SP),IY
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0xE3] = new OperationCode("EX", 2, 23, _ADDR_IMP, _EX_indSP_HLIXIY);

        }
        private void initOpCodesBlockTransferGroup()
        {
            // LDI
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xA0] = new OperationCode("LDI", 2, 16, _ADDR_IMP, _LDI);
            // LDIR
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xB0] = new OperationCode("LDIR", 2, 16, _ADDR_IMP, _LDIR);
            // LDD
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xA8] = new OperationCode("LDD", 2, 16, _ADDR_IMP, _LDD);
            // LDDR
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xB8] = new OperationCode("LDDR", 2, 16, _ADDR_IMP, _LDDR);
        }
        private void initOpCodesSearchGroup()
        {
            // CPI
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xA1] = new OperationCode("CPI", 2, 16, _ADDR_IMP, _CPI);
            // CPIR
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xB1] = new OperationCode("CPIR", 2, 16, _ADDR_IMP, _CPIR);
            // CPD
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xA9] = new OperationCode("CPD", 2, 16, _ADDR_IMP, _CPD);
            // CPDR
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0xB9] = new OperationCode("CPDR", 2, 16, _ADDR_IMP, _CPDR);
        }
        private void initOpCodes8BitArithLogGroup()
        {
            int index = 0;

            // ADD A,r
            // ADD A,p
            // ADD A,q
            for (int source = 0; source <= 7; source++)
            {
                index = 0x80 | source;
                if (source == 6)
                {
                    // ADD A, (HL)
                    m_OpCodes[index] = new OperationCode("ADD", 1, 7, _ADDR_REGIND_HL, _ADD_A);
                    // ADD A, (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("ADD", 3, 19, _ADDR_IDX, _ADD_A);
                    // ADD A, (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("ADD", 3, 19, _ADDR_IDX, _ADD_A);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("ADD", 1, 4, _ADDR_REG, _ADD_A);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("ADD", 2, 8, _ADDR_REG, _ADD_A);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("ADD", 2, 8, _ADDR_REG, _ADD_A);
                }
            }

            // ADD A,n
            m_OpCodes[0x80 | 0x46] = new OperationCode("ADD", 2, 8, _ADDR_IMM, _ADD_A);

            // ADC A,r
            // ADC A,p
            // ADC A,q
            for (int source = 0; source <= 7; source++)
            {
                index = 0x88 | source;
                if (source == 6)
                {
                    // ADC A, (HL)
                    m_OpCodes[index] = new OperationCode("ADC", 1, 7, _ADDR_REGIND_HL, _ADC_A);
                    // ADC A, (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("ADC", 3, 19, _ADDR_IDX, _ADC_A);
                    // ADC A, (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("ADC", 3, 19, _ADDR_IDX, _ADC_A);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("ADC", 1, 4, _ADDR_REG, _ADC_A);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("ADC", 2, 8, _ADDR_REG, _ADC_A);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("ADC", 2, 8, _ADDR_REG, _ADC_A);
                }
            }

            // ADC A,n
            m_OpCodes[0x88 | 0x46] = new OperationCode("ADC", 2, 8, _ADDR_IMM, _ADC_A);


            // SUB A,r
            // SUB A,p
            // SUB A,q
            for (int source = 0; source <= 7; source++)
            {
                index = 0x90 | source;
                if (source == 6)
                {
                    // SUB A, (HL)
                    m_OpCodes[index] = new OperationCode("SUB", 1, 7, _ADDR_REGIND_HL, _SUB_A);
                    // SUB A, (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("SUB", 3, 19, _ADDR_IDX, _SUB_A);
                    // SUB A, (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("SUB", 3, 19, _ADDR_IDX, _SUB_A);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("SUB", 1, 4, _ADDR_REG, _SUB_A);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("SUB", 2, 8, _ADDR_REG, _SUB_A);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("SUB", 2, 8, _ADDR_REG, _SUB_A);
                }
            }

            // SUB A,n
            m_OpCodes[0x90 | 0x46] = new OperationCode("SUB", 2, 8, _ADDR_IMM, _SUB_A);

            // SBC A,r
            // SBC A,p
            // SBC A,q
            for (int source = 0; source <= 7; source++)
            {
                index = 0x98 | source;
                if (source == 6)
                {
                    // SBC A, (HL)
                    m_OpCodes[index] = new OperationCode("SBC", 1, 7, _ADDR_REGIND_HL, _SBC_A);
                    // SBC A, (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("SBC", 3, 19, _ADDR_IDX, _SBC_A);
                    // SBC A, (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("SBC", 3, 19, _ADDR_IDX, _SBC_A);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("SBC", 1, 4, _ADDR_REG, _SBC_A);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("SBC", 2, 8, _ADDR_REG, _SBC_A);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("SBC", 2, 8, _ADDR_REG, _SBC_A);
                }
            }

            // SBC A,n
            m_OpCodes[0x98 | 0x46] = new OperationCode("SBC", 2, 8, _ADDR_IMM, _SBC_A);


            // AND r
            // AND p
            // AND q
            for (int source = 0; source <= 7; source++)
            {
                index = 0xA0 | source;
                if (source == 6)
                {
                    // AND (HL)
                    m_OpCodes[index] = new OperationCode("AND", 1, 7, _ADDR_REGIND_HL, _AND);
                    // AND (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("AND", 3, 19, _ADDR_IDX, _AND);
                    // AND (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("AND", 3, 19, _ADDR_IDX, _AND);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("AND", 1, 4, _ADDR_REG, _AND);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("AND", 2, 8, _ADDR_REG, _AND);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("AND", 2, 8, _ADDR_REG, _AND);
                }
            }

            // AND n
            m_OpCodes[0xA0 | 0x46] = new OperationCode("AND", 2, 8, _ADDR_IMM, _AND);

            // OR r
            // OR p
            // OR q
            for (int source = 0; source <= 7; source++)
            {
                index = 0xB0 | source;
                if (source == 6)
                {
                    // OR (HL)
                    m_OpCodes[index] = new OperationCode("OR", 1, 7, _ADDR_REGIND_HL, _OR);
                    // OR (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("OR", 3, 19, _ADDR_IDX, _OR);
                    // OR (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("OR", 3, 19, _ADDR_IDX, _OR);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("OR", 1, 4, _ADDR_REG, _OR);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("OR", 2, 8, _ADDR_REG, _OR);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("OR", 2, 8, _ADDR_REG, _OR);
                }
            }

            // OR n
            m_OpCodes[0xB0 | 0x46] = new OperationCode("OR", 2, 8, _ADDR_IMM, _OR);

            // XOR r
            // XOR p
            // XOR q
            for (int source = 0; source <= 7; source++)
            {
                index = 0xA8 | source;
                if (source == 6)
                {
                    // XOR (HL)
                    m_OpCodes[index] = new OperationCode("XOR", 1, 7, _ADDR_REGIND_HL, _XOR);
                    // XOR (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("XOR", 3, 19, _ADDR_IDX, _XOR);
                    // XOR (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("XOR", 3, 19, _ADDR_IDX, _XOR);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("XOR", 1, 4, _ADDR_REG, _XOR);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("XOR", 2, 8, _ADDR_REG, _XOR);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("XOR", 2, 8, _ADDR_REG, _XOR);
                }
            }

            // XOR n
            m_OpCodes[0xA8 | 0x46] = new OperationCode("XOR", 2, 8, _ADDR_IMM, _XOR);

            // CP r
            // CP p
            // CP q
            for (int source = 0; source <= 7; source++)
            {
                index = 0xB8 | source;
                if (source == 6)
                {
                    // CP (HL)
                    m_OpCodes[index] = new OperationCode("CP", 1, 7, _ADDR_REGIND_HL, _CP);
                    // CP (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("CP", 3, 19, _ADDR_IDX, _CP);
                    // CP (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("CP", 3, 19, _ADDR_IDX, _CP);
                }
                else
                {
                    m_OpCodes[index] = new OperationCode("CP", 1, 4, _ADDR_REG, _CP);
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("CP", 2, 8, _ADDR_REG, _CP);
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("CP", 2, 8, _ADDR_REG, _CP);
                }
            }

            // CP n
            m_OpCodes[0xB8 | 0x46] = new OperationCode("CP", 2, 8, _ADDR_IMM, _CP);

            // INC r
            // INC p
            // INC q
            // INC (HL)
            // INC (IX+d)
            // INC (IY+d)
            for (int source = 0; source <= 7; source++)
            {
                index = 0x04 | (source << 3);
                m_OpCodes[index] = new OperationCode("INC", 1, 4, _ADDR_IMP, _INC);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("INC", 2, 8, _ADDR_IMP, _INC);
                m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("INC", 2, 8, _ADDR_IMP, _INC);
            }

            // DEC r
            // DEC p
            // DEC q
            // DEC (HL)
            // DEC (IX+d)
            // DEC (IY+d)
            for (int source = 0; source <= 7; source++)
            {
                index = 0x05 | (source << 3);
                m_OpCodes[index] = new OperationCode("DEC", 1, 4, _ADDR_IMP, _DEC);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("DEC", 2, 8, _ADDR_IMP, _DEC);
                m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("DEC", 2, 8, _ADDR_IMP, _DEC);
            }

        }
        private void initOpCodes16BitArithLogGroup()
        {
            int index = 0;

            // ADD HL,ss
            // ADC HL,ss
            // SBC HL,ss
            // ADD IX,ss
            // ADD IY,ss
            for (int source = 0; source < 4; source++)
            {
                index = 0x09 | (source << 4);
                m_OpCodes[index] = new OperationCode("ADD", 1, 11, _ADDR_IMP, _ADD_HL_ss);
                index = 0x4A | (source << 4);
                m_OpCodes[m_dictOpCodePrefix[0xED] + index] = new OperationCode("ADC", 2, 15, _ADDR_IMP, _ADC_HL_ss);
                index = 0x42 | (source << 4);
                m_OpCodes[m_dictOpCodePrefix[0xED] + index] = new OperationCode("SBC", 2, 15, _ADDR_IMP, _SBC_HL_ss);
                index = 0x09 | (source << 4);
                m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("ADD", 2, 15, _ADDR_IMP, _ADD_IX_ss);
                m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("ADD", 2, 15, _ADDR_IMP, _ADD_IY_ss);
            }

            // INC ss
            for(int source=0; source < 4; source++)
            {
                index = 0x03 | (source << 4);
                m_OpCodes[index] = new OperationCode("INC", 1, 6, _ADDR_IMP, _INC_ss);
                if(source == 2)
                {
                    // INC IX
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("INC", 2, 10, _ADDR_IMP, _INC_ss);
                    // INC IY
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("INC", 2, 10, _ADDR_IMP, _INC_ss);
                }
            }

            // DEC ss
            for (int source = 0; source < 4; source++)
            {
                index = 0x0B | (source << 4);
                m_OpCodes[index] = new OperationCode("DEC", 1, 6, _ADDR_IMP, _DEC_ss);
                if (source == 2)
                {
                    // DEC IX
                    m_OpCodes[m_dictOpCodePrefix[0xDD] + index] = new OperationCode("DEC", 2, 10, _ADDR_IMP, _DEC_ss);
                    // DEC IY
                    m_OpCodes[m_dictOpCodePrefix[0xFD] + index] = new OperationCode("DEC", 2, 10, _ADDR_IMP, _DEC_ss);
                }
            }
        }

        private void initOpCodesJumpGroup()
        {
            // JP nn
            m_OpCodes[0xC3] = new OperationCode("JP", 3, 3, _ADDR_IMMEXT, _JP);

            // JP cc, nn
            for (int operands = 0; operands < 8; operands++)
            {
                int index = 0xC2 | (operands << 3);
                m_OpCodes[index] = new OperationCode("JP", 1, 10, _ADDR_IMMEXT, _JP_cc_nn);
            }

            // JR e
            m_OpCodes[0x18] = new OperationCode("JR", 3, 3, _ADDR_IMM, _JR_e);

            // JR ss, e
            for (int operands = 4; operands <= 7; operands++)
            {
                int index = 0x00 | (operands << 3);
                m_OpCodes[index] = new OperationCode("JR", 1, 7, _ADDR_IMM, _JR);
            }

            // JP HL
            m_OpCodes[0xE9] = new OperationCode("JP", 1, 4, _ADDR_IMP, _JP_HLIXIY);
            // JP IX
            m_OpCodes[m_dictOpCodePrefix[0xDD] + 0xE9] = new OperationCode("JP", 2, 8, _ADDR_IMP, _JP_HLIXIY);
            // JP HL
            m_OpCodes[m_dictOpCodePrefix[0xFD] + 0xE9] = new OperationCode("JP", 2, 8, _ADDR_IMP, _JP_HLIXIY);
        }

        private void initOpCodesCallAndReturnGroup()
        {
            int index = 0;

            // CALL nn
            m_OpCodes[0xCD] = new OperationCode("CALL", 3, 17, _ADDR_IMMEXT, _CALL);

            // CALL cc, nn
            for (int condition = 0; condition < 8; condition++)
            {
                index = 0xC4 | (condition << 3);
                m_OpCodes[index] = new OperationCode("CALL", 3, 10, _ADDR_IMMEXT, _CALL_cc_nn);
            }

            // RET
            m_OpCodes[0xC9] = new OperationCode("RET", 3, 17, _ADDR_IMP, _RET);

            // RET cc
            for (int source = 0; source <= 7; source++)
            {
                index = 0xC0 | (source << 3);
                m_OpCodes[index] = new OperationCode("RET", 1, 5, _ADDR_IMP, _RET_cc);
            }

            // RETI
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0x4D] = new OperationCode("RETI", 2, 14, _ADDR_IMP, _RETI);
            // RETN
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0x45] = new OperationCode("RETN", 2, 14, _ADDR_IMP, _RETI);

            // RST p
            for (int source = 0; source <= 7; source++)
            {
                index = 0xC7 | (source << 3);
                m_OpCodes[index] = new OperationCode("RST", 1, 7, _ADDR_IMP, _RST);
            }
        }

        private void initOpCodesGenPurposeArithGroup()
        {
            // DAA
            m_OpCodes[0x27] = new OperationCode("DAA", 1, 4, _ADDR_IMP, _DAA);
            // CPL
            m_OpCodes[0x2F] = new OperationCode("CPL", 1, 4, _ADDR_IMP, _CPL);
        }

        private void initOpCodesCPUControlGroup()
        {
            // CCF
            m_OpCodes[0x3F] = new OperationCode("CCF", 1, 4, _ADDR_IMP, _CCF);
            // SCF
            m_OpCodes[0x37] = new OperationCode("SCF", 1, 4, _ADDR_IMP, _SCF);

            // NOP
            m_OpCodes[0x00] = new OperationCode("NOP", 1, 7, _ADDR_IMP, _NOP);
            // HALT
            m_OpCodes[0x76] = new OperationCode("HALT", 1, 1, _ADDR_IMP, _HALT);

            // DI
            m_OpCodes[0xF3] = new OperationCode("DI", 1, 4, _ADDR_IMP, _DI);
            // EI
            m_OpCodes[0xFB] = new OperationCode("EI", 1, 4, _ADDR_IMP, _EI);

            // IM 0
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0x46] = new OperationCode("IM", 2, 8, _ADDR_IMP, _IM);
            // IM 1
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0x56] = new OperationCode("IM", 2, 8, _ADDR_IMP, _IM);
            // IM 2
            m_OpCodes[m_dictOpCodePrefix[0xED] + 0x5E] = new OperationCode("IM", 2, 8, _ADDR_IMP, _IM);

        }

        private void initOpCodesRotateAndShiftGroup()
        {
            // RLCA
            m_OpCodes[0x07] = new OperationCode("RLCA", 1, 4, _ADDR_IMP, _RLCA);
            // RLA
            m_OpCodes[0x17] = new OperationCode("RLA", 1, 4, _ADDR_IMP, _RLA);
            // RRCA
            m_OpCodes[0x0F] = new OperationCode("RRCA", 1, 4, _ADDR_IMP, _RRCA);
            // RRA
            m_OpCodes[0x1F] = new OperationCode("RRA", 1, 4, _ADDR_IMP, _RRA);

            // RLC r
            for (int reg = 0; reg < 8; reg++)
            {
                if(reg == 6)
                {
                    // RLC (HL)
                    m_OpCodes[m_dictOpCodePrefix[0xCB] + reg] = new OperationCode("RLC", 2, 15, _ADDR_IMP, _RLC_r);
                    // RLC (IX+d)
                    m_OpCodes[m_dictOpCodePrefix[0xDDCB] + reg] = new OperationCode("RLC", 4, 23, _ADDR_IMP, _RLC_r);
                    // RLC (IY+d)
                    m_OpCodes[m_dictOpCodePrefix[0xFDCB] + reg] = new OperationCode("RLC", 4, 23, _ADDR_IMP, _RLC_r);
                }
                else
                    m_OpCodes[m_dictOpCodePrefix[0xCB] + reg] = new OperationCode("RLC", 2, 8, _ADDR_IMP, _RLC_r);
            }

        }

        private void initOpCodesBITManipulationGroup()
        {
            int index = 0;

            // BIT b, r
            for (int reg = 0; reg < 8; reg++)
                for(int bit = 0; bit < 8; bit++)
                {
                    index = 0x40 | (bit << 3) | reg;

                    if (reg == 6)
                    {
                        // BIT b, (HL)
                        m_OpCodes[m_dictOpCodePrefix[0xCB] + index] = new OperationCode("BIT", 2, 12, _ADDR_IMP, _BIT_b_r);
                        // BIT b, (IX+d)
                        m_OpCodes[m_dictOpCodePrefix[0xDDCB] + index] = new OperationCode("BIT", 4, 20, _ADDR_IMP, _BIT_b_r);
                        // BIT b, (IY+d)
                        m_OpCodes[m_dictOpCodePrefix[0xFDCB] + index] = new OperationCode("BIT", 4, 20, _ADDR_IMP, _BIT_b_r);
                    }
                    else
                        m_OpCodes[m_dictOpCodePrefix[0xCB] + index] = new OperationCode("BIT", 2, 8, _ADDR_IMP, _BIT_b_r);


                }

        }

        private void initOpCodesInputAndOutputGroup()
        {
            // IN A,(n)
            m_OpCodes[0xDB] = new OperationCode("IN", 2, 11, _ADDR_IMM, _IN);

            // OUT (n),A
            m_OpCodes[0xD3] = new OperationCode("OUT", 2, 11, _ADDR_IMM, _OUT);

        }

        #region generic & common operations
        private void genericRegisterIndexOperation(Func<byte, byte> doOperation,
                                                    int iShift = 0, int iMask = 7)
        {
            genericRegisterIndexOperation(doOperation, doOperation, iShift, iMask);
        }

        private void genericRegisterIndexOperation(Func<byte, byte> doOperation,
                                                    Func<byte, byte> doOperationIndexed,
                                                    int iShift = 0, int iMask = 7)
        {
            int Register = (m_OpCodeIndex >> iShift) & iMask;
            switch (Register)
            {
                case 0: m_reg_B = doOperation(m_reg_B); break;
                case 1: m_reg_C = doOperation(m_reg_C); break;
                case 2: m_reg_D = doOperation(m_reg_D); break;
                case 3: m_reg_E = doOperation(m_reg_E); break;
                case 4: m_reg_H = doOperation(m_reg_H); break;
                case 5: m_reg_L = doOperation(m_reg_L); break;
                case 6: switch (m_OpCodePrefix)
                    {
                        case 0xDDCB:
                            m_wordValue = m_reg_IX;
                            m_wordValue += m_byteDisplacement;
                            break;

                        case 0xFDCB:
                            m_wordValue = m_reg_IY;
                            m_wordValue += m_byteDisplacement;
                            break;

                        default:
                            m_wordValue = HL;
                            break;
                    }
                    writeMemByte(m_wordValue, doOperationIndexed(readMemByte(m_wordValue)));
                    break;
                case 7: m_reg_A = doOperation(m_reg_A); break;
            }
        }

        #endregion

        #region Operations
        private void _ADC_A()
        {
            int carry = m_Flag_C ? 1 : 0;
            Flags = m_precalc_ADCf[m_reg_A + m_byteValue * 0x100 + carry * 0x10000];
            m_reg_A += m_byteValue;
            m_reg_A += (byte)carry;
        }
        private void _ADC_HL_ss()
        {
            int regs = (m_OpCodeIndex >> 4) & 3;

            switch (regs)
            {
                case 0: m_wordValue = BC; break;
                case 1: m_wordValue = DE; break;
                case 2: m_wordValue = HL; break;
                case 3: m_wordValue = m_reg_SP; break;
            }

            int carry = m_Flag_C ? 1 : 0;
            uint result = (uint)((HL & 0xFFFF) + (m_wordValue & 0xFFFF) + carry);
            int result_signed = (short)HL + (short)m_wordValue + carry;

            m_Flag_N = false;
            m_Flag_C = (result & 0x10000) != 0;
            m_Flag_Z = (result & 0xFFFF) == 0;
            m_Flag_S = (result & 0x8000) != 0;
            m_Flag_H = (((HL & 0x0FFF) + (m_wordValue & 0x0FFF) + carry) & 0x1000) != 0;
            m_Flag_PV = (result_signed < -0x8000 || result_signed >= 0x8000);
            setUnusedFlags((byte)(result >> 8));

            HL = (ushort)result;
        }
        private void _ADD_A()
        {
            Flags = m_precalc_ADCf[m_reg_A + m_byteValue * 0x100];
            m_reg_A += m_byteValue;
        }
        private void _ADD_HL_ss()
        {
            int regs = (m_OpCodeIndex >> 4) & 3;

            switch(regs)
            {
                case 0: m_wordValue = BC; break;
                case 1: m_wordValue = DE; break;
                case 2: m_wordValue = HL; break;
                case 3: m_wordValue = m_reg_SP; break;
            }

            uint result = (uint)((HL & 0xFFFF) + (m_wordValue & 0xFFFF));

            m_Flag_N = false;
            m_Flag_C = (result & 0x10000) != 0;
            m_Flag_H = (((HL & 0x0FFF) + (m_wordValue & 0x0FFF)) & 0x1000) != 0;
            setUnusedFlags((byte)(result >> 8));

            HL = (ushort)result;

#if CPU_TRACE
            m_ei.Op1 = "HL";
            m_ei.Op2 = m_regNames[48 + regs];
#endif
        }
        private void _ADD_IX_ss()
        {
            int regs = (m_OpCodeIndex >> 4) & 3;

            switch (regs)
            {
                case 0: m_wordValue = BC; break;
                case 1: m_wordValue = DE; break;
                case 2: m_wordValue = m_reg_IX; break;
                case 3: m_wordValue = m_reg_SP; break;
            }

            uint result = (uint)((m_reg_IX & 0xFFFF) + (m_wordValue & 0xFFFF));

            m_Flag_N = false;
            m_Flag_C = (result & 0x10000) != 0;
            m_Flag_H = (((m_reg_IX & 0x0FFF) + (m_wordValue & 0x0FFF)) & 0x1000) != 0;
            setUnusedFlags((byte)(result >> 8));

            m_reg_IX = (ushort)result;

#if CPU_TRACE
            m_ei.Op1 = "HL";
            m_ei.Op2 = m_regNames[48 + regs];
#endif
        }
        private void _ADD_IY_ss()
        {
            int regs = (m_OpCodeIndex >> 4) & 3;

            switch (regs)
            {
                case 0: m_wordValue = BC; break;
                case 1: m_wordValue = DE; break;
                case 2: m_wordValue = m_reg_IY; break;
                case 3: m_wordValue = m_reg_SP; break;
            }

            uint result = (uint)((m_reg_IY & 0xFFFF) + (m_wordValue & 0xFFFF));

            m_Flag_N = false;
            m_Flag_C = (result & 0x10000) != 0;
            m_Flag_H = (((m_reg_IY & 0x0FFF) + (m_wordValue & 0x0FFF)) & 0x1000) != 0;
            setUnusedFlags((byte)(result >> 8));

            m_reg_IY = (ushort)result;

#if CPU_TRACE
            m_ei.Op1 = "IY";
            m_ei.Op2 = m_regNames[48 + regs];
#endif
        }
        private void _AND()
        {
            m_reg_A &= m_byteValue;
            Flags = m_precalcLog_f[m_reg_A];
            m_Flag_H = true;
        }
        private void _BIT_b_r()
        {
            Func<byte, byte> doBIT_reg = b =>
            {
                int bit = (m_OpCodeIndex >> 3) & 7;
                int bitmask = 1 << bit;
                int result = b & bitmask;

                m_Flag_Z = m_Flag_PV = result == 0;
                m_Flag_S = (result & (int)ZFLAGS.S) != 0;
                m_Flag_F5 = (b & (byte)ZFLAGS.F5) != 0;
                m_Flag_F3 = (b & (byte)ZFLAGS.F3) != 0;
                m_Flag_H = true;
                m_Flag_N = false;

#if CPU_TRACE
                m_ei.Op1 = bit.ToString();
#endif
                return b;
            };

            Func<byte, byte> doBIT_indexed = b =>
            {
                int bit = (m_OpCodeIndex >> 3) & 7;
                int bitmask = 1 << bit;
                int result = b & bitmask;

                m_Flag_Z = m_Flag_PV = result == 0;
                m_Flag_S = (result & (int)ZFLAGS.S) != 0;
                m_Flag_F5 = (m_wordValue & (byte)ZFLAGS.F5) != 0;
                m_Flag_F3 = (m_wordValue & (byte)ZFLAGS.F3) != 0;
                m_Flag_H = true;
                m_Flag_N = false;

#if CPU_TRACE
                m_ei.Op1 = bit.ToString();
#endif
                return b;
            };

            genericRegisterIndexOperation(doBIT_reg, doBIT_indexed);
        }
        private void _CALL()
        {
            m_reg_SP -= 2;
            writeMemWord(m_reg_SP, m_PC);
            m_PC = m_wordValue;
        }

        private void _CALL_cc_nn()
        {
            int condition = (m_OpCodeIndex >> 3) & 7;

            bool IsTrue = false;

            switch (condition)
            {
                case 0:
                    IsTrue = !m_Flag_Z;
                    break;
                case 1:
                    IsTrue = m_Flag_Z;
                    break;
                case 2:
                    IsTrue = !m_Flag_C;
                    break;
                case 3:
                    IsTrue = m_Flag_C;
                    break;
                case 4:
                    IsTrue = !m_Flag_PV;
                    break;
                case 5:
                    IsTrue = m_Flag_PV;
                    break;
                case 6:
                    IsTrue = !m_Flag_S;
                    break;
                case 7:
                    IsTrue = m_Flag_S;
                    break;
            }

            if (IsTrue)
            {
                if (m_ExtraCycles)
                {
                    _CALL();
                }
                else
                {
                    m_MaxCycles += 7;
                    m_ExtraCycles = true;
                }

            }

#if CPU_TRACE
            string[] conditions = new string[] { "NZ", "Z", "NC", "C", "PO", "PE", "P", "M" };
            m_ei.Op1 = conditions[condition];
#endif
        }
        private void _CCF()
        {
            setUnusedFlags(m_reg_A);
            m_Flag_H = m_Flag_C;
            m_Flag_C = !m_Flag_C;
            m_Flag_N = false;

        }
        private void _CP()
        {
            Flags = m_precalc_CPf[m_reg_A * 0x100 + m_byteValue];
        }
        private void _CPI()
        {
            int result = m_reg_A - readMemByte(HL);
            int resultCopy = result - m_reg_H;

            HL++;
            BC--;

            m_Flag_H = (((m_reg_A & 0xF) - (m_byteValue & 0xF)) & 0x10) != 0;
            setUnusedFlags((byte)(resultCopy & 0xFF));
            m_Flag_N = true;
            m_Flag_S = result < 0;
            m_Flag_Z = result == 0;
            m_Flag_PV = BC != 0;
        }
        private void _CPIR()
        {
            _CPI();

            if(m_Flag_PV && !m_Flag_Z)
            {
                if (m_ExtraCycles) // already in extra cycles
                {
                    m_MaxCycles += 21;
                }
                else // first extra cycle - only add uplift from LDI to LDIR
                {
                    m_MaxCycles += 5;
                    m_ExtraCycles = true;
                }
            }
        }
        private void _CPD()
        {
            int result = m_reg_A - readMemByte(HL);
            int resultCopy = result - m_reg_H;

            HL--;
            BC--;

            m_Flag_H = (((m_reg_A & 0xF) - (m_byteValue & 0xF)) & 0x10) != 0;
            setUnusedFlags((byte)(resultCopy & 0xFF));
            m_Flag_N = true;
            m_Flag_S = result < 0;
            m_Flag_Z = result == 0;
            m_Flag_PV = BC != 0;
        }
        private void _CPDR()
        {
            _CPD();

            if (m_Flag_PV && !m_Flag_Z)
            {
                if (m_ExtraCycles) // already in extra cycles
                {
                    m_MaxCycles += 21;
                }
                else // first extra cycle - only add uplift from LDI to LDIR
                {
                    m_MaxCycles += 5;
                    m_ExtraCycles = true;
                }
            }
        }
        private void _CPL()
        {
            m_reg_A = (byte)~m_reg_A;
            setUnusedFlags(m_reg_A);
            m_Flag_H = m_Flag_N = true;
        }
        private void _DAA()
        {
            int flagmask = (m_Flag_C ? 1 : 0) +
                (m_Flag_N ? 2 : 0) +
                (m_Flag_H ? 4 : 0);
            ushort result = m_precalc_DAA[m_reg_A + 0x100 * flagmask];
            AF = result;
        }
        private void _DEC()
        {
            Func<byte,byte> doDEC = b =>
            {
                Flags = (byte)((m_precalc_DECf[b]) | (byte)(m_Flag_C ? ZFLAGS.C : 0));
                b--;
                return b;
            };

            genericRegisterIndexOperation(doDEC,3);
        }
        private void _DEC_ss()
        {
            int target = (m_OpCodeIndex >> 4) & 3;
            switch (target)
            {
                case 0: BC--; break;
                case 1: DE--; break;
                case 2: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            m_reg_IX--;
                            break;
                        case 0xFD:
                            m_reg_IY--;
                            break;
                        default:
                            HL--;
                            break;
                    }
                    break;
                case 3: m_reg_SP--; break;
            }

            m_Flag_N = true;

#if CPU_TRACE
            int offset = 48;
            if (m_OpCodePrefix == 0xDD) offset += 4;
            if (m_OpCodePrefix == 0xFD) offset += 8;
            m_ei.Op2 = m_regNames[offset + target];
#endif
        }
        private void _DI()
        {
            m_IFF1 = m_IFF2 = false;
        }
        private void _EI()
        {
            m_IFF1 = m_IFF2 = true;
        }
        private void _EX_AF_AFAlt()
        {
            m_wordValue = AF;
            AF = AF_Alt;
            AF = m_wordValue;
#if CPU_TRACE
            m_ei.Op1 = "AF";
            m_ei.Op2 = "AF'";
#endif
        }
        private void _EX_DE_HL()
        {
            m_wordValue = HL;
            HL = DE;
            DE = m_wordValue;
#if CPU_TRACE
            m_ei.Op1 = "DE";
            m_ei.Op2 = "HL";
#endif
        }
        private void _EX_indSP_HLIXIY()
        {
            switch(m_OpCodePrefix)
            {
                case 0x00:
                    m_wordValue = readMemWord(m_reg_SP);
                    writeMemWord(m_reg_SP, HL);
                    HL = m_wordValue;
#if CPU_TRACE
                    m_ei.Op1 = "(SP)";
                    m_ei.Op2 = "HL";
#endif 
                    break;
                case 0xDD:
                    m_wordValue = readMemWord(m_reg_SP);
                    writeMemWord(m_reg_SP, m_reg_IX);
                    m_reg_IX = m_wordValue;
#if CPU_TRACE
                    m_ei.Op1 = "(SP)";
                    m_ei.Op2 = "IX";
#endif
                    break;
                case 0xFD:
                    m_wordValue = readMemWord(m_reg_SP);
                    writeMemWord(m_reg_SP, m_reg_IY);
                    m_reg_IY = m_wordValue;
#if CPU_TRACE
                    m_ei.Op1 = "(SP)";
                    m_ei.Op2 = "IY";
#endif
                    break;
            }
        }
        private void _EXX()
        {
            m_wordValue = BC;
            BC = BC_Alt;
            BC_Alt = m_wordValue;

            m_wordValue = DE;
            DE = DE_Alt;
            DE_Alt = m_wordValue;

            m_wordValue = HL;
            HL = HL_Alt;
            HL_Alt = m_wordValue;
        }
        private void _HALT()
        {
            // TODO Z80 - wait for interrupt
            throw new ApplicationException("HALT");
        }
        private void _IM()
        {
            switch(m_OpCodeIndex)
            {
                case 0x46: m_IM = 0; break;
                case 0x56: m_IM = 1; break;
                case 0x5E: m_IM = 2; break;
                default:
                    throw new ApplicationException(String.Format("invalid IM OpCode {0:X2}",m_OpCodeIndex));
            }

#if CPU_TRACE
            m_ei.Op1 = m_IM.ToString();
#endif
        }
        private void _IN()
        {
            if (m_INHandler.ContainsKey(m_byteValue))
                m_reg_A = m_INHandler[m_byteValue]();
            else
                throw new ApplicationException(String.Format("Z80: IN port {0:X2} not handled in instruction {1:X4}!", m_byteValue, m_PC_Start));
#if CPU_TRACE
            m_ei.Op1 = "A";
#endif
        }
        private void _INC()
        {
            Func<byte,byte> doINC = b =>
            {
                Flags = (byte)((m_precalc_INCf[b]) | (byte)(m_Flag_C ? ZFLAGS.C : 0));
                b++;
                return b;
            };

            genericRegisterIndexOperation(doINC,3);
        }
        private void _INC_ss()
        {
            int target = (m_OpCodeIndex >> 4) & 3;
            switch (target)
            {
                case 0: BC++; break;
                case 1: DE++; break;
                case 2: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            m_reg_IX++;
                            break;
                        case 0xFD:
                            m_reg_IY++;
                            break;
                        default:
                            HL++;
                            break;
                    }
                    break;
                case 3: m_reg_SP++; break;
            }

            m_Flag_N = false;

#if CPU_TRACE
            int offset = 48;
            if (m_OpCodePrefix == 0xDD) offset += 4;
            if (m_OpCodePrefix == 0xFD) offset += 8;
            m_ei.Op2 = m_regNames[offset + target];
#endif
        }
        private void _JP()
        {
            m_PC = m_wordValue;
        }

        private void _JP_cc_nn()
        {
            int condition = (m_OpCodeIndex >> 3) & 7;

            bool IsTrue = false;

            switch (condition)
            {
                case 0:
                    IsTrue = !m_Flag_Z;
                    break;
                case 1:
                    IsTrue = m_Flag_Z;
                    break;
                case 2:
                    IsTrue = !m_Flag_C;
                    break;
                case 3:
                    IsTrue = m_Flag_C;
                    break;
                case 4:
                    IsTrue = !m_Flag_PV;
                    break;
                case 5:
                    IsTrue = m_Flag_PV;
                    break;
                case 6:
                    IsTrue = !m_Flag_S;
                    break;
                case 7:
                    IsTrue = m_Flag_S;
                    break;
            }

            if (IsTrue) m_PC = m_wordValue;

#if CPU_TRACE
            string[] conditions = new string[] { "NZ", "Z", "NC", "C", "PO", "PE", "P", "M" };
            m_ei.Op1 = conditions[condition];
#endif
        }
        private void _JP_HLIXIY()
        {
            switch(m_OpCodePrefix)
            {
                case 0xDD:
                    m_PC = m_reg_IX;
#if CPU_TRACE
                    m_ei.Op1 = "IX";
#endif
                    break;
                case 0xFD:
                    m_PC = m_reg_IY;
#if CPU_TRACE
                    m_ei.Op1 = "IY";
#endif
                    break;
                default:
                    m_PC = HL;
#if CPU_TRACE
                    m_ei.Op1 = "HL";
#endif
                    break;
            }
        }
        private void _JR()
        {
            int condition = (m_OpCodeIndex >> 3) & 7;

            bool IsTrue = false;

            switch (condition)
            {
                case 4:
                    IsTrue = !m_Flag_Z;
                    break;
                case 5:
                    IsTrue = m_Flag_Z;
                    break;
                case 6:
                    IsTrue = !m_Flag_C;
                    break;
                case 7:
                    IsTrue = m_Flag_C;
                    break;
            }

            if (IsTrue && !m_ExtraCycles) // go back to cycling
            {
                m_MaxCycles += 5;
                m_ExtraCycles = true;
                return;
            }

            ushort jump;
            if (m_byteValue >= 0x80)
            {
                jump = (ushort)(0x100 - m_byteValue);
                if(IsTrue) m_PC -= jump;
#if CPU_TRACE
                m_ei.Op2 = String.Format("-{0:X2}", jump);
#endif
            }
            else
            {
                jump = (ushort)(m_byteValue & 0x7F);
                if (IsTrue) m_PC += jump;
#if CPU_TRACE
                m_ei.Op2 = String.Format("{0:X2}", jump);
#endif
            }

#if CPU_TRACE
            string[] conditions = new string[] { "NZ", "Z", "NC", "C" };
            m_ei.Op1 = conditions[condition - 4];
#endif

        }

        private void _JR_e()
        {
            ushort jump;
            if (m_byteValue >= 0x80)
            {
                jump = (ushort)(0x100 - m_byteValue);
                m_PC -= jump;
#if CPU_TRACE
                m_ei.Op2 = String.Format("-{0:X2}", jump);
#endif
            }
            else
            {
                jump = (ushort)(m_byteValue & 0x7F);
                m_PC += jump;
#if CPU_TRACE
                m_ei.Op2 = String.Format("{0:X2}", jump);
#endif
            }
        }


        private void _LD_A_I()
        {
            m_reg_A = m_reg_I;
            setUnusedFlags(m_reg_A);
            m_Flag_S = (m_reg_A & b10000000) != 0;
            m_Flag_Z = m_reg_A == 0;
            m_Flag_H = m_Flag_N = false;
            m_Flag_PV = m_IFF2;
        }
        private void _LD_A_Helper()
        {
#if CPU_TRACE
            m_ei.Op1 = "A";
#endif
            m_reg_A = m_byteValue;
        }
        private void _LD_A_R()
        {
            m_reg_A = m_reg_R;
            setUnusedFlags(m_reg_A);
            m_Flag_S = (m_reg_A & b10000000) != 0;
            m_Flag_Z = m_reg_A == 0;
            m_Flag_H = m_Flag_N = false;
            m_Flag_PV = m_IFF2;
        }
        private void _LD_dd_nn()
        {
            int dd = (m_OpCodeIndex & 0x30) >> 4;
            switch (dd)
            {
                case 0: BC = m_wordValue; break;
                case 1: DE = m_wordValue; break;
                case 2: HL = m_wordValue; break;
                case 3: m_reg_SP = m_wordValue; break;
            }
#if CPU_TRACE
            m_ei.Op1 = m_regNames[48 + dd];
#endif
        }
        private void _LD_I_A()
        {
            m_reg_I = m_reg_A;
        }
        private void _LD_IX_nn()
        {
            m_reg_IX = m_wordValue;
#if CPU_TRACE
            m_ei.Op1 = "IX";
#endif
        }
        private void _LD_IY_nn()
        {
            m_reg_IY = m_wordValue;
#if CPU_TRACE
            m_ei.Op1 = "IX";
#endif
        }
        private void _LD_indBC_A()
        {
#if CPU_TRACE
            m_ei.Op1 = "(BC)";
            m_ei.Op2 = "A";
#endif
            writeMemByte(BC, m_reg_A);
        }
        private void _LD_indDE_A()
        {
#if CPU_TRACE
            m_ei.Op1 = "(DE)";
            m_ei.Op2 = "A";
#endif
            writeMemByte(DE, m_reg_A);
        }
        private void _LD_indnn()
        {
            int source = (m_OpCodePrefix << 8) | m_OpCodeIndex;

            UInt16 w;

            switch (source)
            {
                case 0x0022: w = HL; break;
                case 0xDD22: w = m_reg_IX; break;
                case 0xFD22: w = m_reg_IY; break;
                case 0xED43: w = BC; break;
                case 0xED53: w = DE; break;
                case 0xED63: w = HL; break;
                case 0xED73: w = m_reg_SP; break;
                default:
                    string sMessage = String.Format("Invalid source combination {0:X2} {1:X2}", m_OpCodePrefix, m_OpCodeIndex);
                    throw new ApplicationException(sMessage);
            }

            writeMemWord(m_wordValue, w);

#if CPU_TRACE
            m_ei.Op1 = "(" + m_ei.Op2 + ")";
            switch (source)
            {
                case 0x0022: m_ei.Op2 = "HL"; break;
                case 0xDD22: m_ei.Op2 = "IX"; break;
                case 0xFD22: m_ei.Op2 = "IY"; break;
                case 0xED43: m_ei.Op2 = "BC"; break;
                case 0xED53: m_ei.Op2 = "DE"; break;
                case 0xED63: m_ei.Op2 = "HL"; break;
                case 0xED73: m_ei.Op2 = "SP"; break;
                default:
                    string sMessage = String.Format("Invalid source combination {0:X2} {1:X2}", m_OpCodePrefix, m_OpCodeIndex);
                    throw new ApplicationException(sMessage);
            }
#endif
        }
        private void _LD_indnn_A()
        {
            m_wordValue = getNextMemWord();
#if CPU_TRACE
            m_ei.Bytes += String.Format("{0:X2} {1:X2}", (byte)(m_wordValue & 0xFF), (m_wordValue >> 8));
            m_ei.Op1 = String.Format("({0:X4})", m_wordValue);
            m_ei.Op2 = "A";
#endif
            writeMemByte(m_wordValue, m_reg_A);
        }
        private void _LD_HLIXIY_indnn()
        {
            switch(m_OpCodePrefix)
            {
                case 0x00: HL = m_wordValue; break;
                case 0xDD: m_reg_IX = m_wordValue; break;
                case 0xFD: m_reg_IY = m_wordValue; break;
            }

#if CPU_TRACE
            switch (m_OpCodePrefix)
            {
                case 0x00: m_ei.Op1 = "HL"; break;
                case 0xDD: m_ei.Op1 = "IX"; break;
                case 0xFD: m_ei.Op1 = "IY"; break;
            }
#endif
        }
        private void _LD_R_A()
        {
            m_reg_R = m_reg_A;
        }
        private void _LD_Reg_and_Ind_Helper()
        {
            int target = (m_OpCodeIndex >> 3) & 7;
            switch (target)
            {
                case 0: m_reg_B = m_byteValue; break;
                case 1: m_reg_C = m_byteValue; break;
                case 2: m_reg_D = m_byteValue; break;
                case 3: m_reg_E = m_byteValue; break;
                case 4: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            IX_H = m_byteValue;
                            break;
                        case 0xFD:
                            IY_H = m_byteValue;
                            break;
                        default:
                            m_reg_H = m_byteValue;
                            break;
                    }
                    break;
                case 5: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            IX_L = m_byteValue;
                            break;
                        case 0xFD:
                            IY_L = m_byteValue;
                            break;
                        default:
                            m_reg_L = m_byteValue;
                            break;
                    }
                    break;
                case 6: switch (m_OpCodePrefix)
                    {
                        case 0xDD:
                            UInt16 address = m_reg_IX;
                            address += m_byteDisplacement;
                            writeMemByte(address, m_byteValue);
                            break;
                        case 0xFD:
                            address = m_reg_IY;
                            address += m_byteDisplacement;
                            writeMemByte(address, m_byteValue);
                            break;
                        default:
                            writeMemByte(HL, m_byteValue);
                            break;
                    }
                    break;
                case 7: m_reg_A = m_byteValue; break;
            }
#if CPU_TRACE
            int offset = 0;
            if (m_OpCodePrefix == 0xDD) offset = 8;
            if (m_OpCodePrefix == 0xFD) offset = 16;
            m_ei.Op1 = m_regNames[offset + target];
#endif
        }
        private void _LD_SP_HL()
        {
            switch (m_OpCodePrefix)
            {
                case 0x00: m_reg_SP = HL; break;
                case 0xDD: m_reg_SP = m_reg_IX; break;
                case 0xFD: m_reg_SP = m_reg_IY; break;
            }

#if CPU_TRACE
            m_ei.Op1 = "SP";
            switch (m_OpCodePrefix)
            {
                case 0x00: m_ei.Op2 = "HL"; break;
                case 0xDD: m_ei.Op2 = "IX"; break;
                case 0xFD: m_ei.Op2 = "IY"; break;
            }
#endif
        }
        private void _LDI()
        {
            m_byteValue = readMemByte(HL);
            writeMemByte(DE,m_byteValue);
            setUnusedFlags(m_byteValue);
            m_Flag_H = m_Flag_N = false;

            DE++;
            HL++;
            BC--;

            m_Flag_PV = BC != 0;
        }
        private void _LDIR()
        {
            _LDI();
            if(m_Flag_PV)
            {
                if(m_ExtraCycles) // already in extra cycles
                {
                    m_MaxCycles += 21;
                }
                else // first extra cycle - only add uplift from LDI to LDIR
                {
                    m_MaxCycles += 5;
                    m_ExtraCycles = true;
                }
            }
        }
        private void _LDD()
        {
            m_byteValue = readMemByte(HL);
            writeMemByte(DE, m_byteValue);
            setUnusedFlags(m_byteValue);
            m_Flag_H = m_Flag_N = false;

            DE--;
            HL--;
            BC--;

            m_Flag_PV = BC != 0;
        }
        private void _LDDR()
        {
            _LDD();
            if (m_Flag_PV)
            {
                if (m_ExtraCycles) // already in extra cycles
                {
                    m_MaxCycles += 21;
                }
                else // first extra cycle - only add uplift from LDI to LDIR
                {
                    m_MaxCycles += 5;
                    m_ExtraCycles = true;
                }
            }
        }
        private void _NOP() { }
        private void _OR()
        {
            m_reg_A |= m_byteValue;
            Flags = m_precalcLog_f[m_reg_A];
        }
        private void _OUT()
        {
            if (m_OUTHandler.ContainsKey(m_byteValue))
                m_OUTHandler[m_byteValue](m_reg_A);
            else
                throw new ApplicationException(String.Format("Z80: OUT port {0:X2} not handled in instruction {1:X4}!",m_byteValue,m_PC_Start));
#if CPU_TRACE
            m_ei.Op1 = m_ei.Op2;
            m_ei.Op2 = "A";
#endif
        }
        private void _POP()
        {
            int target = (m_OpCodeIndex >> 4) & 3;
            switch (target)
            {
                case 0:
                    BC = readMemWord(m_reg_SP);
#if CPU_TRACE
                    m_ei.Op1 = "BC";
#endif
                    break;

                case 1:
                    DE = readMemWord(m_reg_SP);
#if CPU_TRACE
                    m_ei.Op1 = "DE";
#endif
                    break;

                case 2: switch(m_OpCodePrefix)
                    {
                        case 0:
                            HL = readMemWord(m_reg_SP);
#if CPU_TRACE
                            m_ei.Op1 = "HL";
#endif
                            break;
                        case 0xDD:
                            m_reg_IX = readMemWord(m_reg_SP);
#if CPU_TRACE
                            m_ei.Op1 = "IY";
#endif
                            break;
                        case 0xFD:
                            m_reg_IY = readMemWord(m_reg_SP);
#if CPU_TRACE
                            m_ei.Op1 = "IY";
#endif
                            break;                        
                    }
                    break;

                case 3:
                    AF = readMemWord(m_reg_SP);
#if CPU_TRACE
                    m_ei.Op1 = "AF";
#endif
                    break;
            }

            m_reg_SP += 2;
        }
        private void _PUSH()
        {
            int target = (m_OpCodeIndex >> 4) & 3;

            m_reg_SP -= 2;

            switch (target)
            {
                case 0:
                    writeMemWord(m_reg_SP,BC);
#if CPU_TRACE
                    m_ei.Op1 = "BC";
#endif
                    break;

                case 1:
                    writeMemWord(m_reg_SP,DE);
#if CPU_TRACE
                    m_ei.Op1 = "DE";
#endif
                    break;

                case 2: switch (m_OpCodePrefix)
                    {
                        case 0:
                            writeMemWord(m_reg_SP,HL);
#if CPU_TRACE
                            m_ei.Op1 = "HL";
#endif
                            break;
                        case 0xDD:
                            writeMemWord(m_reg_SP, m_reg_IX);
#if CPU_TRACE
                            m_ei.Op1 = "IY";
#endif
                            break;
                        case 0xFD:
                            writeMemWord(m_reg_SP, m_reg_IY);
#if CPU_TRACE
                            m_ei.Op1 = "IY";
#endif
                            break;
                    }
                    break;

                case 3:
                    writeMemWord(m_reg_SP, AF);
#if CPU_TRACE
                    m_ei.Op1 = "AF";
#endif
                    break;
            }
        }
        private void _RET()
        {
            m_wordValue = readMemWord(m_reg_SP);
            m_reg_SP += 2;
            m_PC = m_wordValue;
        }
        private void _RETI()
        {
            m_IFF1 = m_IFF2;
            m_wordValue = readMemWord(m_reg_SP);
            m_reg_SP += 2;
            m_PC = m_wordValue;
        }
        private void _RET_cc()
        {
            int condition = (m_OpCodeIndex >> 3) & 7;

            bool IsTrue = false;

            switch (condition)
            {
                case 0:
                    IsTrue = !m_Flag_Z;
                    break;
                case 1:
                    IsTrue = m_Flag_Z;
                    break;
                case 2:
                    IsTrue = !m_Flag_C;
                    break;
                case 3:
                    IsTrue = m_Flag_C;
                    break;
                case 4:
                    IsTrue = !m_Flag_PV;
                    break;
                case 5:
                    IsTrue = m_Flag_PV;
                    break;
                case 6:
                    IsTrue = !m_Flag_S;
                    break;
                case 7:
                    IsTrue = m_Flag_S;
                    break;
            }

            if(IsTrue)
                if(m_ExtraCycles)
                {
                    _RET();
                }
                else
                {
                    m_ExtraCycles = true;
                    m_MaxCycles += 6;
                }

#if CPU_TRACE
            string[] conditions = new string[] { "NZ", "Z", "NC", "C", "PO", "PE", "P", "M" };
            m_ei.Op1 = conditions[condition];
#endif

        }
        private void _RLA()
        {
            if (m_Flag_C)
            {
                Flags = m_precalc_RL1[m_reg_A];
                m_reg_A <<= 1;
                m_reg_A++;
            }
            else
            {
                Flags = m_precalc_RL0[m_reg_A];
                m_reg_A <<= 1;
            }
        }

        private void _RLC_r()
        {
            Func<byte, byte> doRLC = b =>
            {
                m_Flag_C = (b & 0x80) == 0x80;
                b <<= 1;
                if (m_Flag_C) b |= 1;

                m_Flag_S = (b & 0x80) == 0x80;
                m_Flag_Z = b == 0;
                m_Flag_H = m_Flag_N = false;
                setUnusedFlags(m_reg_A);

                checkParity(b);

                return b;
            };

            genericRegisterIndexOperation(doRLC);
        }
        private void _RLCA()
        {
            Flags = m_precalc_RLCf[m_reg_A];

            int result = m_reg_A << 1;
            if ((result & 0x100) != 0) result = (result | 0x01) & 0xFF;

            m_reg_A = (byte)result;
        }
        private void _RRCA()
        {
            Flags = m_precalc_RRCf[m_reg_A];

            if ((m_reg_A & 0x01) != 0)
                m_reg_A = (byte)((m_reg_A >> 1) | 0x80);
            else
                m_reg_A >>= 1;
        }
        private void _RRA()
        {
            if (m_Flag_C)
            {
                Flags = m_precalc_RR1[m_reg_A];
                m_reg_A >>= 1;
                m_reg_A += 0x80;
            }
            else
            {
                Flags = m_precalc_RR0[m_reg_A];
                m_reg_A >>= 1;
            }
        }
        private void _RST()
        {
            int p = (m_OpCodeIndex >> 3) & 0x07;

            UInt16 nextPCAddress = (UInt16)(((p >> 1) & 0x03) << 4);
            nextPCAddress |= (p & 0x01) == 0 ? (UInt16)0 : (UInt16)0x08;

            m_reg_SP -= 2; ;
            writeMemWord(m_reg_SP, m_PC);

            m_PC = nextPCAddress;

#if CPU_TRACE
            m_ei.Op1 = String.Format("${0:X2}", nextPCAddress);
#endif
        }
        private void _SBC_A()
        {
            int carry = m_Flag_C ? 1 : 0;
            Flags = m_precalc_SBCf[m_reg_A * 0x100 + m_byteValue + carry * 0x10000];
            m_reg_A -= m_byteValue;
            m_reg_A -= (byte)carry;

#if CPU_TRACE
            m_ei.Op1 = "A";
#endif
        }
        private void _SBC_HL_ss()
        {
            int regs = (m_OpCodeIndex >> 4) & 3;

            switch (regs)
            {
                case 0: m_wordValue = BC; break;
                case 1: m_wordValue = DE; break;
                case 2: m_wordValue = HL; break;
                case 3: m_wordValue = m_reg_SP; break;
            }

            int carry = m_Flag_C ? 1 : 0;
            uint result = (uint)((HL & 0xFFFF) - (m_wordValue & 0xFFFF) - carry);
            int result_signed = (Int16)HL - (Int16)m_wordValue - carry;

            m_Flag_N = true;
            m_Flag_C = (result & 0x10000) != 0;
            m_Flag_Z = (result & 0xFFFF) == 0;
            m_Flag_S = (result & 0x8000) != 0;
            m_Flag_H = (((HL & 0x0FFF) - (m_wordValue & 0x0FFF) - carry) & 0x1000) != 0;
            m_Flag_PV = (result_signed < -0x8000 || result_signed >= 0x8000);
            setUnusedFlags((byte)(result >> 8));

            HL = (ushort)result;
        }
        private void _SCF()
        {
            setUnusedFlags(m_reg_A);
            m_Flag_H = false; 
            m_Flag_C = true;
            m_Flag_N = false;
        }
        private void _SLA()
        {
            Flags = m_precalc_RL0[m_reg_A];
            m_reg_A <<= 1;
        }
        private void _SRA()
        {
            Flags = m_precalc_SRAf[m_reg_A];
            m_reg_A >>= 1;
        }
        private void _SUB_A()
        {
            Flags = m_precalc_SBCf[m_reg_A * 0x100 + m_byteValue];
            m_reg_A -= m_byteValue;


#if CPU_TRACE
            m_ei.Op1 = "A";
#endif
        }
        private void _XOR()
        {
            m_reg_A ^= m_byteValue;
            Flags = m_precalcLog_f[m_reg_A];
        }
        #endregion
    }

}