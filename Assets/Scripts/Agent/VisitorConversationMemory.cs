using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Process-only bounded memory. No raw transcript is written to preferences or shared between people.</summary>
public static class VisitorConversationMemory
{
    private static readonly Dictionary<string,Queue<string>> exchanges=new Dictionary<string,Queue<string>>();
    private static readonly Dictionary<string,string> explicitPreferences=new Dictionary<string,string>();
    public static void ObservePreference(string text)
    {
        text=text??"";
        if(Regex.IsMatch(text,@"(?i)\b(?:forget|do not remember|don't remember|never remember|stop remembering)\b"))
        {
            if(Regex.IsMatch(text,@"(?i)\b(?:name)\b"))explicitPreferences.Remove("visitor name");
            else if(Regex.IsMatch(text,@"(?i)\b(?:subject|topic|interest)\b"))explicitPreferences.Remove("favourite topic");
            else Reset();
            return;
        }
        if(!Regex.IsMatch(text??"",@"(?i)\bremember\b"))return;
        var name=Regex.Match(text,@"(?i)\bmy name is ([\p{L} '-]{1,40}?)(?:[,.!?]| and |$)");
        var interest=Regex.Match(text,@"(?i)\b(?:my )?favou?rite (?:subject|topic|interest) is ([\p{L} '-]{1,60}?)(?:[,.!?]| please|$)");
        if(name.Success)explicitPreferences["visitor name"]=name.Groups[1].Value.Trim();
        if(interest.Success)explicitPreferences["favourite topic"]=interest.Groups[1].Value.Trim();
    }
    public static void Record(string speaker,string question,string answer)
    {
        if(string.IsNullOrWhiteSpace(question)||string.IsNullOrWhiteSpace(answer))return;
        if(!exchanges.TryGetValue(speaker,out var history))
        {
            if(exchanges.Count>=24)exchanges.Clear();
            exchanges[speaker]=history=new Queue<string>();
        }
        string Clip(string value)=>value.Length>360?value.Substring(0,360):value;
        history.Enqueue("Visitor: "+Clip(question)+" / "+speaker+": "+Clip(answer));
        while(history.Count>4)history.Dequeue();
    }
    public static string Context(string speaker)
    {
        var notes=new List<string>();
        foreach(var pair in explicitPreferences)notes.Add(pair.Key+"="+pair.Value);
        string own=exchanges.TryGetValue(speaker,out var history)?string.Join(" | ",history):"none";
        return "Memory policy: this app session only; resets on exit or Forget. Only explicitly remembered name/topic preferences carry between speakers. Private replies stay with their speaker. Explicit shared preferences: "+string.Join(", ",notes)+". This speaker's last four exchanges: "+own;
    }
    public static void Reset(){exchanges.Clear();explicitPreferences.Clear();}
}
