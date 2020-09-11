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

        private bool daggerActive;
        private ItemPhysic daggerBase;
        private Item summonedDagger;
        private EffectData throwEffect;
        private GameObject blackHolePrefab;
        private GameObject blackHole;
        EffectInstance throwEffectInstance;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            AssetBundle assetBundle = AssetBundle.GetAllLoadedAssetBundles().Where(bundle => bundle.name.Contains("blackhole")).First();
            blackHolePrefab = assetBundle.LoadAsset<GameObject>("BlackHole.prefab");
            daggerBase = Catalog.GetData<ItemPhysic>("DaggerCommon", true);
            throwEffect = Catalog.GetData<EffectData>("SpellTelekinesisPush");
        }

        public override void Init() {
            base.Init();
        }

        private float GetBlackHoleIntensityFromCharge() {
            return (float)Math.Max(Math.Min(Math.Round(Math.Sin(currentCharge * Math.PI) * currentCharge * 1.5f, 3), 1.0f), 0.0f);
        }

        public override void UpdateCaster() {
            base.UpdateCaster();
            if (daggerActive) {
                summonedDagger.transform.localScale = Vector3.one * currentCharge;
                blackHole.transform.localScale = Vector3.one * GetBlackHoleIntensityFromCharge() * 0.2f;
                summonedDagger.transform.position = Vector3.Lerp(summonedDagger.transform.position, GetTargetPosition(), Time.deltaTime * 10);
                switch (spellCaster.bodyHand.side) {
                    case Side.Left:
                        summonedDagger.transform.rotation = Quaternion.Lerp(summonedDagger.transform.rotation, spellCaster.bodyHand.transform.rotation * Quaternion.Euler(0, 0, 180), Time.deltaTime * 10);
                        break;
                    case Side.Right:
                        summonedDagger.transform.rotation = Quaternion.Lerp(summonedDagger.transform.rotation, spellCaster.bodyHand.transform.rotation, Time.deltaTime * 10);
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



        private Bounds GetMaxBounds(GameObject g) {
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) {
                b.Encapsulate(r.bounds);
            }
            return b;
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
                    if (summonedDagger != null)
                        UnityEngine.Object.Destroy(summonedDagger);
                }
                daggerActive = false;
            }
        }

        public override void Throw(Vector3 velocity) {
            if (daggerActive) {
                base.Throw(velocity);
                EnableDagger();
                Rigidbody rb = summonedDagger.GetComponent<Rigidbody>();
                summonedDagger.transform.rotation = Quaternion.LookRotation(velocity) * Quaternion.LookRotation(Vector3.left);
                rb.AddForce(velocity * 5, ForceMode.Impulse);
                //throwEffectInstance = throwEffect.Spawn(Creature.player.body.GetHand(spellCaster.bodyHand.side).transform);
                throwEffectInstance = throwEffect.Spawn(Vector3.zero, Quaternion.identity);
                throwEffectInstance.Play();
                throwEffectInstance.SetIntensity(1f);
                //foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(throwEffectInstance)) {
                //    string name = descriptor.Name;
                //    object value = descriptor.GetValue(throwEffectInstance);
                //    Debug.Log($"{name}={value}");
                //}
                //throwEffectInstance.onEffectFinished += (effect) => { effect.Stop(); effect.Despawn(); };
                summonedDagger.Throw();
                daggerActive = false;
            }
        }
    }
}
