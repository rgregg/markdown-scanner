using ApiDocs.Validation.Error;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Logger
{
    internal class MulticastOutputDelegate : IOutputDelegate
    {
        private IOutputDelegate[] delegates;

        public MulticastOutputDelegate(IEnumerable<IOutputDelegate> delegates)
        {
            this.delegates = delegates.ToArray();
        }


        public Task ReportTestCompleteAsync(TestEngine.DocTest test, ValidationError[] messages)
        {
            // RecordUndocumentedProperties(messages);

            return PerformActionAsync(async output =>
            {
                await output.ReportTestCompleteAsync(test, messages);
            });
        }

        public Task StartTestAsync(TestEngine.DocTest test)
        {
            return PerformActionAsync(async output =>
            {
                await output.StartTestAsync(test);
            });
        }

        public Task CloseAsync(TestEngine engine)
        {
            return PerformActionAsync(async output =>
            {
                await output.CloseAsync(engine);
            });
        }


        private async Task PerformActionAsync(Func<IOutputDelegate, Task> action)
        {
            await ForEachAsync(delegates, 4, action);
        }

        private static Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }


    }
}
