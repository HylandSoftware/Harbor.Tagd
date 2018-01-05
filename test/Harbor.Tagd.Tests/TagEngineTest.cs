using AutoFixture;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using Harbormaster;
using Moq;

namespace Harbor.Tagd.Tests
{
	public class TagEngineTest
	{
		protected readonly IFixture _fixture;

		protected readonly Mock<IHarborClient> Harbor = new Mock<IHarborClient>();
		protected readonly Mock<Serilog.ILogger> Serilog = new Mock<Serilog.ILogger>();
		protected readonly Mock<IRuleProvider> Rules = new Mock<IRuleProvider>();
		protected readonly Mock<IResultNotifier> NotificationHandler = new Mock<IResultNotifier>();

		protected readonly HarborSettings Settings;
		protected readonly TagEngine _sut;

		public TagEngineTest()
		{
			_fixture = new Fixture();
			Settings = _fixture.Freeze<HarborSettings>();
			_sut = new TagEngine(Harbor.Object, Settings, Serilog.Object, Rules.Object, NotificationHandler.Object);
		}
	}
}
