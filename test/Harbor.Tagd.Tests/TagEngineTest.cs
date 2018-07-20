using AutoFixture;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using Moq;
using System;
using Harbor.Tagd.API;

namespace Harbor.Tagd.Tests
{
	public class TagEngineTest
	{
		protected readonly IFixture _fixture;

		protected readonly Mock<IHarborClient> Harbor = new Mock<IHarborClient>();
		protected readonly Mock<Serilog.ILogger> Serilog = new Mock<Serilog.ILogger>();
		protected readonly Mock<IRuleProvider> Rules = new Mock<IRuleProvider>();
		protected readonly Mock<IResultNotifier> NotificationHandler = new Mock<IResultNotifier>();

		protected readonly ApplicationSettings Settings;

		private readonly Lazy<TagEngine> _sutImpl;
		protected TagEngine _sut
		{
			get
			{
				return _sutImpl.Value;
			}
		}

		public TagEngineTest()
		{
			_fixture = new Fixture();
			Settings = _fixture.Freeze<ApplicationSettings>();
			_sutImpl = new Lazy<TagEngine>(() => new TagEngine(Harbor.Object, Settings, Serilog.Object, Rules.Object, NotificationHandler.Object));
		}
	}
}
