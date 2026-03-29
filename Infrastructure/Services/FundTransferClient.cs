using System.Net.Http.Json;
using ShortcodeValidation.Api.Features.FundTransfer;

public class FundTransferClient
{
    private readonly HttpClient _httpClient;

    public FundTransferClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FundTransferResponse?> TransferAsync(FundTransferRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/transfer", request);

        if (!response.IsSuccessStatusCode)
        {
            return new FundTransferResponse
            {
                Status = "FAILED",
                Message = "HTTP error calling transfer service"
            };
        }

        return await response.Content.ReadFromJsonAsync<FundTransferResponse>();
    }
}