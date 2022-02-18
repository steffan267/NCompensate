using System.Threading.Tasks;

namespace NCompensate
{
    public interface ICompensable
    {
        Task InvokeAsync();
        Task CompensateAsync();
    }
}

