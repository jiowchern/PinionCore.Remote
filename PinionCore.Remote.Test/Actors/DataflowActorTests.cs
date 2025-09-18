using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote.Actors;

namespace PinionCore.Remote.Test.Actors
{
    public class DataflowActorTests
    {
        [Test]
        public async Task SendAsync_ShouldProcessMessagesSequentially()
        {
            var processed = new List<int>();

            using (var actor = new DataflowActor<int>(async (message, token) =>
            {
                await Task.Delay(1, token);
                processed.Add(message);
            }))
            {
                var sendTasks = Enumerable.Range(0, 20)
                    .Select(message => actor.SendAsync(message))
                    .ToArray();

                var results = await Task.WhenAll(sendTasks);
                Assert.That(results.All(result => result), Is.True);

                actor.Complete();
                await actor.Completion;
            }

            Assert.That(processed, Is.EqualTo(Enumerable.Range(0, 20)));
        }

        [Test]
        public async Task Post_ShouldReturnFalseAfterComplete()
        {
            using (var actor = new DataflowActor<int>(_ => { }))
            {
                actor.Complete();
                await actor.Completion;

                var result = actor.Post(1);

                Assert.That(result, Is.False);
            }
        }
    }
}
