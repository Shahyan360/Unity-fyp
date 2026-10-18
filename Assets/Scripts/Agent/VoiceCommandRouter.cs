using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Explicit voice commands execute locally, without waiting for an LLM.</summary>
public static class VoiceCommandRouter
{
    public static bool TryExecute(string utterance,out string reply)
    {
        reply="";string text=utterance.ToLowerInvariant().Trim(' ','.','!','?');
        text=Regex.Replace(text,@"^(?:(?:hey )?(?:kai|orion|theo|professor theo|maya|lucius|aurelius|guide)[, ]+)?(?:please )?","");
        if(Regex.IsMatch(text,@"\b(?:don't|do not|never)\b"))return false;
        var identity=UnityEngine.Object.FindAnyObjectByType<ScenarioCharacterIdentity>();
        var guide=identity!=null?identity.GetComponent<AgentCompanionController>():null;
        if(Regex.IsMatch(text,@"^(?:stop|stop moving|stay here|wait here|stay put|stop talking|be quiet)$"))
        {
            guide?.SetVoiceFollow(false);UnityEngine.Object.FindAnyObjectByType<VoicePlayerNavigation>()?.Cancel();
            reply=text.Contains("talk")||text=="be quiet"?"Of course. I'll listen.":"All right. I'll stay here.";return true;
        }
        if(Regex.IsMatch(text,@"^(?:follow me|come with me|keep following me)$"))
        {guide?.SetVoiceFollow(true);reply=guide!=null?"I'm with you. Lead the way.":"We can explore together here.";return true;}
        var command=Regex.Match(text,@"^(?:(?:can|could|would) you )?(go|walk|move|come|take me|move me|walk me)(?: over)?(?: to)?\s+(.+)$");
        if(!command.Success)return false;
        string target=Regex.Replace(command.Groups[2].Value,@"^the\s+|\s+please$","");
        if(Regex.IsMatch(target,@"^(?:museum|ancient rome|rome|classroom|science studio|cafe|café)$"))return false;
        bool playerCommand=command.Groups[1].Value.Contains(" me");
        var player=UnityEngine.Object.FindAnyObjectByType<CafeWalkController>();var camera=Camera.main;
        Vector3 origin=playerCommand&&player!=null?player.transform.position:guide!=null?guide.transform.position:Vector3.zero;
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
                float distance=3;string count=direction.Groups[2].Value;
                if(!float.TryParse(count,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float parsed))
                    parsed=count=="one"?1:count=="two"?2:count=="three"?3:count=="four"?4:count=="five"?5:count=="ten"?10:3;
                distance=Mathf.Clamp(parsed,1,12);var forward=camera.transform.forward;forward.y=0;forward.Normalize();
                Vector3 axis=direction.Groups[1].Value=="left"?-camera.transform.right:direction.Groups[1].Value=="right"?camera.transform.right:direction.Groups[1].Value.StartsWith("back")?-forward:forward;
                destination=origin+axis*distance;resolved=true;
            }
            else if(guide!=null)
            {
                if(!playerCommand&&guide.TryNavigateToNamedTarget(target)){guide.HoldAfterVoiceArrival();reply="I'll meet you by "+target+".";return true;}
                resolved=WorldDestination.TryResolve(target,origin,out destination);
            }
        }
        if(!resolved)resolved=WorldDestination.TryResolve(target,origin,out destination);
        bool moved=false;
        if(resolved)
        {
            if(playerCommand&&player!=null){var motor=player.GetComponent<VoicePlayerNavigation>();if(motor==null)motor=player.gameObject.AddComponent<VoicePlayerNavigation>();moved=motor.Go(destination);if(moved)guide?.SetVoiceFollow(true);}
            else if(guide!=null)moved=guide.TryVoiceDestination(destination);
        }
        reply=moved?playerCommand?"Let's walk there.":"On my way.":"I can't find a safe route there. Look at a clear patch of floor and say 'go there', or name a nearby exhibit.";
        return true;
    }
}
