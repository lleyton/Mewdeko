﻿namespace Mewdeko.Modules.Searches.Common
{
    public class WoWJoke
    {
        public string Question { get; set; }
        public string Answer { get; set; }

        public override string ToString()
        {
            return $"`{Question}`\n\n**{Answer}**";
        }
    }
}