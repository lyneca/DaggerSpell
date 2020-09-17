using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerIceSpellMerge : AbstractMergeSpell {
        public override void OnCatalogRefresh() {
            spellCastName = "IceSpell";
            base.OnCatalogRefresh();
        }
    }
}
