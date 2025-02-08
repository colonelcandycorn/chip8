using Microsoft.Extensions.Logging;

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
    public byte[] Registers { get; init; }
    public uint InstructionsPerSecond { get; init; }
    private ILogger Ilogger { get; init; }
    
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
    
}