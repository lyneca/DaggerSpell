using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerCosmicMerge : AbstractMergeSpell {
        public override void OnCatalogRefresh() {
            spellCastName = "Cosmic";
            base.OnCatalogRefresh();
        }
    }
}
