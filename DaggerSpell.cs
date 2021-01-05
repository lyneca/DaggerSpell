using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using System.ComponentModel;
using Utils.ExtensionMethods;

namespace DaggerSpell {
    public class DaggerSpell : SpellCastCharge {
        public string weaponId;
        public bool magicFX;
        private bool daggerActive;
        private ItemPhysic daggerBase;
        public Item summonedDagger;
        private EffectData throwEffect;
        private GameObject blackHolePrefab;
        private GameObject blackHole;
        EffectInstance throwEffectInstance;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            Addressables.LoadAssetAsync<GameObject>("Lyneca.DaggerSpell.BlackHole").Task.Then(obj => blackHolePrefab = obj);
            daggerBase = Catalog.GetData<ItemPhysic>(weaponId, true);
            throwEffect = Catalog.GetData<EffectData>("SpellTelekinesisPush");
        }

        public override void Init() {
            base.Init();
        }

        public override void UpdateImbue() {
            base.UpdateImbue();
        }

        public override void Load(Imbue imbue) {
            if (imbue.colliderGroup.collisionHandler.item == summonedDagger) return;
            base.Load(imbue);
            TrackingHandler handler = imbue.colliderGroup.collisionHandler.item.gameObject.AddComponent<TrackingHandler>();
            handler.imbue = imbue;
        }

        public override void OnImbueCollisionStart(ref CollisionStruct collisionInstance) {
            base.OnImbueCollisionStart(ref collisionInstance);
            if (collisionInstance.targetColliderGroup.collisionHandler.isRagdollPart)
                collisionInstance.sourceColliderGroup.imbue.energy = 0;
        }

        private float GetBlackHoleIntensityFromCharge() {
            return (float)Math.Max(Math.Min(Math.Round(Math.Sin(currentCharge * Math.PI) * currentCharge * 1.5f, 3), 1.0f), 0.0f);
        }

        private void PointItemFlyRefAtTarget(Item item, Vector3 target, float lerpFactor) {
            item.transform.rotation = Quaternion.Slerp(
                item.transform.rotation * item.flyDirRef.localRotation,
                Quaternion.LookRotation(target),
                lerpFactor) * Quaternion.Inverse(item.flyDirRef.localRotation);
        }

        public override void UpdateCaster() {
            base.UpdateCaster();
            if (daggerActive) {
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                blackHole.transform.localScale = Vector3.one * GetBlackHoleIntensityFromCharge() * 0.2f;
                summonedDagger.transform.position = Vector3.Lerp(summonedDagger.transform.position, GetTargetPosition(), Time.deltaTime * 10);
                Quaternion toRotation = Quaternion.identity;
                PointItemFlyRefAtTarget(summonedDagger, -spellCaster.ragdollHand.transform.right, Time.deltaTime * 10.0f);
                blackHole.transform.position = summonedDagger.transform.position;
                blackHole.transform.rotation = summonedDagger.transform.rotation;
                if (PlayerControl.GetHand(spellCaster.ragdollHand.side).gripPressed && spellCaster.ragdollHand.grabbedHandle == null && currentCharge == 1) {
                    EnableDagger();
                    spellCaster.ragdollHand.Grab(summonedDagger.GetMainHandle(spellCaster.ragdollHand.side));
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
            UnityEngine.Object.Destroy(blackHole);
            rb.isKinematic = false;
        }

        public override void Fire(bool active) {
            base.Fire(active);
            if (active) {
                if (!daggerActive) {
                    daggerActive = true;
                    daggerBase.SpawnAsync(dagger => {
                        summonedDagger = dagger;
                        summonedDagger.GetComponent<Rigidbody>().isKinematic = true;
                        summonedDagger.transform.position = GetTargetPosition();
                        blackHole = UnityEngine.Object.Instantiate(blackHolePrefab, summonedDagger.transform.position, summonedDagger.transform.rotation);
                        blackHole.transform.localScale = Vector3.one * 0.25f;
                        blackHole.transform.localPosition = Vector3.zero;
                        blackHole.GetComponent<Renderer>().material.SetFloat("HoleSize", 0);
                        blackHole.GetComponent<Renderer>().material.SetFloat("DistortionStrength", 0);
                    });
                }
            } else {
                EnableDagger();
                if (currentCharge != 1) {
                    summonedDagger?.Despawn();
                }
                daggerActive = false;
                currentCharge = 0;
                spellCaster.grabbedFire = false;
                spellCaster.isFiring = false;
                spellCaster.ragdollHand.ClearTouch();
            }
        }

        public override void Throw(Vector3 velocity) {
            if (summonedDagger && daggerActive) {
                base.Throw(velocity);
                EnableDagger();
                Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
                PointItemFlyRefAtTarget(summonedDagger, velocity, 1.0f);
                rb.AddForce(velocity * 5, ForceMode.Impulse);
                throwEffectInstance = throwEffect.Spawn(spellCaster.magicSource);
                throwEffectInstance.SetTarget(summonedDagger.transform);
                throwEffectInstance.Play();
                summonedDagger.Throw();
                daggerActive = false;
            }
        }
        class TrackingHandler : MonoBehaviour {
            public Imbue imbue;
            public void Update() {
                Item item = GetComponent<Item>();
                if (imbue && imbue.energy > 0
                    && item.handlers.Count == 0
                    && !item.isGripped
                    && !item.isTelekinesisGrabbed) {
                    Vector3 target = Physics.OverlapSphere(transform.position, 5.0f)
                        .Where(
                            collider => {
                                RagdollPart part = collider.gameObject.GetComponent<RagdollPart>();
                                return part != null
                                    && part.ragdoll?.creature?.state != Creature.State.Dead
                                    && part.ragdoll?.creature != Player.currentCreature;
                            })
                        .Select(collider => collider.gameObject.GetComponent<RagdollPart>().ragdoll.headPart.transform.position)
                        .Aggregate(
                            (curMin, position) => curMin == null
                                || Vector3.Distance(transform.position, position) < Vector3.Distance(transform.position, position)
                                ? position : curMin);
                    Rigidbody rb = GetComponent<Rigidbody>();
                    item.Throw();
                    rb.AddForce((target - transform.position).normalized * rb.mass, ForceMode.Impulse);
                }
            }
        }
    }
}
