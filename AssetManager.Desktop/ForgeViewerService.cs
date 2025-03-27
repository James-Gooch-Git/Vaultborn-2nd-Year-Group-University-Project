using AssetManager.Infrastructure.Services;
using System.Net.Http;

public class ForgeViewerService
{
    private readonly ModelDerivativeService _modelService;
    private readonly FileDownloadService _fileService;
    private readonly string _accessToken;

    public ForgeViewerService(string accessToken)
    {
        _accessToken = accessToken;
        _modelService = new ModelDerivativeService(new HttpClient());
        _fileService = new FileDownloadService();
    }

    public async Task<string?> GetPdfViewerHtmlAsync(string projectId, string itemId)
    {
        string objectId = await _fileService.GetStorageIdFromItem(projectId, itemId);
        if (string.IsNullOrEmpty(objectId)) return null;

        string encodedUrn = EncodeObjectIdToUrn(objectId);
        if (!await EnsureTranslationReadyAsync(encodedUrn, isPdf: true))
            return null;

        return ForgeHtmlTemplates.GetPdfViewerHtml(encodedUrn, _accessToken);
    }

    public async Task<string?> GetModelViewerHtmlAsync(string encodedUrn)
    {
        if (!await EnsureTranslationReadyAsync(encodedUrn, isPdf: false))
            return null;

        // Use the enhanced viewer that includes skybox functionality
        return ForgeHtmlTemplates.GetEnhancedModelViewerHtml(encodedUrn, _accessToken);
    }

    private async Task<bool> EnsureTranslationReadyAsync(string urn, bool isPdf)
    {
        bool isTranslated = await _modelService.IsTranslationCompletedAsync(urn, _accessToken);
        if (isTranslated) return true;

        bool submitted = isPdf
            ? await _modelService.SubmitPdfForTranslationAsync(urn, _accessToken)
            : await _modelService.SubmitModelForTranslationAsync(urn, _accessToken);

        if (!submitted) return false;

        int retries = 30;
        for (int i = 0; i < retries; i++)
        {
            if (await _modelService.IsTranslationCompletedAsync(urn, _accessToken))
                return true;
            await Task.Delay(2000);
        }

        return false;
    }

    private string EncodeObjectIdToUrn(string objectId)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(objectId);
        return Convert.ToBase64String(bytes).TrimEnd('=');
    }
}