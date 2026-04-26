using ChiselingQoLPatches.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChiselingQoLPatches.Config
{
    public class ChiselingQoLPatchesConfig
    {
        [DefaultValue(true)]
        public bool UseBlocksFromInventory = true;

        [DefaultValue(true)]
        public bool DropMaterialsOnLastVoxel = true;
    }
}
