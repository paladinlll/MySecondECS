using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct SpriteModule {
    public float x;
    public float y;
    public float w;
    public float h;
}

[Serializable]
public struct SpriteFrame {
    public int mid;
    public float ox;
    public float oy;
}

[Serializable]
public struct SpriteAnimation {
    public int startFrame;
    public int totalFrame;
}

[Serializable]
public struct SpriteSheetAnimation  {
    public BlobArray<SpriteModule> modules;
    public BlobArray<SpriteFrame> frames;
    public BlobArray<SpriteAnimation> animations;

}

[Serializable]
public struct SpriteSheetAnimationComponent : IComponentData
{
    public int animationId;
    public BlobAssetReference<SpriteSheetAnimation> spriteSheet;
    public int currentFrame;
    //public int frameCount;
    public float frameTimer;
    public float frameTimeMax;

    public float sortingOrder;
    public Vector4 uv;
    public Matrix4x4 matrix;
}
