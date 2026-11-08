using System.Collections.Generic;
using UnityEngine;

/// <summary>Own only explicitly registered runtime creations; imported assets are never registered.</summary>
public sealed class LumenRuntimeResources:MonoBehaviour
{
    private readonly HashSet<Object> owned=new HashSet<Object>();
    public static T Own<T>(T resource,GameObject owner=null) where T:Object
    {
        if(resource==null)return null;
        if(owner==null)owner=GameObject.Find("Lumen scene resources")??new GameObject("Lumen scene resources");
        var lifetime=owner.GetComponent<LumenRuntimeResources>()??owner.AddComponent<LumenRuntimeResources>();
        lifetime.owned.Add(resource);return resource;
    }
    private void OnDestroy(){foreach(var resource in owned)if(resource!=null)Destroy(resource);owned.Clear();}
}
