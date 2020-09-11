using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace DaggerSpell {
    class DaggerMerge : SpellMergeData {
        public int numDaggers = 3;
        private bool isActive;
        List<Item> daggers = new List<Item>();
        ItemPhysic daggerBase;
        public override void OnCatalogRefresh() {
            base.OnCatalogRefresh();
            daggerBase = Catalog.GetData<ItemPhysic>("DaggerCommon", true);
            daggers = new List<Item>();
        }

        private Vector3 GetVelocity(GameObject obj) {
            return obj.GetComponent<Rigidbody>().velocity;
        }

        // private void RenderLines(GameObject bone) {
        //     LineRenderer lineRenderer = bone.GetComponent<LineRenderer>() ?? bone.AddComponent<LineRenderer>();
        //     lineRenderer.positionCount = 2;
        //     lineRenderer.startWidth = 0.01f;
        //     lineRenderer.endWidth = 0.01f;
        //     lineRenderer.startColor = Color.green;
        //     lineRenderer.endColor = Color.green;
        //     lineRenderer.SetPosition(0, bone.transform.position);
        //     lineRenderer.SetPosition(1, bone.transform.right * -10.0f + bone.transform.position);
        // }

        public override void Merge(bool active) {
            base.Merge(active);
            // RenderLines(Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject);
            // RenderLines(Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject);
            if (active && !isActive) {
                isActive = true;
                for (int i = 0; i < 3; i++) {
                    Item dagger = spawnDagger();
                    daggers.Add(dagger);
                    Vector3 target = Vector3.Lerp(
                        Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                        Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).position,
                        (i + 1.0f) / 4.0f
                     );
                    dagger.transform.position = target;
                    dagger.transform.rotation = GetHandsPointingQuaternion() * Quaternion.Euler(0, -90, 0);
                }
            } else {
                if (currentCharge == 1) {
                    foreach (Item dagger in daggers) {
                        releaseDagger(dagger);
                        dagger.GetComponent<Rigidbody>().AddForce(GetHandsPointingQuaternion() * Vector3.forward * 20.0f, ForceMode.Impulse);
                        dagger.Throw();
                    }
                } else {
                    foreach (Item dagger in daggers) {
                        dagger.Despawn();
                    }
                }
                daggers.Clear();
                isActive = false;
            }
        }

        private Item spawnDagger() {
            Item dagger = daggerBase.Spawn();
            dagger.GetComponent<Rigidbody>().isKinematic = true;
            return dagger;
        }

        private void releaseDagger(Item dagger) {
            dagger.GetComponent<Rigidbody>().isKinematic = false;
        }

        private Vector3 GetHandCenterPoint() {
            return Vector3.Lerp(
                Creature.player.body.handLeft.transform.position,
                Creature.player.body.handRight.transform.position,
                0.5f
            );
        }

        private Quaternion GetHandsPointingQuaternion() {
            return Quaternion.Slerp(
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).transform.right * -1.0f),
                Quaternion.LookRotation(Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.right * -1.0f),
                0.5f
            );
        }

        private void PositionDaggers() {
            int i = 0;
            foreach (Item dagger in daggers) {
                if (dagger == null)
                    continue;
                dagger.transform.localScale = Vector3.one * currentCharge;
                Vector3 target = Vector3.Lerp(
                    Creature.player.animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                    Creature.player.animator.GetBoneTransform(HumanBodyBones.RightHand).position,
                    ++i / 4.0f
                 );
                dagger.transform.position = Vector3.Lerp(dagger.transform.position, target, Time.deltaTime * 10.0f);
                dagger.transform.rotation = Quaternion.Slerp(dagger.transform.rotation, GetHandsPointingQuaternion() * Quaternion.Euler(0, -90, 0), Time.deltaTime * 10.0f);
            }
        }

        public override void Update() {
            base.Update();
            PositionDaggers();
        }
    }
}
