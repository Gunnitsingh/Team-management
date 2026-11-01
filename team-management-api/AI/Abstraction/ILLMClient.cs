using team_management_api.Infrastructure;

namespace team_management_api.Interface;

public interface ILLMClient
{
    Task<LLMResponse> GenerateAsync(Prompt prompt, CancellationToken cancellationToken = default);
}

