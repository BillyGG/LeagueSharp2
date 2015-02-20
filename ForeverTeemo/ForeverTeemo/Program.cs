using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace ForeverTeemo
{
    class Program
    {

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += ForeverTeemo.Game_OnGameLoad;
        }
    }
}
