using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProject
{
    public enum Race
    {
        Human,
        Orc,
        Dwarf,
        Goblin
    }


    public class Character
    {
        public string Name { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }

        public bool IsDead { get; set; }

        public Race Race { get; set; }
    }
}
