using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEMU.Systems
{

    internal struct AppleKeyMap
    {
        public byte key_out;
        public byte key_out_ctrl;
        public byte key_out_shift;

        public AppleKeyMap(byte plain, byte control, byte shift)
        {
            key_out = plain;
            key_out_ctrl = control;
            key_out_shift = shift;
        }
    }

    class Apple1KeyMapFactory
    {
        public static Dictionary<byte, AppleKeyMap> Build(string sKeyboard)
        {
            Dictionary<byte, AppleKeyMap> keymap = new Dictionary<byte, AppleKeyMap>();

            keymap.Add(0x08, new AppleKeyMap(0x5f, 0x5f, 0x5f)); // BS

            switch (sKeyboard.ToUpper())
            {
                case "DE":
                    keymap.Add(0x30, new AppleKeyMap(0x30, 0x30, 0x3D)); // 0 - =
                    keymap.Add(0x31, new AppleKeyMap(0x31, 0x31, 0x21)); // 1 - !
                    keymap.Add(0x32, new AppleKeyMap(0x32, 0x00, 0x22)); // 2 - "
                    keymap.Add(0x33, new AppleKeyMap(0x33, 0x33, 0xA7)); // 3 - §
                    keymap.Add(0x34, new AppleKeyMap(0x34, 0x34, 0x24)); // 4 - $
                    keymap.Add(0x35, new AppleKeyMap(0x35, 0x35, 0x25)); // 5 - %
                    keymap.Add(0x36, new AppleKeyMap(0x36, 0x36, 0x26)); // 6 - &
                    keymap.Add(0x37, new AppleKeyMap(0x37, 0x37, 0x2F)); // 7 - /
                    keymap.Add(0x38, new AppleKeyMap(0x38, 0x38, 0x28)); // 8 - (
                    keymap.Add(0x39, new AppleKeyMap(0x39, 0x39, 0x29)); // 9 - )
                    keymap.Add(0xBB, new AppleKeyMap(0x2B, 0x00, 0x2A)); // + - *
                    keymap.Add(0xBC, new AppleKeyMap(0x2C, 0x00, 0x3B)); // , - ;
                    keymap.Add(0xBD, new AppleKeyMap(0x2D, 0x00, 0x5F)); // - - _
                    keymap.Add(0xBE, new AppleKeyMap(0x2E, 0x00, 0x3A)); // . - :
                    break;

                default:
                    keymap.Add(0x30, new AppleKeyMap(0x30, 0x30, 0x29)); // 0 - )
                    keymap.Add(0x31, new AppleKeyMap(0x31, 0x31, 0x21)); // 1 - !
                    keymap.Add(0x32, new AppleKeyMap(0x32, 0x00, 0x40)); // 2 - @
                    keymap.Add(0x33, new AppleKeyMap(0x33, 0x33, 0x23)); // 3 - #
                    keymap.Add(0x34, new AppleKeyMap(0x34, 0x34, 0x24)); // 4 - $
                    keymap.Add(0x35, new AppleKeyMap(0x35, 0x35, 0x25)); // 5 - %
                    keymap.Add(0x36, new AppleKeyMap(0x36, 0x36, 0x5E)); // 6 - ^
                    keymap.Add(0x37, new AppleKeyMap(0x37, 0x37, 0x26)); // 7 - &
                    keymap.Add(0x38, new AppleKeyMap(0x38, 0x38, 0x2A)); // 8 - *
                    keymap.Add(0x39, new AppleKeyMap(0x39, 0x39, 0x28)); // 9 - (
                    break;
            }

            keymap.Add(0x40, new AppleKeyMap(0x40, 0x00, 0x40)); // @
            keymap.Add(0x41, new AppleKeyMap(0x41, 0x01, 0x41)); // A
            keymap.Add(0x42, new AppleKeyMap(0x42, 0x02, 0x42)); // B
            keymap.Add(0x43, new AppleKeyMap(0x43, 0x03, 0x43)); // C - BRK
            keymap.Add(0x44, new AppleKeyMap(0x44, 0x04, 0x44)); // D
            keymap.Add(0x45, new AppleKeyMap(0x45, 0x05, 0x45)); // E
            keymap.Add(0x46, new AppleKeyMap(0x46, 0x06, 0x46)); // F
            keymap.Add(0x47, new AppleKeyMap(0x47, 0x07, 0x47)); // G - BELL
            keymap.Add(0x48, new AppleKeyMap(0x48, 0x08, 0x48)); // H
            keymap.Add(0x49, new AppleKeyMap(0x49, 0x09, 0x49)); // I - TAB
            keymap.Add(0x4A, new AppleKeyMap(0x4A, 0x0A, 0x4A)); // J - NL
            keymap.Add(0x4B, new AppleKeyMap(0x4B, 0x0B, 0x4B)); // K - VT 
            keymap.Add(0x4C, new AppleKeyMap(0x4C, 0x0C, 0x4C)); // L
            keymap.Add(0x4D, new AppleKeyMap(0x4D, 0x0D, 0x4D)); // M - CR
            keymap.Add(0x4E, new AppleKeyMap(0x4E, 0x0E, 0x4E)); // N
            keymap.Add(0x4F, new AppleKeyMap(0x4F, 0x0F, 0x4F)); // O
            keymap.Add(0x50, new AppleKeyMap(0x50, 0x10, 0x50)); // P
            keymap.Add(0x51, new AppleKeyMap(0x51, 0x11, 0x51)); // Q
            keymap.Add(0x52, new AppleKeyMap(0x52, 0x12, 0x52)); // R
            keymap.Add(0x53, new AppleKeyMap(0x53, 0x13, 0x53)); // S
            keymap.Add(0x54, new AppleKeyMap(0x54, 0x14, 0x54)); // T
            keymap.Add(0x55, new AppleKeyMap(0x55, 0x15, 0x55)); // U
            keymap.Add(0x56, new AppleKeyMap(0x56, 0x16, 0x56)); // V
            keymap.Add(0x57, new AppleKeyMap(0x57, 0x17, 0x57)); // W
            keymap.Add(0x58, new AppleKeyMap(0x58, 0x18, 0x58)); // X
            keymap.Add(0x59, new AppleKeyMap(0x59, 0x19, 0x59)); // Y
            keymap.Add(0x5A, new AppleKeyMap(0x5A, 0x1A, 0x5A)); // Z

            return keymap;
        }
    }
}
