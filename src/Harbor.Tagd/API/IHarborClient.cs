using Harbor.Tagd.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harbor.Tagd.API
{
	public interface IHarborClient
	{
		string SessionToken { get; }

		Task Login(string user, string password);
		Task Logout();

		Task<IEnumerable<Project>> GetAllProjects();
		Task<IEnumerable<Repository>> GetRepositories(int project);
		Task<IEnumerable<Tag>> GetTags(string name);

		Task DeleteTag(string repository, string name);
	}
}
