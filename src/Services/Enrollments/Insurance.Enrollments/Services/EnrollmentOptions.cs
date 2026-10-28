namespace Insurance.Enrollments.Services;

/// <summary>"Enrollment" section of the configuration.</summary>
public class EnrollmentOptions
{
    // How long POST /api/enrollments waits for the saga before answering 202 Accepted with a Pending enrollment.
    public TimeSpan SagaTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan SagaPollInterval { get; set; } = TimeSpan.FromMilliseconds(50);
}
