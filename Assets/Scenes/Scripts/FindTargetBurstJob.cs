/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

/*
public class FindTargetSystem : ComponentSystem {

    protected override void OnUpdate() {
        Entities.WithNone<HasTarget>().WithAll<Unit>().ForEach((Entity entity, ref Translation unitTranslation) => {
            // Code running on all entities with "Unit" Tag

            float3 unitPosition = unitTranslation.Value;
            Entity closestTargetEntity = Entity.Null;
            float3 closestTargetPosition = float3.zero;

            Entities.WithAll<Target>().ForEach((Entity targetEntity, ref Translation targetTranslation) => { 
                // Cycling through all entities with "Target" Tag

                if (closestTargetEntity == Entity.Null) {
                    // No target
                    closestTargetEntity = targetEntity;
                    closestTargetPosition = targetTranslation.Value;
                } else {
                    if (math.distance(unitPosition, targetTranslation.Value) < math.distance(unitPosition, closestTargetPosition)) {
                        // This target is closer
                        closestTargetEntity = targetEntity;
                        closestTargetPosition = targetTranslation.Value;
                    }
                }
            });

            // Closest Target
            if (closestTargetEntity != Entity.Null) {
                //PostUpdateCommands.AddComponent(entity, new HasTarget { targetEntity = closestTargetEntity });
            }
        });
    }

}
*/
public class FindTargetJobSystem : JobComponentSystem {

    private struct EntityWithPosition {
        public Entity entity;
        public float3 position;
    }

    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    [BurstCompile]
    private struct FindTargetBurstJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EntityWithPosition> targetArray;
        public NativeArray<Entity> closestTargetEntityArray;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
            float3 unitPosition = translation.Value;
            Entity closestTargetEntity = Entity.Null;
            float3 closestTargetPosition = float3.zero;

            for (int i = 0;i < targetArray.Length;i++) {
                // Cycling through all target entities
                EntityWithPosition targetEntityWithPosition = targetArray[i];

                if (closestTargetEntity == Entity.Null) {
                    // No target
                    closestTargetEntity = targetEntityWithPosition.entity;
                    closestTargetPosition = targetEntityWithPosition.position;
                } else {
                    if (math.distance(unitPosition, targetEntityWithPosition.position) < math.distance(unitPosition, closestTargetPosition)) {
                        // This target is closer
                        closestTargetEntity = targetEntityWithPosition.entity;
                        closestTargetPosition = targetEntityWithPosition.position;
                    }
                }
            }

            closestTargetEntityArray[index] = closestTargetEntity;
        }

    }

    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AddComponentJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> closestTargetEntityArray;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity entity, int index, ref Translation translation) {
            if (closestTargetEntityArray[index] != Entity.Null) {
                entityCommandBuffer.AddComponent(index, entity, new HasTarget { targetEntity = closestTargetEntityArray[index] });
            }
        }

    }


    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        EntityQuery targetQuery = GetEntityQuery(typeof(Target), ComponentType.ReadOnly<Translation>());

        NativeArray<Entity> targetEntityArray = targetQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> targetTranslationArray = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        NativeArray<EntityWithPosition> targetArray = new NativeArray<EntityWithPosition>(targetEntityArray.Length, Allocator.TempJob);

        for (int i = 0;i < targetEntityArray.Length;i++) {
            targetArray[i] = new EntityWithPosition {
                entity = targetEntityArray[i],
                position = targetTranslationArray[i].Value,
            };
        }

        targetEntityArray.Dispose();
        targetTranslationArray.Dispose();

        EntityQuery unitQuery = GetEntityQuery(typeof(Unit), ComponentType.Exclude<HasTarget>());
        NativeArray<Entity> closestTargetEntityArray = new NativeArray<Entity>(unitQuery.CalculateEntityCount(), Allocator.TempJob);


        FindTargetBurstJob findTargetBurstJob = new FindTargetBurstJob {
            targetArray = targetArray,
            closestTargetEntityArray = closestTargetEntityArray
        };
        JobHandle jobHandle = findTargetBurstJob.Schedule(this, inputDeps);

        AddComponentJob addComponentJob = new AddComponentJob {
            closestTargetEntityArray = closestTargetEntityArray,
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };
        jobHandle = addComponentJob.Schedule(this, jobHandle);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }

}

