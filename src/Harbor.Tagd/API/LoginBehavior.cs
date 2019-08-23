namespace Harbor.Tagd.API
{
	public enum LoginBehavior
	{
		/// <summary>
		/// When logging in, detect the version of harbor being used
		/// </summary>
		Probe = 0,
		/// <summary>
		/// When logging in, always use the pre-1.7 route
		/// </summary>
		ForcePre17 = 1,
		/// <summary>
		/// When logging in, always use the post-1.7 route
		/// </summary>
		ForcePost17 = 2,
	}
}
