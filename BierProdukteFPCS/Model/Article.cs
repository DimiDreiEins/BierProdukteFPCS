namespace BierProdukteFPCS.Model
{
    public class Article
    {
        public int id { get; set; }
        public string shortDescription { get; set; }
        public int amountOfBottles {  get; set; }
        public  double price { get; set; }
        public string unit { get; set; }
        public string pricePerUnitText { get; set; }
        public double pricePerUnit {  get; set; }
        public string image { get; set; }
    }
}
