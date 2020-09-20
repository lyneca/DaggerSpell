using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using System.ComponentModel;

namespace DaggerSpell {
    public class DaggerSpell : SpellCastCharge {
        [global::Sirenix.OdinInspector.BoxGroup("Merge", true, false, 0)]
        [global::Sirenix.OdinInspector.ValueDropdown("GetAllItemID")]
        public string weaponId;
        private bool daggerActive;
        private ItemPhysic daggerBase;
        public Item summonedDagger;
        private EffectData throwEffect;
        private GameObject blackHolePrefab;
        private GameObject blackHole;
        EffectInstance throwEffectInstance;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().Where(bundle => bundle.name.Contains("blackhole")).First();
            blackHolePrefab = assetBundle.LoadAsset<GameObject>("BlackHole.prefab");
            daggerBase = Catalog.GetData<ItemPhysic>(weaponId, true);
            throwEffect = Catalog.GetData<EffectData>("SpellTelekinesisPush");
        }

        public override void Init() {
            base.Init();
        }

        public override void UpdateImbue() {
            base.UpdateImbue();
        }

        private float GetBlackHoleIntensityFromCharge() {
            return (float)Math.Max(Math.Min(Math.Round(Math.Sin(currentCharge * Math.PI) * currentCharge * 1.5f, 3), 1.0f), 0.0f);
        }

        private void PointItemFlyRefAtTarget(Item item, Vector3 target, float lerpFactor) {
            item.transform.rotation = Quaternion.Slerp(
                item.transform.rotation * item.definition.flyDirRef.localRotation,
                Quaternion.LookRotation(target),
                lerpFactor) * Quaternion.Inverse(item.definition.flyDirRef.localRotation);
        }

        public override void UpdateCaster() {
            base.UpdateCaster();
            if (daggerActive) {
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                blackHole.transform.localScale = Vector3.one * GetBlackHoleIntensityFromCharge() * 0.2f;
                summonedDagger.transform.position = Vector3.Lerp(summonedDagger.transform.position, GetTargetPosition(), Time.deltaTime * 10);
                Quaternion toRotation = Quaternion.identity;
                switch (spellCaster.bodyHand.side) {
                    case Side.Left:
                        PointItemFlyRefAtTarget(summonedDagger, -spellCaster.bodyHand.transform.right, Time.deltaTime * 10.0f);
                        break;
                    case Side.Right:
                        PointItemFlyRefAtTarget(summonedDagger, spellCaster.bodyHand.transform.right, Time.deltaTime * 10.0f);
                        break;
                }
                blackHole.transform.position = summonedDagger.transform.position;
                blackHole.transform.rotation = summonedDagger.transform.rotation;
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
            UnityEngine.Object.Destroy(blackHole);
            rb.isKinematic = false;
        }

        public override void Fire(bool active) {
            base.Fire(active);
            if (active) {
                if (!daggerActive) {
                    daggerActive = true;
                    summonedDagger = daggerBase.Spawn(true, null);
                    summonedDagger.GetComponent<Rigidbody>().isKinematic = true;
                    summonedDagger.transform.position = GetTargetPosition();
                    blackHole = UnityEngine.Object.Instantiate(blackHolePrefab, summonedDagger.transform.position, summonedDagger.transform.rotation);
                    blackHole.transform.localScale = Vector3.one * 0.25f;
                    blackHole.transform.localPosition = Vector3.zero;
                    blackHole.GetComponent<Renderer>().material.SetFloat("HoleSize", 0);
                    blackHole.GetComponent<Renderer>().material.SetFloat("DistortionStrength", 0);
                }
            } else {
                EnableDagger();
                if (currentCharge != 1) {
                    summonedDagger.Despawn();
                }
                daggerActive = false;
                currentCharge = 0;
                spellCaster.grabbedFire = false;
                spellCaster.isFiring = false;
                spellCaster.bodyHand.interactor.ClearTouch();
            }
        }

        public override void Throw(Vector3 velocity) {
            if (daggerActive) {
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
    }
}
