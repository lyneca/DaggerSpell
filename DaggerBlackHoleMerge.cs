using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerBlackHoleMerge : AbstractMergeSpell {
        public override void OnCatalogRefresh() {
            spellCastName = "BlackHole";
            base.OnCatalogRefresh();
        }
    }
}
