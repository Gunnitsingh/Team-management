using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using team_management_api.Interface;
using team_management_api.Options;

namespace team_management_api.Infrastructure;

public class OllamaClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient> _logger;
    private const string GenerateEndpoint = "api/generate";
    private const string TagsEndpoint = "api/tags";
    private static long ToMilliseconds(long nanoseconds) => nanoseconds / 1_000_000;

    public OllamaClient(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("OllamaClient initialized - BaseAddress: {BaseAddress}, Model: {Model}", 
            _httpClient.BaseAddress?.AbsoluteUri ?? "NOT SET", _options.Model);
    }

    public async Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        _logger.LogInformation("Sending request to Ollama with model: {Model}", _options.Model);

        try
        {
            var request = new OllamaGenerateRequest
            {
                Model = _options.Model,
                Prompt = prompt,
                Stream = false
            };

            _logger.LogDebug("Posting to Ollama API with prompt length: {PromptLength}", prompt.Length);
            
            // Ensure BaseAddress is set
            if (_httpClient.BaseAddress == null)
            {
                _logger.LogWarning("BaseAddress not set, configuring from OllamaOptions: {BaseUrl}", _options.BaseUrl);
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }
            
            _logger.LogDebug("Using BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            var response = await _httpClient.PostAsJsonAsync(GenerateEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);

            if (payload == null)
            {
                _logger.LogError("Ollama API returned null payload");
                return new LLMResponse
                {
                    Success = false,
                    Message = "Ollama API returned null response"
                };
            }

            _logger.LogInformation(
                "Successfully received response from Ollama - PromptTokens: {PromptTokens}, ResponseTokens: {ResponseTokens}, TotalDuration: {TotalDuration}ms, LoadDuration: {LoadDuration}ms",
                payload.PromptEvalCount,
                payload.EvalCount,
                ToMilliseconds(payload.TotalDuration),
                ToMilliseconds(payload.LoadDuration)
            );

            return new LLMResponse
            {
                Success = true,
                Content = payload.Response,
                Model = payload.Model,
                PromptTokens = payload.PromptEvalCount,
                ResponseTokens = payload.EvalCount,

            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Ollama failed: {Message}", ex.Message);
            return new LLMResponse
            {
                Success = false,
                Content = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama request timeout exceeded after {TimeoutSeconds} seconds", _options.TimeoutSeconds);
            return new LLMResponse
            {
                Success = false,
                Message = $"Request timeout after {_options.TimeoutSeconds} seconds"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while communicating with Ollama");
            return new LLMResponse
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<HealthCheckResponse> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check on Ollama service");

        try
        {
            // Ensure BaseAddress is set
            if (_httpClient.BaseAddress == null)
            {
                _logger.LogWarning("BaseAddress not set, configuring from OllamaOptions: {BaseUrl}", _options.BaseUrl);
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds));

            var response = await _httpClient.GetAsync(TagsEndpoint, cts.Token);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Ollama health check passed");
            return new HealthCheckResponse
            {
                Status = "Healthy",
                Model = _options.Model
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Ollama health check timeout after {TimeoutSeconds} seconds", _options.HealthCheckTimeoutSeconds);
            return new HealthCheckResponse
            {
                Status = "Unhealthy",
                Model = _options.Model,
                Message = "Health check timeout"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed: {Message}", ex.Message);
            return new HealthCheckResponse
            {
                Status = "Unhealthy",
                Model = _options.Model,
                Message = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Ollama health check");
            return new HealthCheckResponse
            {
                Status = "Unhealthy",
                Model = _options.Model,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<LLMResponse> GenerateAsync(Prompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.UserPrompt);

        var fullPrompt = $"{prompt.SystemPrompt}\n\n{prompt.UserPrompt}";
        return await GenerateAsync(fullPrompt, cancellationToken);
    }
}