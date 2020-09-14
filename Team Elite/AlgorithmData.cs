using System.Collections.Generic;

namespace Team_Elite
{
    internal class AlgorithmData
    {
        public Chunk chunk;
        public List<BalancedNumber> output;

        public AlgorithmData(Chunk chunk, ref List<BalancedNumber> output)
        {
            this.chunk = chunk;
            this.output = output;
        }
    }
}