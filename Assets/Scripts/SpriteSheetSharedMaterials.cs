using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

public class SpriteSheetSharedMaterialResult {
    public int sheetId;
    public int errorCode;
    public Material spriteMat;
}
public class SpriteSheetSharedMaterials : MonoBehaviour {
    private static SpriteSheetSharedMaterials _instance;
    public static SpriteSheetSharedMaterials instance {
        get {
            if(!_instance) {
                _instance = FindObjectOfType<SpriteSheetSharedMaterials>();
            }
            if(!_instance) {
                GameObject go = new GameObject("SpriteSheetSharedMaterials");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SpriteSheetSharedMaterials>();
            }
            return _instance;
        }
    }

    private class RequestInfo {
        public long requestTimeMS;
        public SpriteSheetSharedMaterialResult result;
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<Texture2D> handle;
    }

    Dictionary<int, RequestInfo> cachSprites = new Dictionary<int, RequestInfo>();

    public delegate void SpriteSheetSharedMaterialCall(SpriteSheetSharedMaterialResult result);
    public static event SpriteSheetSharedMaterialCall eventDataStoreChange;

    public static void RegisterSpriteSheetSharedMaterialChangeEvent(SpriteSheetSharedMaterialCall callback) {
        eventDataStoreChange += callback;
    }

    public static void UnregisterSpriteSheetSharedMaterialChangeEvent(SpriteSheetSharedMaterialCall callback) {
        eventDataStoreChange -= callback;
    }

    public void AddRequest(int sheetId) {
        if(cachSprites.ContainsKey(sheetId)) {
            var requestInfo = cachSprites[sheetId];
            //if(requestInfo.spr != null) {
            //    onComplete?.Invoke(requestInfo.spr);
            //} else {
            //    long dtTimeMs = Axie.Utility.Utils.ToUnixTimeMS(System.DateTime.UtcNow) - requestInfo.requestTimeMS;
            //    if(dtTimeMs < 10000) //10s
            //    {
            //        requestInfo.onComplete += onComplete;
            //    } else {
            //        Main.LogWarning($"Request Sprite [{id}] timeout!!!");
            //    }
            //}
        } else {
            RequestInfo newRequest = new RequestInfo {
                result = new SpriteSheetSharedMaterialResult {
                    sheetId = sheetId,
                    errorCode = 0,
                    spriteMat = null
                }
            };
            cachSprites.Add(sheetId, newRequest);
            newRequest.handle = Addressables.LoadAssetAsync<Texture2D>($"sheet_{sheetId}");
            newRequest.handle.Completed += (tex) => {
                OnTextureLoaded(sheetId, tex.Result);
            };
        }
    }

    private void OnTextureLoaded(int sheetId, Texture2D tex) {
        if(!cachSprites.ContainsKey(sheetId)) return;
        RequestInfo requestInfo = cachSprites[sheetId];

        requestInfo.result.spriteMat = new Material(Shader.Find("Custom/InstancedShader"));
        requestInfo.result.spriteMat.hideFlags = HideFlags.HideAndDontSave;
        requestInfo.result.spriteMat.SetTexture("_MainTex", tex);
        requestInfo.result.spriteMat.enableInstancing = true;

        if(eventDataStoreChange != null) {
            eventDataStoreChange.Invoke(requestInfo.result);
        }
    }
}
