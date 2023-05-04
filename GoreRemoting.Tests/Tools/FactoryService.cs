namespace GoreRemoting.Tests.Tools
{
	using GoreRemoting;

	public class FactoryService : IFactoryService
	{
		public ITestService GetTestService()
		{
			return new TestService();
		}
	}
}