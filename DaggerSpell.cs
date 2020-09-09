using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    public class DaggerSpell : SpellCastCharge {

        private bool daggerActive;
        private ItemPhysic daggerBase;
        private Item summonedDagger;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            AssetBundle itemsBundle = AssetBundle.GetAllLoadedAssetBundles().Where(x => x.name.Contains("items")).First();
            // itemsBundle.LoadAsset<GameObject>("assets/private/bas-items/melee/daggers/wpn_da02_common.prefab");
            daggerBase = Catalog.GetData<ItemPhysic>("DaggerCommon", true);
        }

        public override void UpdateCaster() {
            base.UpdateCaster();
            if (daggerActive) {
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                summonedDagger.transform.position = Vector3.Lerp(summonedDagger.transform.position, GetTargetPosition(), Time.deltaTime * 10);
                switch (spellCaster.bodyHand.side) {
                    case Side.Left:
                        summonedDagger.transform.rotation = Quaternion.Lerp(summonedDagger.transform.rotation, spellCaster.bodyHand.transform.rotation * Quaternion.Euler(0, 0, 180), Time.deltaTime * 10);
                        break;
                    case Side.Right:
                        summonedDagger.transform.rotation = Quaternion.Lerp(summonedDagger.transform.rotation, spellCaster.bodyHand.transform.rotation, Time.deltaTime * 10);
                        break;
                }
                if (PlayerControl.GetHand(spellCaster.bodyHand.side).gripPressed && spellCaster.bodyHand.interactor.grabbedHandle == null && currentCharge == 1) {
                    EnableDagger();
                    switch (spellCaster.bodyHand.side) {
                        case Side.Left:
                            spellCaster.bodyHand.interactor.Grab(summonedDagger.mainHandleLeft);
                            break;
                        case Side.Right:
                            spellCaster.bodyHand.interactor.Grab(summonedDagger.mainHandleRight);
                            break;
                    }
                    daggerActive = false;
                    currentCharge = 0;
                    spellCaster.grabbedFire = false;
                    spellCaster.isFiring = false;
                    base.Fire(false);
                }
            }
        }

        public Vector3 GetTargetPosition() {
            return spellCaster.magicSource.transform.position + spellCaster.magicSource.transform.forward * -0.2f;
        }

        public void EnableDagger() {
            Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }

        public override void Fire(bool active) {
            base.Fire(active);
            if (active) {
                if (!daggerActive) {
                    daggerActive = true;
                    summonedDagger = daggerBase.Spawn(true, null);
                    Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
                    rb.isKinematic = true;
                    summonedDagger.transform.position = GetTargetPosition();
                }
            } else {
                if (currentCharge != 1) {
                    summonedDagger.Despawn();
                }
                daggerActive = false;
                EnableDagger();
            }
        }

        public override void Throw(Vector3 velocity) {
            base.Throw(velocity);
            EnableDagger();
            Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
            summonedDagger.transform.rotation = Quaternion.LookRotation(velocity) * Quaternion.LookRotation(Vector3.left);
            rb.AddForce(velocity * 5, ForceMode.Impulse);
            summonedDagger.Throw();
            daggerActive = false;
        }
    }
}
