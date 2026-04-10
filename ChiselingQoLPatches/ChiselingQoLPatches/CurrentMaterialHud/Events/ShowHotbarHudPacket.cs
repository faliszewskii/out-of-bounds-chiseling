using ProtoBuf;

#nullable disable
namespace ChiselingQoLPatches.CurrentMaterialHud.Events;

[ProtoContract]
public class ShowHotbarHudPacket
{
    [ProtoMember(1)] public int Slot;
    [ProtoMember(2)] public int MaterialId;
}