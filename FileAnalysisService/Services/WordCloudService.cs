using System.Text;
using System.Text.Json;
using System.Net;
using Shared.Models;

namespace FileAnalysisService.Services;

public interface IWordCloudService
{
    Task<string> GenerateWordCloudUrlAsync(Dictionary<string, int> wordFrequency);
}

public class WordCloudService : IWordCloudService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WordCloudService> _logger;

    public WordCloudService(IHttpClientFactory httpClientFactory, ILogger<WordCloudService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<string> GenerateWordCloudUrlAsync(Dictionary<string, int> wordFrequency)
    {
        if (wordFrequency == null || wordFrequency.Count == 0)
        {
            return Task.FromResult(string.Empty);
        }

        try
        {
            // Формируем данные для QuickChart API
            var words = wordFrequency.Select(kvp => new { text = kvp.Key, size = kvp.Value }).ToList();
            
            var chartConfig = new
            {
                type = "wordCloud",
                data = new
                {
                    labels = wordFrequency.Keys.ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Word Frequency",
                            data = wordFrequency.Select(kvp => new { text = kvp.Key, value = kvp.Value }).ToArray()
                        }
                    }
                },
                options = new
                {
                    title = new { display = true, text = "Word Cloud" },
                    plugins = new
                    {
                        wordcloud = new
                        {
                            color = "#000000",
                            minSize = 10,
                            rotation = new { from = 0, to = 0, numOfOrientation = 1 }
                        }
                    }
                }
            };

            var configJson = JsonSerializer.Serialize(chartConfig);
            var encodedConfig = WebUtility.UrlEncode(configJson);
            
            // Используем QuickChart API для генерации облака слов
            var url = $"https://quickchart.io/chart?c={encodedConfig}";
            
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating word cloud");
            return Task.FromResult(string.Empty);
        }
    }
}

