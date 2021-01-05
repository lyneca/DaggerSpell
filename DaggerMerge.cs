using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using Utils.ExtensionMethods;

namespace DaggerSpell {
    class DaggerMerge : SpellMergeData {
        public string weaponId;
        public int maxDaggers = 6;
        private bool isActive;
        private bool isGripping;
        List<FloatingDagger> daggers = new List<FloatingDagger>();
        ItemPhysic daggerBase;
        private GameObject blackHolePrefab;
        private EffectData blackHoleEffect;
        float lastSpawnTime;
        float daggerSpawnTime = 0.5f;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            Addressables.LoadAssetAsync<GameObject>("Lyneca.DaggerSpell.BlackHole").Task.Then(obj => blackHolePrefab = obj);
            blackHoleEffect = Catalog.GetData<EffectData>("SpellDaggerSummonSound");
            daggerBase = Catalog.GetData<ItemPhysic>(weaponId ?? Catalog.GetData<DaggerSpell>("DaggerSpell").weaponId, true);
            daggers = new List<FloatingDagger>();
        }

        private Vector3 GetVelocity(GameObject obj) {
            return obj.GetComponent<Rigidbody>().velocity;
        }

        public override void Merge(bool active) {
            if (active && !isActive && Player.currentCreature.handLeft.grabbedHandle == null && Player.currentCreature.handRight.grabbedHandle == null) {
                base.Merge(true);
                isActive = true;
                if (Player.currentCreature.mana.casterLeft.spellInstance is DaggerSpell leftSpell && leftSpell.summonedDagger != null) {
                    leftSpell.summonedDagger.gameObject.SetActive(false);
                }
                if (Player.currentCreature.mana.casterRight.spellInstance is DaggerSpell rightSpell && rightSpell.summonedDagger != null) {
                    rightSpell.summonedDagger.gameObject.SetActive(false);
                }
            } else {
                ThrowOrDespawn();
                base.Merge(false);
                currentCharge = 0;
            }
        }

        private FloatingDagger SpawnDagger() {
            FloatingDagger dagger = new FloatingDagger(
                daggerBase,
                UnityEngine.Object.Instantiate(blackHolePrefab),
                blackHoleEffect,
                daggers.Count(),
                daggers.Count() + 1);
            return dagger;
        }

        private void ReleaseDagger(Item dagger) {
            dagger.GetComponent<Rigidbody>().isKinematic = false;
            dagger.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private static Vector3 GetHandCenterPoint() {
            return Vector3.Lerp(
                Player.currentCreature.handLeft.transform.position,
                Player.currentCreature.handRight.transform.position,
                0.5f
            );
        }

        private static Quaternion GetHandsPointingQuaternion() {
            return Quaternion.Slerp(
                Quaternion.LookRotation(Player.currentCreature.animator.GetBoneTransform(HumanBodyBones.RightHand).transform.right * -1.0f),
                Quaternion.LookRotation(Player.currentCreature.animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.right * -1.0f),
                0.5f
            );
        }

        public void ThrowOrDespawn() {
            if (currentCharge == 1) {
                foreach (FloatingDagger dagger in daggers) {
                    dagger.Throw();
                    dagger.Destroy();
                }
            } else {
                foreach (FloatingDagger dagger in daggers) {
                    dagger.item.Despawn();
                    dagger.Destroy();
                }
            }
            daggers.Clear();
            isActive = false;
            isGripping = false;
        }

        public static bool IsPlayerGripping() {
            return PlayerControl.handLeft.gripPressed && PlayerControl.handRight.gripPressed;
        }

        public void HandleDaggerThrowCondition() {
            if (IsPlayerGripping()) {
                if (isActive && !isGripping)
                    isGripping = true;
            } else {
                if (!isActive && isGripping) {
                    ThrowOrDespawn();
                }
            }
        }

        public override void Update() {
            base.Update();
            if (isActive) {
                HandleDaggerThrowCondition();
                if (Time.time - lastSpawnTime > daggerSpawnTime && daggers.Count() < maxDaggers) {
                    lastSpawnTime = Time.time;
                    FloatingDagger dagger = SpawnDagger();
                    daggers.Add(dagger);
                }
                int i = 0;
                foreach (FloatingDagger dagger in daggers) {
                    dagger.Update(i++, daggers.Count());
                }
            }
        }

        private static float GetBlackHoleIntensityFromCharge(float charge) {
            return (float)Mathf.Clamp((float)Math.Round(Math.Sin(charge * Math.PI) * charge * 1.5f, 3), 0.0f, 0.1f);
        }

        class FloatingDagger {
            public Item item;
            private float charge = 0.0f;
            private GameObject blackHole;

            public FloatingDagger(ItemPhysic daggerBase, GameObject blackHolePrefab, EffectData soundEffect, int number, int count) {
                daggerBase.SpawnAsync(dagger => {
                    item = dagger;
                    item.transform.localScale = Vector3.one * charge;
                    item.transform.position = GetTarget(number, count);
                    soundEffect.Spawn(dagger.transform).Play();
                    PointItemFlyRefAtTarget(item, GetHandsPointingQuaternion() * Vector3.forward, 1.0f);
                    item.rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    item.GetComponent<Rigidbody>().isKinematic = true;
                    item.disallowDespawn = false;
                    blackHole = UnityEngine.Object.Instantiate(blackHolePrefab, item.transform.position, Quaternion.identity);
                    blackHole.transform.localScale = Vector3.one * 0.25f;
                    blackHole.transform.localPosition = Vector3.zero;
                    blackHole.GetComponent<Renderer>().material.SetFloat("HoleSize", 0);
                    blackHole.GetComponent<Renderer>().material.SetFloat("DistortionStrength", 0);
                });
            }

            private void PointItemFlyRefAtTarget(Item item, Vector3 target, float lerpFactor) {
                item.transform.rotation = Quaternion.Slerp(
                    item.transform.rotation * item.flyDirRef.localRotation,
                    Quaternion.LookRotation(target),
                    lerpFactor) * Quaternion.Inverse(item.flyDirRef.localRotation);
            }

            private Vector3 GetTarget(int number, int count) {
                if (IsPlayerGripping()) {
                    return Vector3.LerpUnclamped(
                        Player.currentCreature.handLeft.transform.position,
                        Player.currentCreature.handRight.transform.position,
                        number / (Math.Max(count - 1.0f, 1)) * 3.0f - 1.0f) + GetHandsPointingQuaternion()
                            * Vector3.forward
                            * (0.3f + Math.Abs(number - Math.Max(count - 1.0f, 1) / 2.0f) / (Math.Max(count - 1.0f, 1) / 2.0f) * 0.5f);
                } else {
                    float angle = 360.0f * (number / (float)count);
                    return GetHandCenterPoint()
                            + GetHandsPointingQuaternion()
                            * Quaternion.Euler(0.0f, 0.0f, angle)
                            * Vector3.left
                            * (0.2f + 1.5f * Player.currentCreature.mana.mergeHandsDistance);
                }
            }

            public void Destroy() {
                UnityEngine.Object.Destroy(blackHole);
            }

            public Vector3 GetClosestCreatureHead() {
                var nearestCreatures = Creature.list
                        .Where(x => !x.faction.name.Equals("Player") && x.state != Creature.State.Dead);
                if (nearestCreatures.Count() == 0) {
                    return item.transform.position + GetHandsPointingQuaternion() * Vector3.forward * 10.0f;
                } else {
                    return nearestCreatures
                        .Aggregate((a, x) => Vector3.Distance(a.transform.position, item.transform.position) < Vector3.Distance(x.transform.position, item.transform.position) ? a : x)
                        .animator.GetBoneTransform(HumanBodyBones.Head).position;
                }
            }

            public void Throw() {
                if (!item)
                    return;
                item.rb.isKinematic = false;
                item.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Vector3 aimVector = GetClosestCreatureHead() - item.transform.position;
                aimVector.Normalize();
                item.rb.AddForce(aimVector * 20.0f, ForceMode.Impulse);
                item.Throw();
            }

            public void Update(int number, int count) {
                if (!item)
                    return;

                //Debug.Log($"{count}, {number}");

                if (charge < 1) {
                    charge += 0.03f;
                } else {
                    charge = 1;
                }

                item.transform.position = Vector3.Lerp(item.transform.position, GetTarget(number, count), Time.deltaTime * 10.0f);
                PointItemFlyRefAtTarget(item, GetClosestCreatureHead() - item.transform.position, Time.deltaTime * 10.0f);

                item.transform.localScale = Vector3.one * charge;
                blackHole.transform.localScale = Vector3.one * GetBlackHoleIntensityFromCharge(charge) * 0.2f;
                blackHole.transform.position = item.transform.position;
                blackHole.transform.rotation = item.transform.rotation;
            }
        }
    }
}
