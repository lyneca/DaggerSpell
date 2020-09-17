using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class AbstractMergeSpell : SpellMergeData {
        [global::Sirenix.OdinInspector.BoxGroup("Merge", true, false, 0)]
        [global::Sirenix.OdinInspector.ValueDropdown("GetAllItemID")]
        public string weaponId;
        private ItemPhysic daggerBase;
        private Item summonedDagger;
        private SpellCastCharge spellCharge;
        private bool daggerActive;
        public string spellCastName;

        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().Where(bundle => bundle.name.Contains("blackhole")).First();
            daggerBase = Catalog.GetData<ItemPhysic>(weaponId, true);
            spellCharge = Catalog.GetData<SpellCastCharge>(spellCastName);
        }

        public override void Merge(bool active) {
            base.Merge(active);
            if (active) {
                if (!daggerActive) {
                    if (Creature.player.mana.casterLeft.spellInstance is DaggerSpell leftSpell && leftSpell.summonedDagger != null) {
                        leftSpell.summonedDagger.gameObject.SetActive(false);
                    } else if (Creature.player.mana.casterRight.spellInstance is DaggerSpell rightSpell && rightSpell.summonedDagger != null) {
                        rightSpell.summonedDagger.gameObject.SetActive(false);
                    }
                    daggerActive = true;
                    summonedDagger = daggerBase.Spawn();
                    summonedDagger.transform.rotation = Quaternion.Slerp(
                        summonedDagger.transform.rotation,
                        Quaternion.LookRotation(GetClosestCreatureHead() - summonedDagger.transform.position),
                        Time.deltaTime * 10.0f
                    );
                    summonedDagger.transform.position = GetHandCenterPoint();
                    summonedDagger.GetComponent<Rigidbody>().isKinematic = true;
                }
            } else {
                if (daggerActive) {
                    if (currentCharge == 1) {
                        Vector3 aimVector = GetClosestCreatureHead() - summonedDagger.transform.position;
                        aimVector.Normalize();
                        EnableDagger();
                        summonedDagger.GetComponent<Rigidbody>().AddForce(aimVector * 15.0f, ForceMode.Impulse);
                        summonedDagger.Throw();
                        summonedDagger = null;
                    } else {
                        summonedDagger.Despawn();
                    }
                    daggerActive = false;
                }
            }
        }

        private Quaternion GetHandsPointingQuaternion() {
            return Quaternion.Slerp(
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).transform.right * -1.0f),
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.right * -1.0f),
                0.5f
            );
        }

        public Vector3 GetClosestCreatureHead() {
            var nearestCreatures = Creature.list
                    .Where(x => !x.faction.name.Equals("Player") && x.state != Creature.State.Dead);
            if (nearestCreatures.Count() == 0) {
                return summonedDagger.transform.position + GetHandsPointingQuaternion() * Vector3.forward * 10.0f;
            } else {
                return nearestCreatures
                    .Aggregate((a, x) => Vector3.Distance(a.transform.position, summonedDagger.transform.position) < Vector3.Distance(x.transform.position, summonedDagger.transform.position) ? a : x)
                    .animator.GetBoneTransform(HumanBodyBones.Head).position;
            }
        }
        public void EnableDagger() {
            Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }

        private Vector3 GetHandCenterPoint() {
            return Vector3.Lerp(
                Creature.player.body.handLeft.transform.position,
                Creature.player.body.handRight.transform.position,
                0.5f
            );
        }

        private float GetBlackHoleIntensityFromCharge() {
            return (float)Math.Max(Math.Min(Math.Round(Math.Sin(currentCharge * Math.PI) * currentCharge * 1.5f, 3), 1.0f), 0.0f);
        }

        private bool CanGrabWithHand(Side side) {
            return PlayerControl.GetHand(side).gripPressed && Creature.player.body.GetHand(side).interactor.grabbedHandle == null;
        }

        public override void Update() {
            base.Update();
            if (daggerActive) {
                foreach (Imbue imbue in summonedDagger.imbues) {
                    try {
                        imbue.Transfer(spellCharge, currentCharge * imbue.maxEnergy - imbue.energy);
                    } catch { }
                }
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                summonedDagger.transform.rotation = Quaternion.Slerp(
                    summonedDagger.transform.rotation,
                    Quaternion.LookRotation(GetClosestCreatureHead() - summonedDagger.transform.position) * Quaternion.LookRotation(Vector3.left),
                    Time.deltaTime * 10
                );
                summonedDagger.transform.position = Vector3.Lerp(
                    summonedDagger.transform.position,
                    GetHandCenterPoint() + GetHandsPointingQuaternion() * Vector3.forward * Creature.player.mana.mergeHandsDistance / 3.0f,
                    Time.deltaTime * 10);
                if (currentCharge == 1 && CanGrabWithHand(Side.Right)) {
                    EnableDagger();
                    Creature.player.body.handRight.interactor.Grab(summonedDagger.mainHandleRight);
                    daggerActive = false;
                    currentCharge = 0;
                    base.Merge(false);
                } else if (currentCharge == 1 && CanGrabWithHand(Side.Left)) {
                    EnableDagger();
                    Creature.player.body.handLeft.interactor.Grab(summonedDagger.mainHandleLeft);
                    daggerActive = false;
                    currentCharge = 0;
                    base.Merge(false);
                }
            }
        }
    }
}
