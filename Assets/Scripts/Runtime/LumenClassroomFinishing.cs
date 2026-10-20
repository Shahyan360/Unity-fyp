using System.Linq;
using UnityEngine;

/// <summary>Finishes the authored room without moving its desks or changing walkable space.</summary>
public static class LumenClassroomFinishing
{
    public static void Apply()
    {
        var ceiling=GameObject.Find("Ceiling");if(ceiling==null||ceiling.GetComponent<LumenRuntimeResources>()!=null)return;
        ceiling.AddComponent<LumenRuntimeResources>();
        var plaster=Mat("Warm acoustic ceiling",new Color(.65f,.64f,.59f));var timber=Mat("Muted ceiling battens",new Color(.39f,.35f,.29f));
        var slats=ceiling.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Slat").OrderBy(r=>r.transform.position.x).ToArray();
        for(int i=0;i<slats.Length;i++){slats[i].enabled=i%3==0;slats[i].sharedMaterial=timber;}
        foreach(var renderer in ceiling.GetComponentsInChildren<MeshRenderer>())if(renderer.name=="CeilingBase")renderer.sharedMaterial=plaster;
        Color[] colors={new Color(.32f,.39f,.39f),new Color(.43f,.26f,.18f),new Color(.38f,.40f,.27f),new Color(.48f,.39f,.24f)};
        var covers=colors.Select((color,i)=>Mat("Classroom book cloth "+i,color)).ToArray();
        var frames=Mat("Classroom muted teal frames",new Color(.07f,.16f,.17f));var tops=Mat("Classroom matte worktops",new Color(.48f,.46f,.41f));
        int book=0;
        foreach(var renderer in Object.FindObjectsByType<MeshRenderer>())
        {
            if(renderer.name=="Book"||renderer.name.StartsWith("Book_On_Desk")){renderer.sharedMaterial=covers[book++%covers.Length];continue;}
            var materials=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                string name=materials[i]!=null?materials[i].name:"";
                if(name=="Style_Table"||(name.Contains("Desk")&&name.Contains("Blue"))){materials[i]=frames;changed=true;}
                else if(name.Contains("Desk")&&(name.Contains("Top")||name.Contains("Surface"))){materials[i]=tops;changed=true;}
            }
            if(changed)renderer.sharedMaterials=materials;
        }
        var dust=GameObject.Find("Dust");if(dust!=null)foreach(var particles in dust.GetComponentsInChildren<ParticleSystem>())particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private static Material Mat(string name,Color color)
    {
        var material=LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color});material.SetFloat("_Smoothness",.18f);return material;
    }
}
