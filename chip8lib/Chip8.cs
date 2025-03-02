﻿using Microsoft.Extensions.Logging;

namespace chip8lib;

public class Chip8
{
    private const uint MEMORY_SIZE = 4 * 1024;
    private const uint DISPLAY_WIDTH = 64;
    private const uint DISPLAY_HEIGHT = 32;

    private static byte[] Font =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80 // F
    ];

    public byte[] Ram { get; init; } // Font should be 0x050 - 0x09F
    public bool[] Display { get; init; } // 60 FPS
    public UInt16 ProgramCounter { get; set; } // program should start at 0x200
    public UInt16 IndexRegister { get; set; }
    public Stack<UInt16> FunctionStack { get; init; }
    public byte DelayTimer { get; set; } // decrement 60 times a second does this need to be async?
    public byte SoundTimer { get; set; } // decrement 60 times a second beep continously well above 0
    public byte[] Registers { get; init; } // V0 through VF
    public uint InstructionsPerSecond { get; init; }
    private ILogger Ilogger { get; init; }
    private bool UseYInCheckedShift { get; init; }
    private bool UseVXForJumpOffset { get; init; }
    
    public HashSet<byte> PressedButtons { get; init; }
    
    private readonly record struct Instruction()
    {
        public UInt16 InstructionType { get; init; } // first nibble
        public UInt16 X { get; init; } // second nibble (one of the 16 registers)
        public UInt16 Y { get; init; } // third nibble  (one of the 16 registers)
        public UInt16 N { get; init; } // fourth nibble (4 bit number)
        public UInt16 Nn { get; init; } // second byte (8-bit immediate number)
        public UInt16 Nnn { get; init; } // 2nd, 3rd, and 4th nibble  (12 bit immediate memory-address)

        public Instruction(UInt16 instruct) : this()
        {
            UInt16 firstNibbleMask = 0b1111 << 12;
            UInt16 secondNibbleMask = 0b1111 << 8;
            UInt16 thirdNibbleMask = 0b1111 << 4;
            UInt16 fourthNibbleMask = 0b1111;
            UInt16 secondByteMask = 0b1111_1111;
            UInt16 twelveBitMask = 0b1111_1111_1111;

            InstructionType = (UInt16)(instruct & firstNibbleMask);
            X = (UInt16)(instruct & secondNibbleMask);
            Y = (UInt16)(instruct & thirdNibbleMask);
            N = (UInt16)(instruct & fourthNibbleMask);
            Nn = (UInt16)(instruct & secondByteMask);
            Nnn = (UInt16)(instruct & twelveBitMask);
        }
    }
    
    /*
     * Fetch
     *
     * Read the current instruction at PC. Read two bytes at a time and advance PC by 2. Combine two bytes into
     * one UInt16.
     */

    // TODO: test this
    private Instruction Fetch()
    {
        UInt16 lowerInstruction = Ram[ProgramCounter++];
        UInt16 higherInstruction = Ram[ProgramCounter++];

        UInt16 instruct = (UInt16)((higherInstruction << 8) | lowerInstruction);

        return new Instruction(instruct);
    }

    private void DecodeAndExecute(Instruction instruct)
    {
        
    }

    private void ClearScreen()
    {
        for (int row = 0; row < DISPLAY_HEIGHT; row++)
        {
            for (int col = 0; col < DISPLAY_WIDTH; col++)
            {
                long convertedIndex = col + (row * DISPLAY_WIDTH);

                if (convertedIndex >= Display.Length || convertedIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(convertedIndex),
                        "Index into Display is out of bounds");
                }
                
                Display[convertedIndex] = false;
            }
        }
    }

    private void Jump(UInt16 nnn)
    {
        ProgramCounter = nnn;
    }

    private void Subroutine(UInt16 nnn)
    {
        FunctionStack.Push(nnn);
        ProgramCounter = nnn;
    }

    private void SubroutineReturn()
    {
        try
        {
            ProgramCounter = FunctionStack.Pop();
        }
        catch (InvalidOperationException e)
        {
            Ilogger.Log(LogLevel.Critical, "When returning from subroutine encountered empty stack");
            throw;
        }
    }

    private void SkipConditionally(Instruction instruct)
    {
        switch (instruct.InstructionType)
        {
            case 3:
                if (Registers[instruct.X] == instruct.Nn)
                {
                    ProgramCounter += 2;
                }
                break;
            case 4:
                if (Registers[instruct.X] != instruct.Nn)
                {
                    ProgramCounter += 2;
                }
                break;
            case 5:
                if (Registers[instruct.X] == Registers[instruct.Y])
                {
                    ProgramCounter += 2;
                }
                break;
            case 9:
                if (Registers[instruct.X] != Registers[instruct.Y])
                {
                    ProgramCounter += 2;
                }
                break;
            default:
                Ilogger.Log(LogLevel.Critical, "Skip conditionally encountered an impossible instruction");
                throw new ArgumentException("Instruction mistakenly sent to skipConditionally");
        }
    }

    private void Set(Instruction instruct)
    {
        Registers[instruct.X] = (byte) instruct.Nn;
    }

    private void Add(Instruction instruct)
    {
        Registers[instruct.X] += (byte)instruct.Nn;
    }

    private void SetXToY(Instruction instruct)
    {
        Registers[instruct.X] = Registers[instruct.Y];
    }

    private void BinaryOrXAndY(Instruction instruct)
    {
        Registers[instruct.X] = (byte)(Registers[instruct.X] | Registers[instruct.Y]);
    }
    
    private void BinaryAndXAndY(Instruction instruct)
    {
        Registers[instruct.X] = (byte)(Registers[instruct.X] & Registers[instruct.Y]);
    }
    
    private void BinaryXOrXAndY(Instruction instruct)
    {
        Registers[instruct.X] = (byte)(Registers[instruct.X] ^ Registers[instruct.Y]);
    }

    private void CheckedAdd(Instruction instruct)
    {
        try
        {
            checked
            {
                Registers[instruct.X] += Registers[instruct.Y];
                Registers[0xF] = 0;
            }
        }
        catch (OverflowException)
        {
            Registers[0xF] = 1;
            unchecked
            {
                Registers[instruct.X] += Registers[instruct.Y];
            }
        }
    }

    private void CheckedSubtraction(Instruction instruct)
    {
        Registers[0xF] = 1;
        switch (instruct.N)
        {
            case 5:
                if (Registers[instruct.X] < Registers[instruct.Y])
                {
                    Registers[0xF] = 0;
                }

                Registers[instruct.X] -= Registers[instruct.Y];
                break;
            case 7:
                if (Registers[instruct.Y] < Registers[instruct.X])
                {
                    Registers[0xF] = 0;
                }

                Registers[instruct.X] = (byte) (Registers[instruct.Y] - Registers[instruct.X]);
                break;
            default:
                Ilogger.Log(LogLevel.Critical, "Illegal argument to CheckedSubtraction");
                throw new ArgumentException("Received an impossible exception in Checked Subtraction");
        }
    }

    private void ConfigurableCheckedShift(Instruction instruct)
    {
        if (UseYInCheckedShift)
        {
            Registers[instruct.X] = Registers[instruct.Y];
        }

        byte mask = 0b1000_0000;

        Registers[0xF] = (byte)((Registers[instruct.X] & mask) >> 7);

        Registers[instruct.X] = instruct.N switch
        {
            (6) => (byte)(Registers[instruct.X] >> 1),
            (0xE) => (byte)(Registers[instruct.X] << 1),
            _ => throw new ArgumentException("Received an impossible instruction in ConfigurableCheckedShift")
        };
    }

    private void SetIndex(Instruction instruct)
    {
        IndexRegister = instruct.Nnn;
    }

    private void JumpWithOffset(Instruction instruct)
    {
        if (UseVXForJumpOffset)
        {
            ProgramCounter = (UInt16) (instruct.Nnn + Registers[instruct.X]);
        }
        else
        {
            ProgramCounter = (UInt16)(instruct.Nnn + Registers[0]);
        }
    }

    private void RandomValue(Instruction instruct)
    {
        var rand = new Random();
        byte[] randByte = new byte[1];
        
        rand.NextBytes(randByte);

        Registers[instruct.X] = (byte)(randByte[0] & instruct.Nn);
    }

    private void DrawOnDisplay(Instruction instruct)
    {
        byte YCoord = (byte) (Registers[instruct.Y] & DISPLAY_HEIGHT);

        Registers[0xF] = 0;

        for (int i = 0; i < instruct.N; i++)
        {
            byte XCoord = (byte) (Registers[instruct.X] & DISPLAY_WIDTH);
            byte row = Ram[IndexRegister + i];
            byte mask = 0b1000_0000;
            for (int j = 0; j < 8; j++)
            {
                byte tmp = (byte)((row & mask) >> (7 - j));
                mask = (byte)(mask >> 1);

                if (tmp == 1)
                {
                    bool currentDisplayValue = Display[XCoord + (YCoord * DISPLAY_WIDTH)];

                    if (currentDisplayValue)
                    {
                        Registers[0xF] = 1;
                    }

                    Display[XCoord + (YCoord * DISPLAY_WIDTH)] = !currentDisplayValue;
                }

                XCoord += 1;

                if (XCoord > DISPLAY_WIDTH - 1)
                {
                    break;
                }
            }

            YCoord += 1;
            if (YCoord > DISPLAY_HEIGHT - 1)
            {
                break;
            }
        }
    }

    private void SkipIfKey(Instruction instruct)
    {
        bool isPressed = PressedButtons.Contains((byte)instruct.X);

        ProgramCounter += instruct.Nn switch
        {
            (0x9E) => (byte)(isPressed ? 2 : 0),
            (0xA1) => (byte)(isPressed ? 0 : 2),
            _ => 0
        };
    }

    private void VxToDelayTimer(Instruction instruct)
    {
        Registers[instruct.X] = DelayTimer;
    }

    private void DelayTimerToVx(Instruction instruct)
    {
        DelayTimer = Registers[instruct.X];
    }

    private void SoundTimerToVx(Instruction instruct)
    {
        SoundTimer = Registers[instruct.X];
    }
}