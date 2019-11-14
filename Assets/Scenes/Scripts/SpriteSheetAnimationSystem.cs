using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class SpriteSheetAnimationSystem : JobComponentSystem {
    protected override void OnCreate() {

        //material = Resources.Load("Test", typeof(Material)) as Material;
    }

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct SpriteSheetAnimationSystemJob : IJobForEach<Translation, SpriteSheetAnimationComponent> {
        // Add fields here that your job needs to do its work.
        // For example,
        public float deltaTime;

        public void Execute(ref Translation translation, ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) {
            spriteSheetAnimationComponent.frameTimer += deltaTime;
            while(spriteSheetAnimationComponent.frameTimer > spriteSheetAnimationComponent.frameTimeMax) {
                var anim = spriteSheetAnimationComponent.spriteSheet.Value.animations[spriteSheetAnimationComponent.animationId];

                spriteSheetAnimationComponent.frameTimer -= spriteSheetAnimationComponent.frameTimeMax;
                spriteSheetAnimationComponent.currentFrame = (spriteSheetAnimationComponent.currentFrame + 1) % anim.totalFrame;
                var frame = spriteSheetAnimationComponent.spriteSheet.Value.frames[anim.startFrame + spriteSheetAnimationComponent.currentFrame];
                var module = spriteSheetAnimationComponent.spriteSheet.Value.modules[frame.mid];
                float uvWidth = module.w;
                float uvHeight = module.h;
                float uvOffsetX = module.x;
                float uvOffsetY = module.y;
                spriteSheetAnimationComponent.uv = new UnityEngine.Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

                float3 position = translation.Value;
                position.z = position.y * 0.1f;
                //position.x += frame.ox + module.w / 2;
                //position.y -= frame.oy - module.h / 2;

                float3 scale = new float3(10 * module.w, 10 * module.h, 1);

                position.x -= -10 * frame.ox - scale.x / 2;
                position.y += -10 * frame.oy - scale.y / 2;
                spriteSheetAnimationComponent.matrix = Matrix4x4.TRS(position, Quaternion.identity, scale);
            }
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as [ReadOnly], which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;


        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        var job = new SpriteSheetAnimationSystemJob();

        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        job.deltaTime = UnityEngine.Time.deltaTime;



        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);
    }
}