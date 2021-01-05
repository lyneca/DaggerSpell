using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Utils {
    namespace ExtensionMethods {
        static class ExtensionMethods {
            public static Task<TOutput> Then<TInput, TOutput>(this Task<TInput> task, Func<TInput, TOutput> func) {
                return task.ContinueWith((input) => func(input.Result));
            }
            public static Task Then(this Task task, Action<Task> func) {
                return task.ContinueWith(func);
            }
            public static Task Then<TInput>(this Task<TInput> task, Action<TInput> func) {
                return task.ContinueWith((input) => func(input.Result));
            }
            public static T GetOrAddComponent<T>(this GameObject obj) where T : Component {
                return obj.GetComponent<T>() ?? obj.AddComponent<T>();
            }
            public static void Play(this WhooshPoint point) {
                if ((Utils.GetInstanceField(point, "trigger") is WhooshPoint.Trigger trigger) && trigger != WhooshPoint.Trigger.OnGrab  && Utils.GetInstanceField(point, "effectInstance") != null)
                    (Utils.GetInstanceField(point, "effectInstance") as EffectInstance)?.Play();
                Utils.SetInstanceField(point, "effectActive", true);
                Utils.SetInstanceField(point, "dampenedIntensity", 0);
            }
        }
    }
    static class Utils {
        // WARNING: If you can find a way to not use the following two methods, please do - they are INCREDIBLY bad practice
        // Get a private field from an object
        internal static object GetInstanceField<T>(T instance, string fieldName) {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = instance.GetType().GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        // Set a private field from an object
        internal static void SetInstanceField<T, U>(T instance, string fieldName, U value) {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = instance.GetType().GetField(fieldName, bindFlags);
            field.SetValue(instance, value);
        }

        // Taken from walterellisfun on github: https://github.com/walterellisfun/ConeCast/blob/master/ConeCastExtension.cs
        public static RaycastHit[] ConeCastAll(Vector3 origin, float maxRadius, Vector3 direction, float maxDistance, float coneAngle) {
            RaycastHit[] sphereCastHits = Physics.SphereCastAll(origin - new Vector3(0, 0, maxRadius), maxRadius, direction, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            List<RaycastHit> coneCastHitList = new List<RaycastHit>();

            if (sphereCastHits.Length > 0) {
                for (int i = 0; i < sphereCastHits.Length; i++) {
                    Vector3 hitPoint = sphereCastHits[i].point;
                    Vector3 directionToHit = hitPoint - origin;
                    float angleToHit = Vector3.Angle(direction, directionToHit);

                    if (angleToHit < coneAngle) {
                        coneCastHitList.Add(sphereCastHits[i]);
                    }
                }
            }

            return coneCastHitList.ToArray();
        }
    }
}
