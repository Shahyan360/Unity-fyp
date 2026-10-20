using System;
using System.Linq;
using UnityEngine;

/// <summary>The same authored records drive wardrobe, labels and backend personas.</summary>
public static class LumenCharacterDefinitions
{
    [Serializable] public sealed class Definition
    {
        public string id,name,role,style,resource,voice,cloudVoice,neuralVoice,direction,manner;
    }
    [Serializable] private sealed class Catalog {public Definition[] characters;}
    private static Definition[] definitions;
    public static Definition ForStyle(string style)
    {
        if(definitions==null)
        {
            var json=Resources.Load<TextAsset>("LumenCharacterIdentities");
            definitions=json!=null?JsonUtility.FromJson<Catalog>(json.text)?.characters:Array.Empty<Definition>();
        }
        return definitions?.FirstOrDefault(d=>d.style==style);
    }
}
