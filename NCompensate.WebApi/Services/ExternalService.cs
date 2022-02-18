using System.Threading.Tasks;

namespace NCompensate.WebApi.Services
{
    public static class ExternalService
    {
        public static async Task<T> Post<T>(string route, T data)
        {
            return await Task.FromResult<T>(default);
        } 
        
        public static Task Remove(string route, int id)
        {
            return Task.CompletedTask;
        } 
    }
}