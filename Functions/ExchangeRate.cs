using System.Net;
using Newtonsoft.Json;

namespace SlickRentals_Discord_Bot.Functions
{
    public class ExchangeRate
    {
        private static readonly string apiKey;

        static ExchangeRate()
        {
            var config = DiscordFunctions.GetConfig();
            var privKey = config["currency_api_key"].ToString();

            apiKey = privKey;
        }

        public static double ConvertCurrency(double usdInput)
        {
            using var client = new WebClient();
            var response = client.DownloadString("https://openexchangerates.org/api/latest.json?app_id=" + apiKey);

            dynamic obj = JsonConvert.DeserializeObject(response);

            var rate = (double) obj.rates.GBP;

            return usdInput * rate;
        }
    }
}