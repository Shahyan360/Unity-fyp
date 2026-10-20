using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Removes only vertices unreferenced by every visible submesh of a wardrobe body mask.</summary>
public static class LumenMeshCompaction
{
    public static Mesh Compact(Mesh source,out string evidence)
    {
        var used=new SortedSet<int>();
        for(int sub=0;sub<source.subMeshCount;sub++)foreach(int vertex in source.GetTriangles(sub))used.Add(vertex);
        int[] indices=used.ToArray();
        var remap=new int[source.vertexCount];for(int i=0;i<indices.Length;i++)remap[indices[i]]=i;
        var mesh=new Mesh{name=source.name+" visible vertices",indexFormat=indices.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
        Vector3[] positions=source.vertices;mesh.vertices=indices.Select(i=>positions[i]).ToArray();
        Vector3[] normals=source.normals;if(normals.Length==source.vertexCount)mesh.normals=indices.Select(i=>normals[i]).ToArray();
        Vector4[] tangents=source.tangents;if(tangents.Length==source.vertexCount)mesh.tangents=indices.Select(i=>tangents[i]).ToArray();
        for(int channel=0;channel<8;channel++)
        {
            var uv=new List<Vector4>();source.GetUVs(channel,uv);
            if(uv.Count!=source.vertexCount)continue;
            int dimension=source.GetVertexAttributeDimension((UnityEngine.Rendering.VertexAttribute)((int)UnityEngine.Rendering.VertexAttribute.TexCoord0+channel));
            if(dimension==2)mesh.SetUVs(channel,indices.Select(i=>new Vector2(uv[i].x,uv[i].y)).ToList());
            else if(dimension==3)mesh.SetUVs(channel,indices.Select(i=>new Vector3(uv[i].x,uv[i].y,uv[i].z)).ToList());
            else mesh.SetUVs(channel,indices.Select(i=>uv[i]).ToList());
        }
        Color32[] colors=source.colors32;if(colors.Length==source.vertexCount)mesh.colors32=indices.Select(i=>colors[i]).ToArray();
        BoneWeight[] weights=source.boneWeights;if(weights.Length==source.vertexCount)mesh.boneWeights=indices.Select(i=>weights[i]).ToArray();
        mesh.bindposes=source.bindposes;
        mesh.subMeshCount=source.subMeshCount;
        for(int sub=0;sub<source.subMeshCount;sub++)mesh.SetTriangles(source.GetTriangles(sub).Select(i=>remap[i]).ToArray(),sub);
        var delta=new Vector3[source.vertexCount];var normalDelta=new Vector3[source.vertexCount];var tangentDelta=new Vector3[source.vertexCount];
        int frames=0;
        for(int shape=0;shape<source.blendShapeCount;shape++)
            for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                source.GetBlendShapeFrameVertices(shape,frame,delta,normalDelta,tangentDelta);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),
                    indices.Select(i=>delta[i]).ToArray(),indices.Select(i=>normalDelta[i]).ToArray(),indices.Select(i=>tangentDelta[i]).ToArray());
                frames++;
            }
        mesh.bounds=source.bounds;
        // Exact comparisons, including every retained facial delta, before accepting the replacement.
        var check=new Vector3[indices.Length];var checkNormal=new Vector3[indices.Length];var checkTangent=new Vector3[indices.Length];
        float maxError=0;
        for(int shape=0;shape<source.blendShapeCount;shape++)
            for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                source.GetBlendShapeFrameVertices(shape,frame,delta,normalDelta,tangentDelta);
                mesh.GetBlendShapeFrameVertices(shape,frame,check,checkNormal,checkTangent);
                for(int i=0;i<indices.Length;i++)maxError=Mathf.Max(maxError,Mathf.Max((delta[indices[i]]-check[i]).magnitude,Mathf.Max((normalDelta[indices[i]]-checkNormal[i]).magnitude,(tangentDelta[indices[i]]-checkTangent[i]).magnitude)));
            }
        if(maxError!=0||mesh.blendShapeCount!=source.blendShapeCount)throw new InvalidOperationException("Facial mesh compaction changed a retained expression.");
        evidence=source.name+" vertices "+source.vertexCount+" -> "+mesh.vertexCount+"; retained shapes="+mesh.blendShapeCount+"; retained frames="+frames+"; exact retained-delta maximum error="+maxError+". Removed vertices were referenced by zero visible triangles. This is not a VRAM measurement.";
        return mesh;
    }
}
