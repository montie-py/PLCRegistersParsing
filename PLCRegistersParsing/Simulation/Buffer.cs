public class HoldingRegisters
{
    public short[] localArray = new short[(int) ushort.MaxValue];

    public short this[int x]
    {
        get => this.localArray[x];
        set => this.localArray[x] = value;
    }
}