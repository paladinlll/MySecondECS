using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;
using CodeMonkey.MonoBehaviours;

public struct Unit : IComponentData { }
public struct Target : IComponentData { }

public struct HasTarget : IComponentData {
    public Entity targetEntity;
}

public class HasTargetDebug : ComponentSystem {

    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) => {
            if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(hasTarget.targetEntity)) {
                Translation targetTranslation = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);
                Debug.DrawLine(translation.Value, targetTranslation.Value);
            }
        });
    }

}

public class MyCustomBootstrap : ICustomBootstrap {
    public bool Initialize(string defaultWorldName) {
        World.DefaultGameObjectInjectionWorld = new World(defaultWorldName);
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World.DefaultGameObjectInjectionWorld, systems);
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.DefaultGameObjectInjectionWorld);

        LoadWorld();
        return true;
    }

    private BlobAssetReference<SpriteSheetAnimation> LoadSpriteSheetData(string sheetName) {
        XmlDTO.SpriteDTO spriteContainer = XmlDTO.SpriteDTO.Load(Resources.Load<TextAsset>($"sprites/heroes/{sheetName}").text);
        if (spriteContainer == null) {
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
        for (int i = 0;i < spriteContainer.Modules.Count;i++) {
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
        for (int i = 0;i < spriteContainer.Animations.Count;i++) {
            var a = spriteContainer.Animations[i];
            animations[i] = new SpriteAnimation {
                startFrame = totalFrames,
                totalFrame = a.AFrames.Count
            };
            totalFrames += a.AFrames.Count;
        }

        var frames = builder.Allocate(ref root.frames, totalFrames);
        int k = 0;
        for (int i = 0;i < spriteContainer.Animations.Count;i++) {
            var a = spriteContainer.Animations[i];
            for (int j = 0;j < a.AFrames.Count;j++) {
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

    void LoadWorld() {
        BlobAssetReference<SpriteSheetAnimation>[ ] blobAssets = new BlobAssetReference<SpriteSheetAnimation>[2];
        blobAssets[0] = LoadSpriteSheetData("hero_sophie_sheet");
        blobAssets[1] = LoadSpriteSheetData("enemy_global_sheet");

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        {
            EntityArchetype unitArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(SpriteSheetAnimationComponent),
                typeof(LocalToWorld),
                typeof(MoveSpeedComponent),
                typeof(Unit)
            );

            NativeArray<Entity> entities = new NativeArray<Entity>(500, Allocator.Temp);
            entityManager.CreateEntity(unitArchetype, entities);

            for (int i = 0;i < entities.Length;i++) {
                Entity entity = entities[i];
                entityManager.SetComponentData(entity,
                    new MoveSpeedComponent {
                        moveSpeed = 0//UnityEngine.Random.Range(1f, 2f)
                    }
                );
                entityManager.SetComponentData(entity,
                    new Translation {
                        Value = new float3(UnityEngine.Random.Range(-8f, 8f) * 5, UnityEngine.Random.Range(Testing.yMin, Testing.yMax) * 5, 0)
                    }
                );

                var sheetId = 0;
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

        {
            EntityArchetype targetArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(SpriteSheetAnimationComponent),
                typeof(LocalToWorld),
                typeof(MoveSpeedComponent),
                typeof(Target)
            );
            NativeArray<Entity> entities = new NativeArray<Entity>(3000, Allocator.Temp);
            entityManager.CreateEntity(targetArchetype, entities);

            for (int i = 0;i < entities.Length;i++) {
                Entity entity = entities[i];
                entityManager.SetComponentData(entity,
                    new MoveSpeedComponent {
                        moveSpeed = 0//UnityEngine.Random.Range(1f, 2f)
                    }
                );
                entityManager.SetComponentData(entity,
                    new Translation {
                        Value = new float3(UnityEngine.Random.Range(-8f, 8f) * 5, UnityEngine.Random.Range(Testing.yMin, Testing.yMax) * 5, 0)
                    }
                );

                var sheetId = 1;
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
    }
}
public class Testing : MonoBehaviour {
    public const float yMin = -4.5f;
    public const float yMax = 4.5f;
    static Testing _instance;
    public static Testing Instance {
        get {
            return _instance;
        }
    }
    [SerializeField] Mesh _mesh;
    public Mesh mesh { get { return _mesh; } }

    [SerializeField] private CameraFollow cameraFollow;

    [SerializeField] private bool _useQuadrantSystem;
    public bool useQuadrantSystem { get { return _useQuadrantSystem; } }

    [SerializeField] private bool _sortSprite;
    public bool sortSprite { get { return _sortSprite; } }

    private Vector3 cameraFollowPosition;
    private float cameraFollowZoom;
    //[SerializeField] Material _material;
    //public Material material { get { return _material; } }

    private void Awake() {
        _instance = this;

    }

    private void Update() {
        HandleCamera();
    }
    
    private void HandleCamera() {
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) { moveDir.y = +1f; }
        if (Input.GetKey(KeyCode.S)) { moveDir.y = -1f; }
        if (Input.GetKey(KeyCode.A)) { moveDir.x = -1f; }
        if (Input.GetKey(KeyCode.D)) { moveDir.x = +1f; }

        moveDir = moveDir.normalized;
        float cameraMoveSpeed = 50f;
        cameraFollowPosition += moveDir * cameraMoveSpeed * Time.deltaTime;

        float zoomSpeed = 200f;
        if (Input.mouseScrollDelta.y > 0) cameraFollowZoom -= 1 * zoomSpeed * Time.deltaTime;
        if (Input.mouseScrollDelta.y < 0) cameraFollowZoom += 1 * zoomSpeed * Time.deltaTime;

        cameraFollowZoom = Mathf.Clamp(cameraFollowZoom, 4f, 40f);
    }

    // Start is called before the first frame update
    void Start() {
        cameraFollowZoom = 15f;
        cameraFollow.Setup(() => cameraFollowPosition, () => cameraFollowZoom, true, true);
        
    }
}
