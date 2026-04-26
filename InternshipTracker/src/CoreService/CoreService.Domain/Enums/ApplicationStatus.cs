namespace CoreService.Domain.Enums;

public enum ApplicationStatus
{
    Pending = 0,
    Accepted = 1,
    Enrolling = 2,
    Enrolled = 3,
    Rejected = 4,
    EnrolledNotificationFault = 5
}