using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.MathTools;

#nullable disable
namespace ChiselingQoLPatches.OutOfBoundsChiseling.Events;

[ProtoContract]
public class PlaceBEChiselPacket
{
    [ProtoMember(1)] public int blockId;
    [ProtoMember(2)] public BlockPos atPos;
}