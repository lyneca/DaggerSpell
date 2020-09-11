using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerGravityMerge : SpellMergeData {
        private ItemPhysic daggerBase;
        private Item summonedDagger;
        private SpellCastCharge gravitySpellCharge;
        private bool daggerActive;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            daggerBase = Catalog.GetData<ItemPhysic>("DaggerCommon", true);
            gravitySpellCharge = Catalog.GetData<SpellCastCharge>("Gravity");
        }

        public override void Merge(bool active) {
            base.Merge(active);
            if (active) {
                if (!daggerActive) {
                    daggerActive = true;
                    summonedDagger = daggerBase.Spawn();
                    summonedDagger.GetComponent<Rigidbody>().isKinematic = true;
                    summonedDagger.transform.rotation = Quaternion.Slerp(
                        summonedDagger.transform.rotation,
                        Quaternion.LookRotation(GetClosestCreatureHead() - summonedDagger.transform.position),
                        Time.deltaTime * 10.0f
                    );

                    summonedDagger.transform.position = GetHandCenterPoint();
                }
            } else {
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
                return GetHandsPointingQuaternion() * Vector3.forward * 10.0f;
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

        public override void Update() {
            base.Update();
            if (daggerActive) {
                foreach (Imbue imbue in summonedDagger.imbues) {
                    try {
                        imbue.Transfer(gravitySpellCharge, currentCharge * imbue.maxEnergy - imbue.energy);
                    } catch { }
                }
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                summonedDagger.transform.rotation = Quaternion.Slerp(
                    summonedDagger.transform.rotation,
                    Quaternion.LookRotation(GetClosestCreatureHead() - summonedDagger.transform.position) * Quaternion.LookRotation(Vector3.left),
                    Time.deltaTime * 10
                );
                summonedDagger.transform.position = Vector3.Lerp(summonedDagger.transform.position, GetHandCenterPoint(), Time.deltaTime * 10);
            }
        }
    }
}
