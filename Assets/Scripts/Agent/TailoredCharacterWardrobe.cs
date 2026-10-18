using UnityEngine;

/// <summary>Shared fitted clothing meshes using the character's original bind poses and skin weights.</summary>
public sealed class TailoredCharacterWardrobe : MonoBehaviour
{
    public string character;
    public Mesh maskedBody;
    public Mesh garments;
    public Material[] garmentSurfaces;
    public Material[] nativeSurfaces;

    public static bool TryApply(Transform visual,string style)
    {
        var definition=Resources.Load<GameObject>("TailoredCharacters/"+style)?.GetComponent<TailoredCharacterWardrobe>();
        if(definition==null)return false;
        var native=Resources.Load<GameObject>("ScenarioCharacters/"+definition.character)?.GetComponentInChildren<SkinnedMeshRenderer>();
        var body=visual.GetComponentInChildren<SkinnedMeshRenderer>();
        if(body==null||native==null||body.bones.Length!=native.bones.Length)return false;
        var mask=visual.GetComponent<WardrobeBodyMask>();
        if(mask==null)mask=visual.gameObject.AddComponent<WardrobeBodyMask>();
        mask.body=body;mask.original=native.sharedMesh;mask.originalMaterials=definition.nativeSurfaces;mask.style=style;
        body.sharedMesh=definition.maskedBody;body.sharedMaterials=definition.nativeSurfaces;
        var owner=new GameObject("CharacterWardrobe");owner.transform.SetParent(visual,false);
        var lifetime=owner.AddComponent<WardrobeAttachments>();
        var fitted=new GameObject(style+" fitted garments");fitted.transform.SetParent(body.transform,false);
        lifetime.parts.Add(fitted);
        var skin=fitted.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=definition.garments;
        skin.bones=body.bones;skin.rootBone=body.rootBone;skin.sharedMaterials=definition.garmentSurfaces;
        // The original renderer's bounds are expressed around this root bone.
        // Raw mesh bounds use the FBX's different axes and caused premature culling.
        Bounds bounds=body.localBounds;bounds.Expand(.45f);skin.localBounds=bounds;
        fitted.AddComponent<TailoredClothingMesh>();
        skin.updateWhenOffscreen=false;
        return true;
    }
}

