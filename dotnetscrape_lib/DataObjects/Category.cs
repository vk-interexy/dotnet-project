using System.Collections.Generic;
using dotnetscrape_constants;

namespace dotnetscrape_lib.DataObjects
{
    public class Category
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public List<SubCategory> SubCategories { get; }

        public Category()
        {
            SubCategories = new List<SubCategory>();
        }
        public void ClearSubCategories()
        {
            SubCategories.Clear();
        }
        public void AddSubCategory(SubCategory subCat)
        {
            subCat.ParentCategory = this;
            SubCategories.Add(subCat);
        }

        public bool IsFullSearch()
        {
            return string.Equals(Name, Constants.FullSearchCategoryName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
