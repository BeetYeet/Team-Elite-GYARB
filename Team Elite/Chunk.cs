using Extreme.Mathematics;
namespace Team_Elite
{
    public struct Chunk
    {
        public readonly BigInteger start;
        public readonly BigInteger end;

        public Chunk(BigInteger start, BigInteger end)
        {
            this.start = start;
            this.end = end;
        }
    }
}