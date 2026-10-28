namespace Insurance.Enrollments.Contracts;

public enum EnrollmentStatus
{
    Active,
    Cancelled,
    Expired,
    Pending,   // the enrollment saga is still setting it up (customers normally never see this)
    Failed     // the saga could not complete; hidden from customers
}
