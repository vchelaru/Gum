using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Data
{
    public class HyTaleItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }

        private int _durability;
        public int Durability 
        {
            get
            {
                return _durability;
            }
            set
            {
                if (value > 100)
                {
                    _durability = 100;
                }
                else if (value < 0)
                {
                    _durability = 0;
                }
                else
                {
                    _durability = value;
                }
            } 
        }

        public HyTaleItem(string name, int quantity, int durability)
        {
            Name = name;
            Quantity = quantity;
            Durability = durability;
        }
    }
}
