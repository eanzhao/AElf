﻿using AElf.ChainController;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Contracts.Genesis.Tests.ConfigureTestFramework", "AElf.Contracts.Genesis.Tests")]

namespace AElf.Contracts.Genesis.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            var assembly1 = typeof(IStateDictator).Assembly;
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            var assembly2 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
            var assembly3 = typeof(StateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly3).AsImplementedInterfaces();
            var assembly4 = typeof(BlockVaildationService).Assembly;
            builder.RegisterAssemblyTypes(assembly4).AsImplementedInterfaces();
            var assembly5 = typeof(Execution.ParallelTransactionExecutingService).Assembly;
            builder.RegisterAssemblyTypes(assembly5).AsImplementedInterfaces();
            var assembly6 = typeof(AElf.Node.Node).Assembly;
            builder.RegisterAssemblyTypes(assembly6).AsImplementedInterfaces();
            var assembly7 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly7).AsImplementedInterfaces();
            builder.RegisterType(typeof(Hash)).As(typeof(IHash));
            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new StateDictatorModule());
            
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
            smartContractRunnerFactory.AddRunner(0, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}