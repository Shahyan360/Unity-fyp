using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>One scene action's result. Parsing, path acceptance and arrival are distinct.</summary>
[Serializable]
public sealed class WorldActionExecution
{
    public string actionId=Guid.NewGuid().ToString("N"), turnId, state="understood", target, reason;
    public bool handled, accepted;
    public bool Terminal => state=="completed"||state=="failed"||state=="cancelled";
    public bool Succeeded => state=="completed";
    public void Set(string value,string detail){state=value;reason=detail;}
}

