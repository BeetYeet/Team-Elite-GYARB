using System.CodeDom;
using System.IO;

namespace Team_Elite
{
    public interface ISaveable
    {
        void CreateFromBinaryStream(BinaryReader reader);
        void WriteToBinaryStream(BinaryWriter writer);
    }
}