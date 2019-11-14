using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class SpriteSheetRenderer : ComponentSystem {
    Matrix4x4[] matrixInstanceArray = new Matrix4x4[1023];
    Vector4[] uvInstanceArray = new Vector4[1023];

    [BurstCompile]
    private struct CopySpriteJob : IJobForEachWithEntity< SpriteSheetAnimationComponent> {
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet0NativeQueue;

        public void Execute(Entity entity, int index, ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) {
            sheet0NativeQueue.Enqueue(spriteSheetAnimationComponent);
        }
    }

    [BurstCompile]
    private struct NativeQueueToArrayJob : IJob {
        public NativeQueue<SpriteSheetAnimationComponent> nativeQueue;
        public NativeArray<SpriteSheetAnimationComponent> nativeArray;

        public void Execute() {
            int index = 0;

            SpriteSheetAnimationComponent renderData;
            while(nativeQueue.TryDequeue(out renderData)) {
                nativeArray[index++] = renderData;
            }
        }
    }

    [BurstCompile]
    private struct FillArrayForParalleJob : IJobParallelFor {
        [ReadOnly] public NativeArray<SpriteSheetAnimationComponent> nativeArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Matrix4x4> matrixArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Vector4> uvArray;
        public int startingIndex;

        public void Execute(int index) {
            SpriteSheetAnimationComponent renderData = nativeArray[index];
            matrixArray[startingIndex + index] = renderData.matrix;
            uvArray[startingIndex + index] = renderData.uv;
        }
    }

    [BurstCompile]
    private struct SortLayerJob : IJob {
        public NativeArray<SpriteSheetAnimationComponent> sortArray;
       
        public void Execute() {
            for(int i = 0;i < sortArray.Length;i++) {
                for(int j = 0;j < sortArray.Length;j++) {
                    if(sortArray[i].sortingOrder < sortArray[j].sortingOrder) {
                        SpriteSheetAnimationComponent tmp = sortArray[i];
                        sortArray[i] = sortArray[j];
                        sortArray[j] = tmp;
                    }
                }
            }
        }
    }

    protected override void OnUpdate() {
        //var camera = Camera.main;
        //Entities.ForEach((ref Translation translation, ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) => {
        //    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        //    materialPropertyBlock.SetVector("_MainTex_ST", spriteSheetAnimationComponent.uv);
        //    Graphics.DrawMesh(
        //        Testing.Instance.mesh,
        //        spriteSheetAnimationComponent.matrix,
        //        Testing.Instance.material,
        //        0,
        //        camera,
        //        0,
        //        materialPropertyBlock
        //    );

        //});

        NativeQueue<SpriteSheetAnimationComponent> sheet0NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
        CopySpriteJob copyTileJob = new CopySpriteJob {
            sheet0NativeQueue = sheet0NativeQueue.AsParallelWriter()
        };
        JobHandle jobHandle = copyTileJob.Schedule(this);
        jobHandle.Complete();
        //Debug.Log(sheet0NativeQueue.Count);

        NativeArray<SpriteSheetAnimationComponent> sheet0NativeArray = new NativeArray<SpriteSheetAnimationComponent>(sheet0NativeQueue.Count, Allocator.TempJob);

        NativeQueueToArrayJob sheet0NativeQueueToArrayJob = new NativeQueueToArrayJob {
            nativeQueue = sheet0NativeQueue,
            nativeArray = sheet0NativeArray,
        };
        jobHandle = sheet0NativeQueueToArrayJob.Schedule();
        jobHandle.Complete();

        sheet0NativeQueue.Dispose();

        SortLayerJob sortLayerJob = new SortLayerJob {
            sortArray = sheet0NativeArray
        };
        jobHandle = sortLayerJob.Schedule();
        jobHandle.Complete();

        int visibleTileTotal = sheet0NativeArray.Length;

        NativeArray<Matrix4x4> matrixArray = new NativeArray<Matrix4x4>(visibleTileTotal, Allocator.TempJob);
        NativeArray<Vector4> uvArray = new NativeArray<Vector4>(visibleTileTotal, Allocator.TempJob);

        FillArrayForParalleJob fillArrayForParalleJob = new FillArrayForParalleJob {
            matrixArray = matrixArray,
            uvArray = uvArray,
            nativeArray = sheet0NativeArray,
            startingIndex = 0
        };
        jobHandle = fillArrayForParalleJob.Schedule(sheet0NativeArray.Length, 10);
        jobHandle.Complete();

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        int sharedPropertyId = Shader.PropertyToID("_MainTex_UV");
        int sliceCount = matrixInstanceArray.Length;
        int off = 0;
        while(off < visibleTileTotal) {
            int sliceSize = math.min(visibleTileTotal - off, sliceCount);
            if(off < sheet0NativeArray.Length && off + sliceSize >= sheet0NativeArray.Length) {
                sliceSize = sheet0NativeArray.Length - off;
            }
            NativeArray<Matrix4x4>.Copy(matrixArray, off, matrixInstanceArray, 0, sliceSize);
            NativeArray<Vector4>.Copy(uvArray, off, uvInstanceArray, 0, sliceSize);

            materialPropertyBlock.SetVectorArray(sharedPropertyId, uvInstanceArray);
           
            Graphics.DrawMeshInstanced(
                Testing.Instance.mesh,
                0,
                Testing.Instance.material,
                matrixInstanceArray,
                sliceSize,
                materialPropertyBlock
            );

            off += sliceSize;
        }

        matrixArray.Dispose();
        uvArray.Dispose();
        sheet0NativeArray.Dispose();

    }
}
