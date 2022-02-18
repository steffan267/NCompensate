using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NCompensate.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }
        
        [HttpGet("/unordered")]
        public async Task<List<int>> GetUnordered()
        {
            var random = new Random();
            var list = new ConcurrentQueue<int>();
            var function = Enumerable.Range(0, 100).Select(i => new Func<Task<int>>(async () =>
            {
                await Task.Delay(random.Next(i, 100));
                list.Enqueue(i);
                return i;
            })).ToList();


            var pipe = Transaction.New(new TransactionConfiguration(){ InvokeOrder = InvocationOrder.Ordered });
            foreach (var f in function)
            {
                pipe.Add(f);
            }

            await pipe.InvokeWithCompensationAsync();
            return list.ToList();
        }

        [HttpGet("/ordered")]
        public async Task<List<int>> GetOrdered()
        {
            var random = new Random();
            var list = new ConcurrentQueue<int>();
            var function = Enumerable.Range(0, 100).Select(i => new Func<Task<int>>(async () =>
            {
                await Task.Delay(random.Next(i, 100));
                list.Enqueue(i);
                return i;
            })).ToList();


            var pipe = Transaction.New();
            foreach (var f in function)
            {
                pipe.Add(f);
            }

            await pipe.InvokeWithCompensationAsync();
            return list.ToList();
        }
    }
}