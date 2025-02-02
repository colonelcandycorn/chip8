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
    
    /*
     * Fetch
     *
     * Read the current instruction at PC. Read two bytes at a time and advance PC by 2. Combine two bytes into
     * one UInt16.
     */
}