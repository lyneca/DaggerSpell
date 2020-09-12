using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerGravityMerge : AbstractMergeSpell {
        public override void OnCatalogRefresh() {
            spellCastName = "Gravity";
            base.OnCatalogRefresh();
        }
    }
}
