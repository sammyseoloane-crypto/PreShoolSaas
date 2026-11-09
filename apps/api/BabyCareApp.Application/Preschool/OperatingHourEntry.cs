namespace BabyCareApp.Application.Preschool;

public record OperatingHourEntry(DayOfWeek DayOfWeek, TimeSpan? OpenTime, TimeSpan? CloseTime, bool IsClosed);
