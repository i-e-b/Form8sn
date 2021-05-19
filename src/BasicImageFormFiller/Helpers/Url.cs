using System.Collections.Generic;

namespace BasicImageFormFiller.Helpers
{
    public static class Url
    {
        public static string? Prefix(string command)
        {
            return command.Contains('?') ? command[..command.IndexOf('?')] : command;
        }

        public static int GetIndexFromQuery(string command)
        {
            if (!GetQueryBits(command).TryGetValue("index", out var idxStr)) idxStr = "0";
            if (!int.TryParse(idxStr, out var idx)) idx = 0;
            return idx;
        }
        
        public static string GetValueFromQuery(string command, string key)
        {
            if (!GetQueryBits(command).TryGetValue(key, out var idxStr)) idxStr = "";
            return idxStr;
        }

        private static Dictionary<string,string> GetQueryBits(string command)
        {
            var result = new Dictionary<string,string>();
            var idx = command.IndexOf('?')+1;
            if (idx < 0) return result;
            var pairs = command[idx..]?.Split('&');
            if (pairs == null) return result;
            foreach (var pair in pairs)
            {
                var kvp = pair.Split("=", 2);
                if (result.ContainsKey(kvp[0])) continue;
                result.Add(kvp[0], kvp[1]);
            }
            return result;
        }
    }
}