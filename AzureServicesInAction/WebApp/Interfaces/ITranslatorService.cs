using System.Transactions;

namespace WebApp.Interfaces
{
    public interface ITranslatorService
    {
        Task<string> TranslateTextAsync(string inputText, string targetLanguage);
    }
}
