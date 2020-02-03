using System.Collections.Generic;
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
    Matrix4x4[ ] matrixInstanceArray = new Matrix4x4[1023];
    Vector4[ ] uvInstanceArray = new Vector4[1023];

    Dictionary<int, Material> materialBuffer = new Dictionary<int, Material>();
    Material defaultMaterial;
    MaterialPropertyBlock materialPropertyBlock;
    int sharedPropertyId;

    protected override void OnCreate() {
        base.OnCreate();
        defaultMaterial = new Material(Shader.Find("Custom/InstancedShader"));
        defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
        defaultMaterial.enableInstancing = true;

        materialPropertyBlock = new MaterialPropertyBlock();
        sharedPropertyId = Shader.PropertyToID("_MainTex_UV");

        SpriteSheetSharedMaterials.RegisterSpriteSheetSharedMaterialChangeEvent(OnSpriteSheetSharedMaterialChange);
    }
    protected override void OnDestroy() {
        SpriteSheetSharedMaterials.UnregisterSpriteSheetSharedMaterialChangeEvent(OnSpriteSheetSharedMaterialChange);

        materialBuffer.Clear();
        base.OnDestroy();
    }

    void OnSpriteSheetSharedMaterialChange(SpriteSheetSharedMaterialResult result) {
        if (materialBuffer.ContainsKey(result.sheetId)) {
            materialBuffer[result.sheetId] = result.spriteMat;
        }
    }

    private Material GetMaterial(int sheetId) {
        if (materialBuffer.ContainsKey(sheetId)) {
            return materialBuffer[sheetId];
        }
        materialBuffer[sheetId] = defaultMaterial;
        SpriteSheetSharedMaterials.instance.AddRequest(sheetId);
        return defaultMaterial;
    }

    [BurstCompile]
    private struct CopySpriteJob : IJobForEachWithEntity<SpriteSheetAnimationComponent> {
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet0NativeQueue;
        //public NativeQueue<int>.ParallelWriter sheetGroupNativeQueue;

        public void Execute(Entity entity, int index, ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) {
            sheet0NativeQueue.Enqueue(spriteSheetAnimationComponent);
        }
    }

    [BurstCompile]
    private struct CopySpriteMultiGroupJob : IJobForEachWithEntity<SpriteSheetAnimationComponent> {
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet0NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet1NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet2NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet3NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet4NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet5NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet6NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet7NativeQueue;
        public NativeQueue<SpriteSheetAnimationComponent>.ParallelWriter sheet8NativeQueue;


        public void Execute(Entity entity, int index, [ReadOnly] ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) {
            float yStep = (Testing.yMax - Testing.yMin) / 9.0f;
            if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep) {
                sheet0NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 2) {
                sheet1NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 3) {
                sheet2NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 4) {
                sheet3NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 5) {
                sheet4NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 6) {
                sheet5NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 7) {
                sheet6NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else if (spriteSheetAnimationComponent.sortingOrder < Testing.yMin + yStep * 8) {
                sheet7NativeQueue.Enqueue(spriteSheetAnimationComponent);
            } else {
                sheet8NativeQueue.Enqueue(spriteSheetAnimationComponent);
            }
        }
    }

    [BurstCompile]
    private struct NativeQueueToArrayJob : IJob {
        public NativeQueue<SpriteSheetAnimationComponent> nativeQueue;
        public NativeArray<SpriteSheetAnimationComponent> nativeArray;

        public void Execute() {
            int index = 0;

            SpriteSheetAnimationComponent SpriteSheetAnimationComponent;
            while (nativeQueue.TryDequeue(out SpriteSheetAnimationComponent)) {
                nativeArray[index++] = SpriteSheetAnimationComponent;
            }
        }
    }

    [BurstCompile]
    private struct FillArrayForParalleJob : IJobParallelFor {
        [ReadOnly] public NativeArray<SpriteSheetAnimationComponent> nativeArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Matrix4x4> matrixArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Vector4> uvArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<int> matIdArray;
        public int startingIndex;

        public void Execute(int index) {
            SpriteSheetAnimationComponent SpriteSheetAnimationComponent = nativeArray[index];
            matrixArray[startingIndex + index] = SpriteSheetAnimationComponent.matrix;
            uvArray[startingIndex + index] = SpriteSheetAnimationComponent.uv;
            matIdArray[startingIndex + index] = SpriteSheetAnimationComponent.sheetMaterialId;
        }
    }

    [BurstCompile]
    private struct SortLayerJob : IJob {
        public NativeArray<SpriteSheetAnimationComponent> sortArray;

        public void Execute() {
            for (int i = 0;i < sortArray.Length - 1;i++) {
                for (int j = sortArray.Length - 1;j > i;j--) {
                    if ((sortArray[i].sortingOrder)> ( sortArray[j].sortingOrder)) {
                        SpriteSheetAnimationComponent tmp = sortArray[i];
                        sortArray[i] = sortArray[j];
                        sortArray[j] = tmp;
                    }
                }
            }
        }
    }

    protected override void OnUpdate() {
        
        //Debug.Log(sheet0NativeQueue.Count);
        bool useQuadrantSystem = Testing.Instance.useQuadrantSystem;
        bool sortSprite = Testing.Instance.sortSprite;
        if (useQuadrantSystem) {
            #region useQuadrantSystem
            NativeQueue<SpriteSheetAnimationComponent> node0NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node1NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node2NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node3NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node4NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node5NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node6NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node7NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            NativeQueue<SpriteSheetAnimationComponent> node8NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);

            CopySpriteMultiGroupJob copyTileMultiJob = new CopySpriteMultiGroupJob {
                sheet0NativeQueue = node0NativeQueue.AsParallelWriter(),
                sheet1NativeQueue = node1NativeQueue.AsParallelWriter(),
                sheet2NativeQueue = node2NativeQueue.AsParallelWriter(),
                sheet3NativeQueue = node3NativeQueue.AsParallelWriter(),
                sheet4NativeQueue = node4NativeQueue.AsParallelWriter(),
                sheet5NativeQueue = node5NativeQueue.AsParallelWriter(),
                sheet6NativeQueue = node6NativeQueue.AsParallelWriter(),
                sheet7NativeQueue = node7NativeQueue.AsParallelWriter(),
                sheet8NativeQueue = node8NativeQueue.AsParallelWriter()
            };
            JobHandle jobHandle = copyTileMultiJob.Schedule(this);
            jobHandle.Complete();

            NativeArray<SpriteSheetAnimationComponent> node0NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node0NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node1NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node1NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node2NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node2NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node3NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node3NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node4NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node4NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node5NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node5NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node6NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node6NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node7NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node7NativeQueue.Count, Allocator.TempJob);
            NativeArray<SpriteSheetAnimationComponent> node8NativeArray = new NativeArray<SpriteSheetAnimationComponent>(node8NativeQueue.Count, Allocator.TempJob);

            NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(9, Allocator.TempJob);

            NativeQueueToArrayJob node0NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node0NativeQueue,
                nativeArray = node0NativeArray,
            };
            jobHandleArray[0] = node0NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node1NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node1NativeQueue,
                nativeArray = node1NativeArray,
            };
            jobHandleArray[1] = node1NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node2NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node2NativeQueue,
                nativeArray = node2NativeArray,
            };
            jobHandleArray[2] = node2NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node3NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node3NativeQueue,
                nativeArray = node3NativeArray,
            };
            jobHandleArray[3] = node3NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node4NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node4NativeQueue,
                nativeArray = node4NativeArray,
            };
            jobHandleArray[4] = node4NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node5NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node5NativeQueue,
                nativeArray = node5NativeArray,
            };
            jobHandleArray[5] = node5NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node6NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node6NativeQueue,
                nativeArray = node6NativeArray,
            };
            jobHandleArray[6] = node6NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node7NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node7NativeQueue,
                nativeArray = node7NativeArray,
            };
            jobHandleArray[7] = node7NativeQueueToArrayJob.Schedule();

            NativeQueueToArrayJob node8NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = node8NativeQueue,
                nativeArray = node8NativeArray,
            };
            jobHandleArray[8] = node8NativeQueueToArrayJob.Schedule();

            JobHandle.CompleteAll(jobHandleArray);

            node0NativeQueue.Dispose();
            node1NativeQueue.Dispose();
            node2NativeQueue.Dispose();
            node3NativeQueue.Dispose();
            node4NativeQueue.Dispose();
            node5NativeQueue.Dispose();
            node6NativeQueue.Dispose();
            node7NativeQueue.Dispose();
            node8NativeQueue.Dispose();

            if (sortSprite) {
                SortLayerJob sortLayer0Job = new SortLayerJob {
                    sortArray = node0NativeArray
                };
                jobHandleArray[0] = sortLayer0Job.Schedule();

                SortLayerJob sortLayer1Job = new SortLayerJob {
                    sortArray = node1NativeArray
                };
                jobHandleArray[1] = sortLayer1Job.Schedule();

                SortLayerJob sortLayer2Job = new SortLayerJob {
                    sortArray = node2NativeArray
                };
                jobHandleArray[2] = sortLayer2Job.Schedule();

                SortLayerJob sortLayer3Job = new SortLayerJob {
                    sortArray = node3NativeArray
                };
                jobHandleArray[3] = sortLayer3Job.Schedule();

                SortLayerJob sortLayer4Job = new SortLayerJob {
                    sortArray = node4NativeArray
                };
                jobHandleArray[4] = sortLayer4Job.Schedule();

                SortLayerJob sortLayer5Job = new SortLayerJob {
                    sortArray = node5NativeArray
                };
                jobHandleArray[5] = sortLayer5Job.Schedule();

                SortLayerJob sortLayer6Job = new SortLayerJob {
                    sortArray = node6NativeArray
                };
                jobHandleArray[6] = sortLayer6Job.Schedule();

                SortLayerJob sortLayer7Job = new SortLayerJob {
                    sortArray = node7NativeArray
                };
                jobHandleArray[7] = sortLayer7Job.Schedule();

                SortLayerJob sortLayer8Job = new SortLayerJob {
                    sortArray = node8NativeArray
                };
                jobHandleArray[8] = sortLayer8Job.Schedule();

                JobHandle.CompleteAll(jobHandleArray);
            }
            int visibleTileTotal = node0NativeArray.Length +
                node1NativeArray.Length +
                node2NativeArray.Length +
                node3NativeArray.Length +
                node4NativeArray.Length +
                node5NativeArray.Length +
                node6NativeArray.Length +
                node7NativeArray.Length +
                node8NativeArray.Length
            ;

            NativeArray<Matrix4x4> matrixArray = new NativeArray<Matrix4x4>(visibleTileTotal, Allocator.TempJob);
            NativeArray<Vector4> uvArray = new NativeArray<Vector4>(visibleTileTotal, Allocator.TempJob);
            NativeArray<int> matIdArray = new NativeArray<int>(visibleTileTotal, Allocator.TempJob);

            int startingIndex = 0;
            FillArrayForParalleJob fillArrayForParalleJob_0 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node0NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[0] = fillArrayForParalleJob_0.Schedule(node0NativeArray.Length, 10);
            startingIndex += node0NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_1 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node1NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[1] = fillArrayForParalleJob_1.Schedule(node1NativeArray.Length, 10);
            startingIndex += node1NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_2 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node2NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[2] = fillArrayForParalleJob_2.Schedule(node2NativeArray.Length, 10);
            startingIndex += node2NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_3 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node3NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[3] = fillArrayForParalleJob_3.Schedule(node3NativeArray.Length, 10);
            startingIndex += node3NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_4 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node4NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[4] = fillArrayForParalleJob_4.Schedule(node4NativeArray.Length, 10);
            startingIndex += node4NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_5 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node5NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[5] = fillArrayForParalleJob_5.Schedule(node5NativeArray.Length, 10);
            startingIndex += node5NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_6 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node6NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[6] = fillArrayForParalleJob_6.Schedule(node6NativeArray.Length, 10);
            startingIndex += node6NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_7 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node7NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[7] = fillArrayForParalleJob_7.Schedule(node7NativeArray.Length, 10);
            startingIndex += node7NativeArray.Length;

            FillArrayForParalleJob fillArrayForParalleJob_8 = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = node8NativeArray,
                startingIndex = startingIndex
            };
            jobHandleArray[8] = fillArrayForParalleJob_8.Schedule(node8NativeArray.Length, 10);
            startingIndex += node8NativeArray.Length;

            JobHandle.CompleteAll(jobHandleArray);

            int sliceCount = matrixInstanceArray.Length;
            int off = 0;
            while (off < visibleTileTotal) {
                int sheetId = matIdArray[off];
                int sliceSize = 0;
                while (off + sliceSize < visibleTileTotal && sliceSize < sliceCount) {
                    if (matIdArray[off + sliceSize] != sheetId) {
                        break;
                    }
                    sliceSize++;
                }
               
                NativeArray<Matrix4x4>.Copy(matrixArray, off, matrixInstanceArray, 0, sliceSize);
                NativeArray<Vector4>.Copy(uvArray, off, uvInstanceArray, 0, sliceSize);

                materialPropertyBlock.SetVectorArray(sharedPropertyId, uvInstanceArray);

                Graphics.DrawMeshInstanced(
                    Testing.Instance.mesh,
                    0,
                    GetMaterial(sheetId),
                    matrixInstanceArray,
                    sliceSize,
                    materialPropertyBlock
                );

                off += sliceSize;
            }

            matrixArray.Dispose();
            uvArray.Dispose();
            matIdArray.Dispose();

            node0NativeArray.Dispose();
            node1NativeArray.Dispose();
            node2NativeArray.Dispose();
            node3NativeArray.Dispose();
            node4NativeArray.Dispose();
            node5NativeArray.Dispose();
            node6NativeArray.Dispose();
            node7NativeArray.Dispose();
            node8NativeArray.Dispose();
            jobHandleArray.Dispose();
            #endregion
        } else {
            NativeQueue<SpriteSheetAnimationComponent> sheet0NativeQueue = new NativeQueue<SpriteSheetAnimationComponent>(Allocator.TempJob);
            CopySpriteJob copyTileJob = new CopySpriteJob {
                sheet0NativeQueue = sheet0NativeQueue.AsParallelWriter()
            };
            JobHandle jobHandle = copyTileJob.Schedule(this);
            jobHandle.Complete();

            NativeArray<SpriteSheetAnimationComponent> sheet0NativeArray = new NativeArray<SpriteSheetAnimationComponent>(sheet0NativeQueue.Count, Allocator.TempJob);

            NativeQueueToArrayJob sheet0NativeQueueToArrayJob = new NativeQueueToArrayJob {
                nativeQueue = sheet0NativeQueue,
                nativeArray = sheet0NativeArray,
            };
            jobHandle = sheet0NativeQueueToArrayJob.Schedule();
            jobHandle.Complete();

            sheet0NativeQueue.Dispose();

            if (sortSprite) {
                SortLayerJob sortLayerJob = new SortLayerJob {
                    sortArray = sheet0NativeArray
                };
                jobHandle = sortLayerJob.Schedule();
                jobHandle.Complete();
            }

            int visibleTileTotal = sheet0NativeArray.Length;

            NativeArray<Matrix4x4> matrixArray = new NativeArray<Matrix4x4>(visibleTileTotal, Allocator.TempJob);
            NativeArray<Vector4> uvArray = new NativeArray<Vector4>(visibleTileTotal, Allocator.TempJob);
            NativeArray<int> matIdArray = new NativeArray<int>(visibleTileTotal, Allocator.TempJob);
            FillArrayForParalleJob fillArrayForParalleJob = new FillArrayForParalleJob {
                matrixArray = matrixArray,
                uvArray = uvArray,
                matIdArray = matIdArray,
                nativeArray = sheet0NativeArray,
                startingIndex = 0
            };
            jobHandle = fillArrayForParalleJob.Schedule(sheet0NativeArray.Length, 10);
            jobHandle.Complete();


            int sliceCount = matrixInstanceArray.Length;
            int off = 0;
            while (off < visibleTileTotal) {
                int sheetId = matIdArray[off];
                int sliceSize = 0;
                while (off + sliceSize < visibleTileTotal && sliceSize < sliceCount) {
                    if (matIdArray[off + sliceSize] != sheetId) {
                        break;
                    }
                    sliceSize++;
                }
                NativeArray<Matrix4x4>.Copy(matrixArray, off, matrixInstanceArray, 0, sliceSize);
                NativeArray<Vector4>.Copy(uvArray, off, uvInstanceArray, 0, sliceSize);

                materialPropertyBlock.SetVectorArray(sharedPropertyId, uvInstanceArray);

                Graphics.DrawMeshInstanced(
                    Testing.Instance.mesh,
                    0,
                    GetMaterial(sheetId),
                    matrixInstanceArray,
                    sliceSize,
                    materialPropertyBlock
                );

                off += sliceSize;
            }

            matrixArray.Dispose();
            uvArray.Dispose();
            matIdArray.Dispose();

            sheet0NativeArray.Dispose();
        }

        
    }
}
