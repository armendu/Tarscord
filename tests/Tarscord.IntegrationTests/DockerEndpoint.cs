namespace Tarscord.IntegrationTests;

/// <summary>
/// Points Testcontainers at whichever Docker socket this machine actually has.
/// </summary>
/// <remarks>
/// Testcontainers reads DOCKER_HOST before ~/.testcontainers.properties, and a developer machine
/// running Rancher Desktop or Colima has no /var/run/docker.sock at all. Probing here keeps the
/// socket path out of the repo and out of the developer's global configuration.
/// </remarks>
internal static class DockerEndpoint
{
    private const string DockerHostVariable = "DOCKER_HOST";

    public static void EnsureConfigured()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DockerHostVariable)))
            return;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] candidates =
        [
            "/var/run/docker.sock",
            Path.Combine(home, ".rd", "docker.sock"),
            Path.Combine(home, ".docker", "run", "docker.sock"),
            Path.Combine(home, ".colima", "default", "docker.sock")
        ];

        string? socket = candidates.FirstOrDefault(File.Exists);

        if (socket is null)
        {
            throw new InvalidOperationException(
                $"No Docker socket found. Start Docker or Rancher Desktop, or set {DockerHostVariable}. " +
                $"Looked in: {string.Join(", ", candidates)}");
        }

        Environment.SetEnvironmentVariable(DockerHostVariable, $"unix://{socket}");
    }
}
