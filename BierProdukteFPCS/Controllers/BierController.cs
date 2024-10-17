using BierProdukteFPCS.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.OpenApi.Any;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BierProdukteFPCS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BierController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public BierController( IHttpClientFactory clientFactory) 
        {
            _clientFactory = clientFactory;
        }

        // Helpers
        // we need to a method to read any JSON from web
        private async Task<List<Product>> ReadJSONFromUrl(string url) 
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            HttpClient client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            // check the response
            if (response.IsSuccessStatusCode)
            {
                var jsondata = await response.Content.ReadAsStringAsync();
                return ReadJSONdata(jsondata);
            }
            else
            {
                return null;
            }
        }

        private List<Product> ReadJSONdata(string jsondata)
        {
            var jsonObject = JsonSerializer.Deserialize<List<Product>>(jsondata);

            if (jsonObject != null)
            {
                jsonObject = PricePerUnitFromPricePerUnitText(jsonObject);
                return jsonObject;
            }
            else
            {
                return null;
            }
            
        }

        private List<Product> PricePerUnitFromPricePerUnitText(List<Product> jsonObject)
        {
            if(jsonObject != null)
            {
                foreach (var product in jsonObject)
                {
                    foreach (var article in product.articles)
                    {
                        article.pricePerUnit = ExtractDoubleValueFromStringPricePerUnitText(article.pricePerUnitText);
                        article.amountOfBottles = ExtractIntValueFromshortDescription(article.shortDescription);
                    }
                }

                return jsonObject;
            }
            else
            {
                return null;
            }
        }

        // mean logic
        private double ExtractDoubleValueFromStringPricePerUnitText(string input)
        {
            string pattern = @"\(([\d,]+) €/Liter\)";

            var match = Regex.Match(input, pattern);

            if (match.Success)
            {
                string priceText = match.Groups[1].Value.Replace(",", ".");

                if (double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out double pricePerLiter))
                {
                    return pricePerLiter;
                }
            }
            return 0;
        }
        private int ExtractIntValueFromshortDescription(string input)
        {
            string pattern = @"^\d+";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                return int.Parse(match.Value);
            }
            else
            {
                return 0;
            }
        }
        private Product findMostExpensiveBeerPerLitre(List<Product> products)
        {
            Product mostExpensive = products.FirstOrDefault();

            foreach (var product in products)
            {
                foreach (var article in product.articles)
                {
                    if (mostExpensive.articles.FirstOrDefault().pricePerUnit < article.pricePerUnit)
                    {
                        mostExpensive = product;
                    }
                }
            }

            return mostExpensive;
        }
        private Product findCheapestBeerPerLitre(List<Product> products)
        {
            Product cheapest = products.FirstOrDefault();

            foreach (var product in products)
            {
                foreach (var article in product.articles)
                {
                    if (cheapest.articles.FirstOrDefault().pricePerUnit > article.pricePerUnit)
                    {
                        cheapest = product;
                    }
                }
            }

            return cheapest;
        }

        private List<Product> findSpecificPrice(List<Product> products, double price)
        {
            List<Product> matches = new List<Product>();

            foreach (var product in products)
            {
                foreach (var article in product.articles)
                {
                    if (article.price == price)
                    {
                        if (!matches.Contains(product))
                        {
                            matches.Add(product);
                        }         
                    }
                }
            }

            return matches;
        }

        private Product findMostBottlesPerProduct(List<Product> products)
        {
            Product match = null;

            foreach (var product in products)
            {
                foreach (var article in product.articles)
                {
                    if (match != null && article.amountOfBottles > match.articles.FirstOrDefault().amountOfBottles)
                    {
                        match = product;
                    }
                    else if (match == null)
                    {
                        match = product;
                    }
                }
            }

            return match;
        }

        private List<Product> sortProductListByArticlePrice(List<Product> products)
        {
            // we first sort articles inside of each product - so the cheapest article is always first
            foreach (var product in products)
            {
                product.articles = product.articles.OrderBy(a => a.pricePerUnit).ToList();
            }

            // then we just need to sort the products by the first articles price per unit
            return products.OrderBy(p => p.articles.FirstOrDefault().pricePerUnit).ToList();
        }

        [HttpGet("priceRange")]
        public async Task<IActionResult> GetPriceRange(string url)
        {
            List<Product> products = await ReadJSONFromUrl(url);
            if (products == null) return BadRequest("Irgendwas ist schiefgelaufen");

            Product mostExpensive = findMostExpensiveBeerPerLitre((List<Product>)products);
            Product cheapest = findCheapestBeerPerLitre(((List<Product>)products));

            return Ok(new
            {
                MostExpensive = mostExpensive,
                Cheapest = cheapest
            });
        }

        [HttpGet("priceExactly")]
        public async Task<IActionResult> GetBeersWithPrice(string url, double targetPrice = 17.99)
        {
            var products = await ReadJSONFromUrl(url);
            if (products == null) return BadRequest("Unable to fetch products");

            List<Product> matchingProducts = findSpecificPrice(products, targetPrice);

            matchingProducts = sortProductListByArticlePrice(matchingProducts);

            return Ok(matchingProducts);
        }

        [HttpGet("mostBottles")]
        public async Task<IActionResult> GetProductWithMostBottles(string url)
        {
            var products = await ReadJSONFromUrl(url);
            if (products == null) return BadRequest("Unable to fetch products");

            Product mostBottlesProduct = findMostBottlesPerProduct((List<Product>)products);

            return Ok(mostBottlesProduct);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll(string url, double targetPrice = 17.99)
        {
            var products = await ReadJSONFromUrl(url);
            if (products == null) return BadRequest("Unable to fetch products");


            Product mostExpensive = findMostExpensiveBeerPerLitre((List<Product>)products);
            Product cheapest = findCheapestBeerPerLitre(((List<Product>)products));
            List<Product> matchingProducts = findSpecificPrice(products, targetPrice);

            matchingProducts = sortProductListByArticlePrice(matchingProducts);

            Product mostBottlesProduct = findMostBottlesPerProduct((List<Product>)products);

            return Ok(new
            {
                MostExpensive = mostExpensive,
                Cheapest = cheapest,
                MatchingPricesSorted = matchingProducts,
                MostBottlesProduct = mostBottlesProduct
            });
        }


    }
}
