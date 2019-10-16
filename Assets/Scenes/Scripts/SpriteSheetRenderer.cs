using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class SpriteSheetRenderer : ComponentSystem {
    protected override void OnUpdate() {
        var camera = Camera.main;
        Entities.ForEach((ref Translation translation, ref SpriteSheetAnimationComponent spriteSheetAnimationComponent) => {
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetVector("_MainTex_ST", spriteSheetAnimationComponent.uv);
            Graphics.DrawMesh(
                Testing.Instance.mesh,
                spriteSheetAnimationComponent.matrix,
                Testing.Instance.material,
                0,
                camera,
                0,
                materialPropertyBlock
            );

        });
    }
}
