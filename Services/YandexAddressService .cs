using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;


namespace DeliveryTgBot.Services
{
    public class YandexAddressService : IAddressService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

        public YandexAddressService(HttpClient httpClient, IConfigurationService configurationService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
        }
        public string ExtractStreetAndHouse(string fullAddress, string cityName)
        {
            if (string.IsNullOrEmpty(fullAddress) || string.IsNullOrEmpty(cityName))
                return fullAddress;

            int cityIndex = fullAddress.IndexOf(cityName, StringComparison.OrdinalIgnoreCase);
            if (cityIndex == -1)
                return null;

            int startIndex = cityIndex + cityName.Length;

            if (startIndex >= fullAddress.Length)
                return "";

            string streetPart = fullAddress.Substring(startIndex).Trim(new char[] { ',', ' ' });

            return streetPart;
        }

        public async Task<List<string>> GetAddressSuggestionsAsync(string query, string cityName)
        {
            string queryWithCity = $"{cityName}, {query}";
            string url = $"https://geocode-maps.yandex.ru/1.x/?" +
                $"apikey={_configurationService.YandexApiKey}" +
                $"&geocode={Uri.EscapeDataString(queryWithCity)}" +
                $"&format=json&lang=ru_RU&results={5}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var members = doc.RootElement
                .GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember");

            var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var member in members.EnumerateArray())
            {
                var geoObject = member.GetProperty("GeoObject");
                var address = geoObject
                    .GetProperty("metaDataProperty")
                    .GetProperty("GeocoderMetaData")
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(address))
                {
                    address = address.Trim();

                    // Выделяем только улицу и дом
                    string streetHouse = ExtractStreetAndHouse(address, cityName);

                    if (!string.IsNullOrEmpty(streetHouse))
                        suggestions.Add(streetHouse);
                }
            }

            return suggestions.ToList();
        }


    }

}