using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerMerge : SpellMergeData {
        [global::Sirenix.OdinInspector.BoxGroup("Merge", true, false, 0)]
        [global::Sirenix.OdinInspector.ValueDropdown("GetAllItemID")]
        public string weaponId;
        public int maxDaggers = 6;
        private bool isActive;
        List<FloatingDagger> daggers = new List<FloatingDagger>();
        ItemPhysic daggerBase;
        private GameObject blackHolePrefab;
        private EffectData blackHoleEffect;
        float lastSpawnTime;
        float daggerSpawnTime = 0.5f;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().Where(bundle => bundle.name.Contains("blackhole")).First();
            blackHolePrefab = assetBundle.LoadAsset<GameObject>("BlackHole.prefab");
            blackHoleEffect = Catalog.GetData<EffectData>("SpellDaggerSummonSound");
            daggerBase = Catalog.GetData<ItemPhysic>(weaponId, true);
            daggers = new List<FloatingDagger>();
        }

        private Vector3 GetVelocity(GameObject obj) {
            return obj.GetComponent<Rigidbody>().velocity;
        }

        public override void Merge(bool active) {
            base.Merge(active);
            if (active && !isActive) {
                isActive = true;
            } else {
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
            }
        }

        private FloatingDagger SpawnDagger() {
            FloatingDagger dagger = new FloatingDagger(daggerBase.Spawn(), UnityEngine.Object.Instantiate(blackHolePrefab));
            dagger.item.GetComponent<Rigidbody>().isKinematic = true;
            dagger.item.disallowDespawn = false;
            return dagger;
        }

        private void ReleaseDagger(Item dagger) {
            dagger.GetComponent<Rigidbody>().isKinematic = false;
        }

        private static Vector3 GetHandCenterPoint() {
            return Vector3.Lerp(
                Creature.player.body.handLeft.transform.position,
                Creature.player.body.handRight.transform.position,
                0.5f
            );
        }

        private static Quaternion GetHandsPointingQuaternion() {
            return Quaternion.Slerp(
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).transform.right * -1.0f),
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.right * -1.0f),
                0.5f
            );
        }

        public override void Update() {
            base.Update();
            if (isActive) {
                if (Time.time - lastSpawnTime > daggerSpawnTime && daggers.Count() < maxDaggers) {
                    lastSpawnTime = Time.time;
                    FloatingDagger dagger = SpawnDagger();
                    daggers.Add(dagger);
                    blackHoleEffect.Spawn(dagger.item.transform).Play();
                }
                int i = 0;
                foreach (FloatingDagger dagger in daggers) {
                    dagger.Update(daggers.Count(), i++);
                }
            }
        }
        private static float GetBlackHoleIntensityFromCharge(float charge) {
            return (float)Math.Max(Math.Min(Math.Round(Math.Sin(charge * Math.PI) * charge * 1.5f, 3), 1.0f), 0.0f);
        }


        class FloatingDagger {
            public Item item;
            private float charge = 0.0f;
            private GameObject blackHole;

            public FloatingDagger(Item dagger, GameObject blackHolePrefab) {
                item = dagger;
                item.transform.localScale = Vector3.one * charge;
                item.transform.position = GetTarget(0);
                item.transform.rotation = GetHandsPointingQuaternion() * Quaternion.Euler(0, -90, 0);
                blackHole = UnityEngine.Object.Instantiate(blackHolePrefab, GetTarget(0), Quaternion.identity);
                blackHole.transform.localScale = Vector3.one * 0.25f;
                blackHole.transform.localPosition = Vector3.zero;
                blackHole.GetComponent<Renderer>().material.SetFloat("HoleSize", 0);
                blackHole.GetComponent<Renderer>().material.SetFloat("DistortionStrength", 0);
            }

            private Vector3 GetTarget(float angle) {
                return GetHandCenterPoint()
                        + GetHandsPointingQuaternion()
                        * Quaternion.Euler(0.0f, 0.0f, angle)
                        * Vector3.left
                        * (0.2f + 1.5f * Creature.player.mana.mergeHandsDistance);
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
                item.GetComponent<Rigidbody>().isKinematic = false;
                Vector3 aimVector = GetClosestCreatureHead() - item.transform.position;
                aimVector.Normalize();
                item.GetComponent<Rigidbody>().AddForce(aimVector * 20.0f, ForceMode.Impulse);
                item.Throw();
            }

            public void Update(int count, int number) {
                if (item == null)
                    return;

                //Debug.Log($"{count}, {number}");

                float angle = 360.0f * (number / (float)count);

                if (charge < 1) {
                    charge += 0.03f;
                } else {
                    charge = 1;
                }

                item.transform.position = Vector3.Lerp(item.transform.position, GetTarget(angle), Time.deltaTime * 10.0f);
                item.transform.rotation = Quaternion.Slerp(item.transform.rotation, Quaternion.LookRotation(GetClosestCreatureHead() - item.transform.position) * Quaternion.LookRotation(Vector3.left), Time.deltaTime * 10.0f);
                item.transform.localScale = Vector3.one * charge;
                blackHole.transform.localScale = Vector3.one * GetBlackHoleIntensityFromCharge(charge) * 0.2f;
                blackHole.transform.position = item.transform.position;
                blackHole.transform.rotation = item.transform.rotation;
            }
        }
    }
}
