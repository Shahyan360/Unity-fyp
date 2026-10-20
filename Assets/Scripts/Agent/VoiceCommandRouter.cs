using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Local command parsing preserves handled state separately from execution success.</summary>
public static class VoiceCommandRouter
{
    public static WorldActionExecution LastExecution {get;private set;}
    public static bool TryExecute(string utterance,out string reply)
    {
        var result=Execute(utterance);
        reply=result.reason??"";
        return result.handled;
    }
    public static WorldActionExecution Execute(string utterance,string turnId="")
    {
        var result=new WorldActionExecution{turnId=turnId,target=utterance??""};
        string text=(utterance??"").ToLowerInvariant().Trim(' ','.','!','?');
        text=Regex.Replace(text,@"^(?:(?:hey )?(?:kai|orion|theo|professor theo|maya|lucius|aurelius|guide)[, ]+)?(?:please )?","");
        if(Regex.IsMatch(text,@"\b(?:don't|do not|never|explain|what happens|if someone)\b"))return result;
        var identity=UnityEngine.Object.FindAnyObjectByType<ScenarioCharacterIdentity>();
        var guide=identity!=null?identity.GetComponent<AgentCompanionController>():null;
        var hubFollower=identity!=null?identity.GetComponent<WalkableGuideFollower>():null;
        var player=UnityEngine.Object.FindAnyObjectByType<CafeWalkController>();
        if(Regex.IsMatch(text,@"^(?:stop|stop moving|stay here|wait here|stay put|stop talking|be quiet)$"))
        {
            guide?.SetVoiceFollow(false);player?.GetComponent<VoicePlayerNavigation>()?.Cancel();
            if(!text.Contains("talk")&&text!="be quiet")hubFollower?.SetFollowing(false);
            player?.GetComponent<WorldActionExecutionMonitor>()?.Cancel("You stopped the walk.");
            guide?.GetComponent<WorldActionExecutionMonitor>()?.Cancel("You stopped the walk.");
            result.handled=result.accepted=true;result.Set("completed",text.Contains("talk")||text=="be quiet"?"Of course. I'll listen.":"All right. We'll stay here.");
            return LastExecution=result;
        }
        if(Regex.IsMatch(text,@"^(?:follow me|come with me|keep following me)$"))
        {
            result.handled=true;result.accepted=guide!=null||hubFollower!=null;guide?.SetVoiceFollow(true);hubFollower?.SetFollowing(true);
            result.Set(result.accepted?"completed":"failed",result.accepted?"I'm with you. Lead the way.":"The guide is unavailable here.");return LastExecution=result;
        }
        var command=Regex.Match(text,@"^(?:(?:can|could|would) you )?(go|walk|move|come|take me|move me|walk me|lead me)(?: over)?(?: to)?\s+(.+)$");
        if(!command.Success)return result;
        string target=Regex.Replace(command.Groups[2].Value,@"^the\s+|\s+please$","");
        // World travel is owned by ScenarioTransitionService, never a local Rome food location.
        if(Regex.IsMatch(target,@"^(?:museum|ancient rome|rome|classroom|science studio|modern cafe|cafe|café|home)$"))return result;
        result.handled=true;result.target=target;
        bool visitor=command.Groups[1].Value.Contains(" me");
        var camera=Camera.main;
        Vector3 origin=visitor&&player!=null?player.transform.position:guide!=null?guide.transform.position:Vector3.zero;
        Vector3 destination=origin;bool resolved=false;
        if(target=="here"||target=="me"||target=="my side")
        {if(camera!=null){destination=camera.transform.position-camera.transform.forward*1.5f;resolved=true;}}
        else if(target=="there"||target=="that spot"||target=="where i'm looking")
        {if(camera!=null&&Physics.Raycast(camera.ViewportPointToRay(new Vector3(.5f,.5f,0)),out RaycastHit hit,100,~0,QueryTriggerInteraction.Ignore)){destination=hit.point;resolved=true;}}
        else
        {
            var direction=Regex.Match(target,@"^(forward|forwards|back|backward|backwards|left|right)(?:\s+(\d+(?:\.\d+)?|one|two|three|four|five|ten))?(?:\s+(?:meters|metres|steps))?$");
            if(direction.Success&&camera!=null)
            {
                string count=direction.Groups[2].Value;
                if(!float.TryParse(count,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float distance))
                    distance=count=="one"?1:count=="two"?2:count=="three"?3:count=="four"?4:count=="five"?5:count=="ten"?10:3;
                var forward=camera.transform.forward;forward.y=0;forward.Normalize();
                Vector3 axis=direction.Groups[1].Value=="left"?-camera.transform.right:direction.Groups[1].Value=="right"?camera.transform.right:direction.Groups[1].Value.StartsWith("back")?-forward:forward;
                destination=origin+axis*Mathf.Clamp(distance,1,12);resolved=true;
            }
        }
        if(!resolved)resolved=WorldDestination.TryResolve(target,origin,out destination);
        bool moved=false;
        if(resolved)
        {
            if(visitor&&player!=null)
            {
                var motor=player.GetComponent<VoicePlayerNavigation>()??player.gameObject.AddComponent<VoicePlayerNavigation>();
                moved=motor.Go(destination);if(moved)guide?.SetVoiceFollow(true);
            }
            else if(guide!=null)moved=guide.TryVoiceDestination(destination);
        }
        result.accepted=moved;
        result.Set(moved?"accepted":"failed",moved?(visitor?"Let's walk there. Arrival is still pending.":"On my way. Arrival is still pending."):"I can't find a safe route there. Try a nearby named destination or a clear patch of floor.");
        if(moved)
        {
            var owner=visitor?player.gameObject:guide.gameObject;
            var monitor=owner.GetComponent<WorldActionExecutionMonitor>()??owner.AddComponent<WorldActionExecutionMonitor>();
            monitor.Begin(result,destination,visitor);
        }
        return LastExecution=result;
    }
}
