using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Additive full-sized street details; retains the authored Roman city.</summary>
public sealed class RomanCityVisualUpgrade : MonoBehaviour
{
    public static void Install()
    {
        if(GameObject.Find("RomanCityVisualUpgrade")!=null)return;
        new GameObject("RomanCityVisualUpgrade").AddComponent<RomanCityVisualUpgrade>();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        BuildCityBlocks();
        RestoreTemples();
        AddStreetGreenery();
        RomanArenaPerformance.Install();
        gameObject.AddComponent<RomanVisitorAccess>();
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.60f,.67f,.73f);
        RenderSettings.ambientEquatorColor=new Color(.56f,.49f,.39f);
        RenderSettings.ambientGroundColor=new Color(.28f,.25f,.20f);
        foreach(Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            if(light.type==LightType.Directional)
            {
                light.color=new Color(1f,.92f,.79f);light.intensity=1.2f;
                light.shadows=LightShadows.Soft;light.shadowStrength=.55f;
                light.transform.rotation=Quaternion.Euler(42f,-30f,0f);
            }
        GameObject arcade=Resources.Load<GameObject>("LumenCollection/RomanArcadeRestoration");
        if(arcade!=null)
        {
            Instantiate(arcade,Vector3.zero,Quaternion.identity,transform).name="Full-sized amphitheatre stone arcades";
            foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                if(renderer.name.StartsWith("Arcade_Shadow_"))renderer.enabled=false;
        }
        GameObject prefab=Resources.Load<GameObject>("LumenCollection/RomanMarketStall");
        if(prefab==null){Debug.LogWarning("Full-sized Roman market stall prefab is missing");yield break;}
        foreach(Vector3 requested in new[]{new Vector3(-7f,0f,-23f),new Vector3(7f,0f,-23f),new Vector3(-20f,0f,5f),new Vector3(20f,0f,5f)})
        {
            Vector3 position=requested;
            if(NavMesh.SamplePosition(requested,out NavMeshHit ground,2f,NavMesh.AllAreas))position=ground.position;
            GameObject stall=Instantiate(prefab,position,Quaternion.identity,transform);
            stall.name="Roman street market stall";
            ObjectMetadata data=stall.AddComponent<ObjectMetadata>();
            data.objectName="Roman market stall";data.displayName=data.objectName;
            data.scenario="ancient_rome";data.category="City market";data.isInteractable=true;
            data.description="A full-sized interpretive market stall with timber supports, striped cloth shade, produce bins, storage crates and clay jars. Roman markets traded food and everyday goods; this is a reconstruction, not a specific excavated stall.";
            data.descriptionSummary=data.description;data.contextualTags="rome, market, trade, food, city";
            data.interactionHint="Ask your guide about Roman markets";
            NavMeshObstacle obstacle=stall.AddComponent<NavMeshObstacle>();obstacle.carving=true;
            obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0f,.49f,-.38f);obstacle.size=new Vector3(3.28f,1f,.96f);
            Vector3 merchantPosition=position+Vector3.forward*1.1f;
            ScenarioPopulation.Spawn(transform,"Merchant "+(requested.x<0?"Gaius":"Publius")+requested.z,merchantPosition,"RomanCitizen","market seller",false,"ancient_rome");
        }
    }

    private void BuildCityBlocks()
    {
        Texture2D pavers=Resources.Load<Texture2D>("EnvironmentTextures/LimestonePavers");
        if(pavers!=null)
        {
            var stone=LumenRuntimeResources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Roman limestone street",color=new Color(.82f,.83f,.79f),mainTexture=pavers});
            stone.mainTextureScale=new Vector2(30f,30f);stone.SetFloat("_Smoothness",.18f);
            foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                if(renderer.name=="RomanGround"||renderer.name.Contains("Paver")||renderer.name.Contains("Cobble"))renderer.sharedMaterial=stone;
        }
        var source=Resources.Load<GameObject>("LumenCollection/RomanInsula");if(source==null)return;
        // The old Egyptian comparison monument does not belong in this Roman
        // district. Preserve its authored source, retire it from this live route.
        GameObject.Find("Pharaoh_Tomb_Complex")?.SetActive(false);
        // Replace the coarse box shops at their existing sites; preserve temples/forum.
        foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if(renderer.name.StartsWith("Forum_Shop_")||renderer.name.StartsWith("Market_West_")||renderer.name.StartsWith("Market_East_")||renderer.name.StartsWith("Market_Back_"))
            {
                renderer.enabled=false;
                foreach(Collider c in renderer.GetComponents<Collider>())c.enabled=false;
            }
        foreach(Vector3 p in new[]{new Vector3(-25,0,-19),new Vector3(27,0,-19),new Vector3(-31,0,1),new Vector3(31,0,1),new Vector3(-17,0,22),new Vector3(17,0,22),new Vector3(-22,0,-5),new Vector3(22,0,-5),new Vector3(-17.5f,0,12),new Vector3(17.5f,0,12),new Vector3(27,0,26),new Vector3(35,0,26)})
        {
            Quaternion rotation=p.x<0?Quaternion.Euler(0,-90,0):Quaternion.Euler(0,90,0);
            if(p.z>20)rotation=Quaternion.identity;
            GameObject building=Instantiate(source,p,rotation,transform);building.name="Full-sized Roman shop and dwelling";
            foreach(MeshFilter f in building.GetComponentsInChildren<MeshFilter>())f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
            var obstacle=building.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.center=new Vector3(0,3,0);obstacle.size=new Vector3(7.2f,6f,5.6f);
            var data=building.AddComponent<ObjectMetadata>();data.objectName="Roman shop and home";data.displayName=data.objectName;data.category="Architecture";data.scenario="ancient_rome";data.isInteractable=true;
            data.description="A full-sized interpretive shop-house with an open ground-floor trading space, upper-storey shutters, red plaster dado and tiled roof. It illustrates mixed domestic and commercial urban buildings; it is not a scan of a specific site.";data.descriptionSummary=data.description;
        }
    }

    private void AddStreetGreenery()
    {
        var source=Resources.Load<GameObject>("LumenCollection/CityOlivePlanter");if(source==null)return;
        foreach(float x in new[]{-15f,15f})foreach(float z in new[]{-19f,-11f,10f,21f})
        {
            var tree=Instantiate(source,new Vector3(x,0,z),Quaternion.Euler(0,z*11,0),transform);tree.name="Street olive and stone planter";
            var box=tree.AddComponent<BoxCollider>();box.center=Vector3.up*.33f;box.size=new Vector3(1.3f,.66f,1.3f);
            var obstacle=tree.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.center=box.center;obstacle.size=box.size;
        }
    }

    private void RestoreTemples()
    {
        var source=Resources.Load<GameObject>("LumenCollection/RomanTempleRestoration");if(source==null)return;
        foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if(renderer.name.StartsWith("Temple_"))
            {
                renderer.enabled=false;
                foreach(Collider collider in renderer.GetComponents<Collider>())collider.enabled=false;
            }
        foreach(float x in new[]{-25f,25f})
        {
            var temple=Instantiate(source,new Vector3(x,0,14),Quaternion.identity,transform);temple.name="Full-sized Roman temple restoration";
            foreach(MeshFilter mesh in temple.GetComponentsInChildren<MeshFilter>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            var obstacle=temple.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.center=new Vector3(0,2.5f,0);obstacle.size=new Vector3(8.8f,5,7.2f);
            var data=temple.AddComponent<ObjectMetadata>();data.objectName="Roman temple";data.displayName=data.objectName;data.category="Architecture";data.scenario="ancient_rome";data.isInteractable=true;
            data.description="A full-sized interpretive Roman temple with fluted columns, moulded capitals, triangular pediments, tiled roof and an open cella. It is an original architectural composition, not a scan of a specific excavated temple.";data.descriptionSummary=data.description;
        }
    }
}
