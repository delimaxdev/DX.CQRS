using Autofac;
using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Contracts.ReadModels;
using DX.Cqrs.Common;
using DX.Cqrs.Domain;
using DX.Cqrs.EventStore;
using DX.Cqrs.Queries;
using DX.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xbehave;

namespace Queries {
    public class QueriesFeature : Feature {
        [Scenario]
        internal void Test(TestServices services, EmployeesQuery q, Ref<Employee> employee, IReadOnlyCollection<EmployeeRM> result) {
            GIVEN["a query"] = () => {
                services = new TestServices(b => b
                    .Services(c => c.RegisterType<EmployeesQuery>().AsImplementedInterfaces())
                    .UseDefaults());

                q = new EmployeesQuery(services.GetRequiredService<IEventStore>());

                var e = new Employee(ID.NewID(), "Test employee");
                services.Save(e);
                employee = e;
                
                EmployeeCodes ec = new EmployeeCodes();
                ec.AssignCode(employee, "1234");
                services.Save(ec);
            };
            
            WHEN["running the query"] = () => result = q.Run(services.GetRequiredService<IContext>());

            THEN["the result is correct"] = () => result.Should().BeEquivalentTo(new EmployeeRM(employee) {
                Code = "1234",
                Name = "Test employee"
            });

            WHEN["running the query via container"] = () => result = services.RunQuery(new GetEmployees()).Result;
            THEN["it returns the result"] = () => result.Should().NotBeEmpty();
        }

        internal class EmployeesQuery : CollectionQuery<EmployeeRM>, IQuery<GetEmployees, IReadOnlyCollection<EmployeeRM>> {
            public EmployeesQuery(IEventStore store)
                : base(store) {

                On<Employee.Created>((state, e, id) => state.Add(new EmployeeRM(id.ToRef<Employee>()) { Name = e.Name }));
                On<EmployeeCodes.CodeAssigned>((state, e, id) => state.Update(e.Employee, rm => rm.Code = e.Code));
            }

            public new IReadOnlyCollection<EmployeeRM> Run(IContext c)
                => base.Run(new CollectionQueryState<EmployeeRM>(), c).Result.Result();

            public Task<IReadOnlyCollection<EmployeeRM>> Run(GetEmployees criteria, IContext context) 
                => Task.FromResult(Run(context));
        }

        internal class GetEmployees : ICriteria<IReadOnlyCollection<EmployeeRM>> { }

        internal class EmployeeRM : ReadModel<Employee> {
            public EmployeeRM(Ref<Employee> employee) : base(employee) {
                Employee = employee;
            }

            public Ref<Employee> Employee { get; }
            public string Name { get; set; }
            public string Code { get; set; }
        }



        internal class Employee : TestAggregate {
            public Employee(ID id, string name) : base(id) {
                M.ApplyChange(new Created { Name = name });
            }

            public class Created : IEvent {
                public string Name { get; set; }
            }
        }

        internal class EmployeeCodes : TestAggregate {

            public void AssignCode(Ref<Employee> e, string code)
                => M.ApplyChange(new CodeAssigned { Employee = e, Code = code });

            public class CodeAssigned : IEvent {
                public Ref<Employee> Employee { get; set; }

                public string Code { get; set; }
            }
        }
    }
}