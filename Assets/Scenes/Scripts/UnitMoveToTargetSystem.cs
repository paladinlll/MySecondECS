using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class UnitMoveToTargetSystem : ComponentSystem {
    protected override void OnUpdate() {
        Entities.ForEach((Entity unitEntity, ref HasTarget hasTarget, ref Translation translation) => {
            if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(hasTarget.targetEntity)) {
                Translation targetTranslation = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);

                float3 targetDir = math.normalize(targetTranslation.Value - translation.Value);
                float moveSpeed = 0.5f;
                translation.Value += targetDir * moveSpeed * Time.DeltaTime;

                if (math.distance(translation.Value, targetTranslation.Value) < .2f) {
                    // Close to target, destroy it
                    PostUpdateCommands.DestroyEntity(hasTarget.targetEntity);
                    PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
                }
            } else {
                // Target Entity already destroyed
                PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
            }
        });
    }
}
