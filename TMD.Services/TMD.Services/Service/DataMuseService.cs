using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMDInvestments.Models;

namespace TMD.Services.Service
{
    public sealed class DataMuseService
    {
        IHttpClientFactory? _httpClientFactory;
        ILogger? _logger;
        const string _baseUrl = "https://api.datamuse.com/";
        public DataMuseService(ILogger? logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        public string GetSynonyms(string word)
        {
            try
            {
                word = GetSuggenstion(word);
                var client = _httpClientFactory.CreateClient();
                //var response = client.GetAsync($"{_baseUrl}?ml={word}").Result;
                //var response = client.GetAsync($"{_baseUrl}words?rel_trg={word}").Result;
                var response = client.GetAsync($"{_baseUrl}words?ml={word.Replace(" ", "+")}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    return content;
                }
                else
                {
                    _logger?.LogError($"Failed to get synonyms for {word}. Status code: {response.StatusCode}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting synonyms for {word}");
                return string.Empty;
            }
        }
        private string GetSuggenstion(string word)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                //var response = client.GetAsync($"{_baseUrl}?ml={word}").Result;
                var response = client.GetAsync($"{_baseUrl}sug?s={word}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var results = JsonSerializer.Deserialize<List<WordResult>>(content);//.OrderByDescending(x => x.score);
                    return (results.FirstOrDefault()?.word.Trim() != string.Empty) ? results.FirstOrDefault()?.word : word;
                }
                else
                {
                    _logger?.LogError($"Failed to get synonyms for {word}. Status code: {response.StatusCode}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting synonyms for {word}");
                return string.Empty;
            }
        }
    }
}
