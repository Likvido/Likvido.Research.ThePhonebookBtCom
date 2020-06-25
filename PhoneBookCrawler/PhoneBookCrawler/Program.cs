using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PhoneBookCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Time to crawl :-) ");

            var service = new PhoneBookCrawler();

            var cities =new List<string>() {"London","Manchester", "Glasgow", "Leeds", "Liverpool","Newcastle","Sheffield","Belfast","Leicester","Sunderland","Bristol" };
            var categories = new List<string>() {"Accountants", "Plumbers", "Electricians%20-%20Commercial%20and%20Industrial", "Manufacturing%20Joiners", "Food%20Manufacturers" };

            foreach (var city in cities)
            {
                Console.WriteLine("Crawling: " + city);

                foreach (var category in categories)
                {
                    Console.WriteLine(string.Format("Crawling city {0} and category {1}", city,category));
                    service.Crawl(category,city);
                }
            }

            
        }
    }

    public class PhoneBookCrawler
    {
        private const string FILEPATH = "Goldleads.csv";
        private List<string> ExistingWebsites = new List<string>();


        public PhoneBookCrawler()
        {
            ReadExistingWebsites();
        }
        
        public void Crawl(string category, string city)
        {
            var baseUrl =
                "https://www.thephonebook.bt.com/Business/TypeSearch/?BusinessSearchTerm={0}&Location={1}&SearchOrder=Recommended&PageNumber={2}&InitialBusinessTypeSearchTerm={0}&InitialLocationSearchTerm={1}&BusinessTypeFound=true&LocationFound=true";

            for (int page = 1; page < 3; page++)
            {
                var finalUrl = string.Format(baseUrl, category, city, page);

                using (var client = new WebClient())
                {
                    var str = client.DownloadString(finalUrl);

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(str);

                    var content = doc.DocumentNode.SelectNodes("//a[contains(@data-ref,'website')]");

                    List<string> websites = new List<string>();
                    foreach (var website in content)
                    {
                        var actualSite = website.Attributes["href"].Value;
                        Uri uriResult;
                        bool result = Uri.TryCreate(actualSite, UriKind.Absolute, out uriResult)
                                      && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                        if (result && !websites.Contains(actualSite) && !ExistingWebsites.Contains(actualSite))
                            websites.Add(actualSite);
                    }

                    StorePage(category, city, websites);
                }
            }

         
        }


        private void ReadExistingWebsites()
        {
            if (!File.Exists(FILEPATH))
                return;

            using (var rdr = new StreamReader(FILEPATH))
            {
                while (!rdr.EndOfStream)
                {
                    var line = rdr.ReadLine().Split(';')[2];
                    if (line != "Website")
                        ExistingWebsites.Add(line);
                }
            }

        }

        private void StorePage(string category, string city, List<string> websites)
        {
            if (!websites.Any())
            {
                return;
            }

            if (!File.Exists(FILEPATH))
            {
                File.Create(FILEPATH).Dispose();
                TextWriter tw = new StreamWriter(FILEPATH);
                tw.WriteLine(string.Format("Category;City;Website"));

                foreach (var site in websites)
                {
                    tw.WriteLine(string.Format("{0};{1};{2}", category, city, site));
                }

                tw.Close();
            }
            else
            {
                using (var tw = new StreamWriter(FILEPATH, true))
                {
                    foreach (var site in websites)
                    {
                        tw.WriteLine(string.Format("{0};{1};{2}", category, city, site));
                    }

                    tw.Close();
                }
            }
        }


    }
}
