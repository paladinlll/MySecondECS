using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;

public class Testing : MonoBehaviour {
    static Testing _instance;
    public static Testing Instance {
        get {
            return _instance;
        }
    }
    [SerializeField] Mesh _mesh;
    public Mesh mesh { get { return _mesh; } }

    //[SerializeField] Material _material;
    //public Material material { get { return _material; } }

    private void Awake() {
        _instance = this;
    }

    private BlobAssetReference<SpriteSheetAnimation> LoadSpriteSheetData(string sheetName) {
        XmlDTO.SpriteDTO spriteContainer = XmlDTO.SpriteDTO.Load(Resources.Load<TextAsset>($"sprites/heroes/{sheetName}").text);
        if(spriteContainer == null) {
            Debug.LogError("spriteContainer == null");
        } else {
            Debug.Log($"total animation {spriteContainer.Animations.Count}");
        }
        var builder = new BlobBuilder(Allocator.Persistent);
        ref var root = ref builder.ConstructRoot<SpriteSheetAnimation>();
        float ppu = 100;
        float texWidth = 1024;
        float texHeight = 1024;
        var modules = builder.Allocate(ref root.modules, spriteContainer.Modules.Count);
        for(int i = 0;i < spriteContainer.Modules.Count;i++) {
            var m = spriteContainer.Modules[i];
            modules[i] = new SpriteModule {
                x = m.x / texWidth,
                y = (texHeight - m.y - m.h) / texHeight,
                w = m.w / texWidth,
                h = m.h / texHeight,
            };
        }

        var animations = builder.Allocate(ref root.animations, spriteContainer.Animations.Count);
        int totalFrames = 0;
        for(int i = 0;i < spriteContainer.Animations.Count;i++) {
            var a = spriteContainer.Animations[i];
            animations[i] = new SpriteAnimation {
                startFrame = totalFrames,
                totalFrame = a.AFrames.Count
            };
            totalFrames += a.AFrames.Count;
        }

        var frames = builder.Allocate(ref root.frames, totalFrames);
        int k = 0;
        for(int i = 0;i < spriteContainer.Animations.Count;i++) {
            var a = spriteContainer.Animations[i];
            for(int j = 0;j < a.AFrames.Count;j++) {
                string fid = a.AFrames[j].fid.Substring(1);
                int iFid = int.Parse(fid, System.Globalization.NumberStyles.HexNumber);
                var f = spriteContainer.Frames[iFid].FModules[0];
                int iMid = int.Parse(f.mid, System.Globalization.NumberStyles.HexNumber);
                frames[k++] = new SpriteFrame {
                    mid = iMid,
                    ox = f.ox / texWidth,
                    oy = f.oy / texHeight
                };
            }
        }

        var clipBlob = builder.CreateBlobAssetReference<SpriteSheetAnimation>(Allocator.Persistent);
        return clipBlob;
    }

    // Start is called before the first frame update
    void Start() {
        BlobAssetReference<SpriteSheetAnimation>[] blobAssets = new BlobAssetReference<SpriteSheetAnimation>[2];
        blobAssets[0] = LoadSpriteSheetData("hero_sophie_sheet");
        blobAssets[1] = LoadSpriteSheetData("enemy_global_sheet");

        EntityManager entityManager = World.Active.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            //typeof(RenderMesh),
            typeof(SpriteSheetAnimationComponent),
            typeof(LocalToWorld),
            typeof(MoveSpeedComponent)
        );

        NativeArray<Entity> entities = new NativeArray<Entity>(2000, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entities);

        
        for(int i = 0;i < entities.Length;i++) {
            Entity entity = entities[i];
            entityManager.SetComponentData(entity,
                new MoveSpeedComponent {
                    moveSpeed = 0//UnityEngine.Random.Range(1f, 2f)
                }
            );
            entityManager.SetComponentData(entity,
                new Translation {
                    Value = new float3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-4.5f, 4.5f), 0)
                }
            );

            var sheetId = UnityEngine.Random.Range(0, 2);
            var clipBlob = blobAssets[sheetId];
            int animationId = UnityEngine.Random.Range(0, clipBlob.Value.animations.Length);
            var anim = clipBlob.Value.animations[animationId];
            entityManager.SetComponentData(entity,
                new SpriteSheetAnimationComponent {
                    sheetMaterialId = sheetId,
                    spriteSheet = clipBlob,
                    animationId = animationId,
                    currentFrame = UnityEngine.Random.Range(0, anim.totalFrame),
                    frameTimer = 0,
                    frameTimeMax = 0.1f
                }
            );
        }


        entities.Dispose();
    }

    // Update is called once per frame
    void Update() {

    }
}
