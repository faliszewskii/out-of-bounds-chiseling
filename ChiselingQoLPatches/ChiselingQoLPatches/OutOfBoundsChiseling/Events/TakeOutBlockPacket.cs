using ProtoBuf;
using Vintagestory.API.MathTools;

#nullable disable
namespace ChiselingQoLPatches.OutOfBoundsChiseling.Events;

[ProtoContract]
public class TakeOutBlockPacket
{    
    [ProtoMember(1)] public int blockId;
    [ProtoMember(2)] public int quantity;
}