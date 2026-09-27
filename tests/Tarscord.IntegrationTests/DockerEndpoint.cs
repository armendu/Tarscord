namespace Tarscord.IntegrationTests;

/// <summary>Points Testcontainers at whichever Docker socket this machine has.</summary>
/// <remarks>Rancher Desktop and Colima have no /var/run/docker.sock; DOCKER_HOST wins.</remarks>
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
