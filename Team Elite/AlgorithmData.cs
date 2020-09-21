using System.Collections.Generic;

namespace Team_Elite
{
    internal class AlgorithmData
    {
        public Chunk chunk;
        public List<BalancedNumber> output;
        public bool returnOnNew;

        public AlgorithmData(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            this.chunk = chunk;
            this.output = output;
            this.returnOnNew = returnOnNew;
        }
    }
}