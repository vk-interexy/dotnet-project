using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOTNETScrape.DataObjects
{
    public class SubCategory
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public Category ParentCategory { get; set; }
    }
    public class SubCategoryResult
    {
        public List<SubCategory> d { get; set; }
    }
}
