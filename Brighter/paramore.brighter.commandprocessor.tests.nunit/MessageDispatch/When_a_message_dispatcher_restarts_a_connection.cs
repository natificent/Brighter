#region Licence
/* The MIT License (MIT)
Copyright � 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the �Software�), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED �AS IS�, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System.Collections.Generic;
using System.Threading.Tasks;
using nUnitShouldAdapter;
using NUnit.Framework;
using NUnit.Specifications;
using paramore.brighter.commandprocessor.Logging;
using paramore.brighter.commandprocessor.tests.nunit.CommandProcessors.TestDoubles;
using paramore.brighter.commandprocessor.tests.nunit.MessageDispatch.TestDoubles;
using paramore.brighter.serviceactivator;
using paramore.brighter.serviceactivator.TestHelpers;

namespace paramore.brighter.commandprocessor.tests.nunit.MessageDispatch
{
    [Ignore("Breaks dotnet test runner")]
    [Subject(typeof(Dispatcher))]
    public class When_A_Message_Dispatcher_Restarts_A_Connection : ContextSpecification
    {
        private static Dispatcher s_dispatcher;
        private static FakeChannel s_channel;
        private static IAmACommandProcessor s_commandProcessor;
        private static Connection s_connection;

        private Establish _context = () =>
        {
            s_channel = new FakeChannel();
            s_commandProcessor = new SpyCommandProcessor();

            var logger = LogProvider.For<Dispatcher>();

            var messageMapperRegistry = new MessageMapperRegistry(new SimpleMessageMapperFactory(() => new MyEventMessageMapper()));
            messageMapperRegistry.Register<MyEvent, MyEventMessageMapper>();

            s_connection = new Connection(name: new ConnectionName("test"), dataType: typeof(MyEvent), noOfPerformers: 1, timeoutInMilliseconds: 1000, channelFactory: new InMemoryChannelFactory(s_channel), channelName: new ChannelName("fakeChannel"), routingKey: "fakekey");
            s_dispatcher = new Dispatcher(s_commandProcessor, messageMapperRegistry, new List<Connection> { s_connection }, logger);

            var @event = new MyEvent();
            var message = new MyEventMessageMapper().MapToMessage(@event);
            s_channel.Add(message);

            s_dispatcher.State.ShouldEqual(DispatcherState.DS_AWAITING);
            s_dispatcher.Receive();
            Task.Delay(1000).Wait();
            s_dispatcher.Shut(s_connection);
        };


        private Because _of = () =>
        {
            s_dispatcher.Open(s_connection);

            var @event = new MyEvent();
            var message = new MyEventMessageMapper().MapToMessage(@event);
            s_channel.Add(message);

            s_dispatcher.End().Wait();
        };

        private It _should_have_consumed_the_messages_in_the_event_channel = () => s_channel.Length.ShouldEqual(0);
        private It _should_have_a_stopped_state = () => s_dispatcher.State.ShouldEqual(DispatcherState.DS_STOPPED);
    }
}