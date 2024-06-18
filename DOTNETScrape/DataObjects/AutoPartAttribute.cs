using System;

namespace DOTNETScrape.DataObjects
{
    [Serializable]
    public class AutoPartAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return $"{Name}:{Value}";
        }
    }
}
   
