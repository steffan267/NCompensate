using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NCompensate.WebApi.Services;

namespace NCompensate.WebApi.Controllers
{
    public class ExampleController : Controller
    {
        /// <summary>
        /// Attempts to add a course - and optionally adds 
        /// </summary>
        public async Task Create(Course model)
        {
            await Transaction.New()
                .Add(
                    async () => await ExternalService.Post("/courses", model), 
                    async (d) => await ExternalService.Remove("/courses", d.Id))
                .AddConditionally(
                    model.AllocateRoom,
                    async () => await ExternalService.Post("/courses/allocate", model.Id))
                .InvokeWithCompensationAsync();
        }
        
        public async Task Create1(Course model)
        {
            ICompensable createCourse = 
                new Compensable<Course>(async () => await ExternalService.Post("/courses", model))
                    .WithCompensation(async (d) => await ExternalService.Remove("/courses", d.Id));

            ICompensable allocateRoom =
                new Compensable<int>(async () => await ExternalService.Post("/courses/allocate", model.Id));

            await Transaction.New()
                .Add(createCourse)
                .AddConditionally(
                    model.AllocateRoom,
                    allocateRoom)
                .InvokeWithCompensationAsync();
        }
        
        public async Task Create2(Course model)
        {
            var createCourse =
                new Compensable<Course>(
                    async () => await ExternalService.Post("/courses", model),
                    async (d) => await ExternalService.Remove("/courses", d.Id));

            ICompensable allocateRoom =
                new Compensable<int>(async () => await ExternalService.Post("/courses/allocate", model.Id));

            await Transaction.New()
                .Add(createCourse)
                .AddConditionally(
                    model.AllocateRoom,
                    allocateRoom)
                .InvokeWithCompensationAsync();
        }
    }

    public class Course
    {
        public int Id { get; set; }
        public bool AllocateRoom { get; set; }
    }
}