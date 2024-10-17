namespace BierProdukteFPCS.Model
{
    public class Product
    {
        public int id { get; set; }
        public string brandName { get; set; }
        public string name { get; set; }
        public string descriptionText { get; set; }
        public List<Article> articles { get; set; }
    }
}
