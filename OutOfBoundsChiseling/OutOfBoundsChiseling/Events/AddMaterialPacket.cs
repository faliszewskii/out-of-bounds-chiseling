using ProtoBuf;
using Vintagestory.API.MathTools;

#nullable disable
namespace OutOfBoundsChiseling.Events;

[ProtoContract]
public class AddMaterialPacket
{
    [ProtoMember(1)] public BlockPos Pos;
    [ProtoMember(2)] public int BlockId;
}