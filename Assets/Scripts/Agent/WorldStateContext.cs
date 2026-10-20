using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Compact authoritative scene facts, never guesses about the rendered image.</summary>
public static class WorldStateContext
{
    public static WorldActionExecution LastTransition {get;set;}
    public static string Snapshot()
    {
        string scene=SceneManager.GetActiveScene().name;
        string variant=ScenarioRuntimeManager.GetScenarioForScene(scene);
        var player=Object.FindAnyObjectByType<CafeWalkController>();
        Vector3 position=player!=null?player.transform.position:Camera.main!=null?Camera.main.transform.position:Vector3.zero;
        var destinations=Object.FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude)
            .Where(p=>p.enabled&&p.isInteractable&&(p.category=="Destination"||p.category=="City market"))
            .OrderBy(p=>(p.transform.position-position).sqrMagnitude).ToArray();
        string nearby=destinations.Length>0?destinations[0].GetDisplayName():"arrival / exploration area";
        string names=string.Join(", ",destinations.Select(p=>p.GetDisplayName()).Distinct().Take(16));
        string model=variant=="classroom"&&ClassroomLearningExhibit.HasActiveModel?ClassroomLearningExhibit.ActiveDescription:"none";
        return "Actual scene="+scene+"; world/variant="+variant+"; nearest named area="+nearby+
            "; visitor position="+position.ToString("F1")+"; available local walk destinations="+names+
            "; active learning exhibit="+model+"; optional visual aids="+(LumenPreferences.GetInt("Lumen.VisualAids",1)==0?"disabled; use verbal explanations and do not request images":"allowed, subject to the visitor's explicit negative image preference")+"; simulated cafe order="+(Object.FindAnyObjectByType<CafeOrderExperience>()?.OrderState??"not in the cafe")+"; world destinations=Home, Ancient Rome, Classroom, Museum, modern Cafe. "+
            "Rome arena viewing terrace is accessible by a validated walking route. The modern Cafe requires changing worlds. "+
            "Only completed execution confirms arrival. Latest world transition="+(LastTransition!=null?LastTransition.state+" / "+LastTransition.reason:"none")+". Scene metadata is not visual perception.";
    }
}
