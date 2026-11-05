using UnityEngine;
using UnityEngine.UI;

/// <summary>Original schematic geography: one cached UI mesh, rebuilt only when its rectangle changes.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LumenAtlasTerrain : Graphic
{
    private VertexHelper mesh;
    private Rect area;
    private static readonly Vector2[] Centers = { new Vector2(.23f,.34f), new Vector2(.53f,.71f), new Vector2(.80f,.33f) };
    private static readonly Color[] Land = { new Color(.35f,.62f,.43f),new Color(.34f,.57f,.72f),new Color(.78f,.56f,.32f) };
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        mesh=helper;mesh.Clear();area=rectTransform.rect;
        // Quiet cartographic grid and sailing routes, in normalized coordinates.
        for(float x=.04f;x<1;x+=.08f)Line(new Vector2(x,.02f),new Vector2(x,.98f),new Color(.5f,.8f,.85f,.07f),.7f);
        for(float y=.04f;y<1;y+=.1f)Line(new Vector2(.02f,y),new Vector2(.98f,y),new Color(.5f,.8f,.85f,.07f),.7f);
        for(int a=0;a<2;a++)for(int i=0;i<22;i+=2)
        {
            Vector2 from=Vector2.Lerp(Centers[a],Centers[a+1],i/22f);
            Vector2 to=Vector2.Lerp(Centers[a],Centers[a+1],(i+1)/22f);
            Line(from,to,new Color(.77f,.87f,.80f,.45f),1.5f);
        }
        for(int island=0;island<3;island++)
        {
            Coast(island,1.16f,new Color(.24f,.52f,.56f,.28f),new Vector2(0,-.008f));
            Coast(island,1.07f,new Color(.80f,.77f,.58f),Vector2.zero);
            Coast(island,1f,Land[island],Vector2.zero);
            Coast(island,.72f,Color.Lerp(Land[island],new Color(.77f,.84f,.56f),.22f),new Vector2(-.01f,.01f));
            // A river meanders through each region toward its coast.
            Vector2 last=Centers[island]+new Vector2(-.09f,.13f);
            for(int segment=1;segment<=12;segment++)
            {
                float t=segment/12f;
                Vector2 next=Centers[island]+new Vector2(-.09f+t*.14f+Mathf.Sin(t*9)*.012f,.13f-t*.25f);
                Line(last,next,new Color(.38f,.71f,.79f),3f);last=next;
            }
            for(int i=0;i<8;i++)
            {
                Vector2 p=Centers[island]+new Vector2(Mathf.Cos(i*2.4f)*.115f,Mathf.Sin(i*2.4f)*.12f);
                if(island==2)Mountain(p,.025f,new Color(.60f,.36f,.22f));
                else Tree(p,island==0?new Color(.13f,.37f,.28f):new Color(.19f,.40f,.48f));
            }
            Vector2 town=Centers[island]+new Vector2(.04f,-.12f);
            for(int i=0;i<4;i++)
            {
                Vector2 p=town+new Vector2((i%2)*.028f,(i/2)*.038f);
                Quad(p,new Vector2(.019f,.027f),new Color(.94f,.88f,.68f));
                Triangle(p+new Vector2(-.004f,.027f),p+new Vector2(.023f,.027f),p+new Vector2(.009f,.046f),new Color(.58f,.32f,.24f));
            }
        }
        mesh=null;
    }
    private Vector2 Point(Vector2 p)=>area.min+Vector2.Scale(p,area.size);
    private void Coast(int island,float scale,Color tint,Vector2 offset)
    {
        int start=mesh.currentVertCount;
        Vector2 center=Centers[island]+offset;
        mesh.AddVert(Point(center),tint,Vector2.zero);
        const int count=60;
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            float r=1+.11f*Mathf.Sin(angle*5+island)+.07f*Mathf.Cos(angle*9-island)+.05f*Mathf.Sin(angle*13);
            Vector2 p=center+new Vector2(Mathf.Cos(angle)*.168f,Mathf.Sin(angle)*.215f)*r*scale;
            mesh.AddVert(Point(p),tint,Vector2.zero);
        }
        for(int i=0;i<count;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
    }
    private void Triangle(Vector2 a,Vector2 b,Vector2 c,Color tint)
    {
        int n=mesh.currentVertCount;mesh.AddVert(Point(a),tint,Vector2.zero);mesh.AddVert(Point(b),tint,Vector2.zero);mesh.AddVert(Point(c),tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);
    }
    private void Quad(Vector2 p,Vector2 size,Color tint)
    {
        Triangle(p,p+new Vector2(size.x,0),p+size,tint);Triangle(p,p+size,p+new Vector2(0,size.y),tint);
    }
    private void Line(Vector2 a,Vector2 b,Color tint,float width)
    {
        Vector2 start=Point(a),end=Point(b);Vector2 delta=end-start;
        Vector2 normal=new Vector2(-delta.y,delta.x).normalized*width*.5f;
        int n=mesh.currentVertCount;
        mesh.AddVert(start-normal,tint,Vector2.zero);mesh.AddVert(start+normal,tint,Vector2.zero);
        mesh.AddVert(end+normal,tint,Vector2.zero);mesh.AddVert(end-normal,tint,Vector2.zero);
        mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
    }
    private void Tree(Vector2 p,Color tint)
    {
        Quad(p,new Vector2(.003f,.025f),new Color(.35f,.28f,.21f));
        Triangle(p+new Vector2(-.014f,.012f),p+new Vector2(.017f,.012f),p+new Vector2(.0015f,.051f),tint);
        Triangle(p+new Vector2(-.011f,.027f),p+new Vector2(.014f,.027f),p+new Vector2(.0015f,.062f),tint);
    }
    private void Mountain(Vector2 p,float size,Color tint)
    {
        Triangle(p,p+new Vector2(size*1.6f,0),p+new Vector2(size*.7f,size*1.7f),tint);
        Triangle(p+new Vector2(size*.7f,size*1.7f),p+new Vector2(size*1.6f,0),p+new Vector2(size*.8f,0),Color.Lerp(tint,Color.black,.18f));
        Triangle(p+new Vector2(size*.43f,size*1.05f),p+new Vector2(size*.7f,size*1.7f),p+new Vector2(size*1.05f,size*1.05f),new Color(.96f,.89f,.73f));
    }
}
