using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerEarthBendingMerge : AbstractMergeSpell {
        public override void OnCatalogRefresh() {
            spellCastName = "EarthBending";
            base.OnCatalogRefresh();
        }
    }
}
