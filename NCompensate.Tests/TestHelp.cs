using System;
using System.Threading.Tasks;

namespace NCompensate.Tests
{
    public static class TestHelp
    {
        public static async Task<bool> SuppressException(Func<Task> func)
        {
            try
            {
                await func.Invoke();
            }
            catch
            {
                return true;
            }

            return false;
        }
    }
}