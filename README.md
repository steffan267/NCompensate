# NCompensate
A .NET library to easily create Compensating Transactions 

https://docs.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction

Below are three examples on how to achieve the same behavior with NCompensate:

Example 1 - inline call backs:
```csharp
await Transaction.New()
    .Add(
        async () => await ExternalService.Post("/courses", model), 
        async (d) => await ExternalService.Remove("/courses", d.Id))
    .AddConditionally(
        model.AllocateRoom,
        async () => await ExternalService.Post("/courses/allocate", model.Id))
    .InvokeWithCompensationAsync();
```

Example 2 - Injectable ICompensables:
```csharp
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
```

Example 3 - Hybrid:
```csharp
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
```
