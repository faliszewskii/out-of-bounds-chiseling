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
    public class OutOfBoundsChiselingConfig
    {
        [DefaultValue(true)]
        public bool ConsumeBlockOnOutOfBounds = true;

        //[JsonIgnore]
        //internal DropMaterialsMode DropMaterialsEnum;

        //[DefaultValue("All")]
        //public string DropMaterials
        //{
        //    get => DropMaterialsEnum.ToString();
        //    set
        //    {
        //        if (!Enum.TryParse<DropMaterialsMode>(value, true, out var result))
        //        {
        //            throw new Exception($"Wrong config value for DropMaterials");
        //        }
        //        DropMaterialsEnum = result;
        //    }
        //}
    }
}
