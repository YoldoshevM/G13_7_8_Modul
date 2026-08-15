using ToDoList.Application.Dtos;
using ToDoList.Domain.Entities;

namespace ToDoList.Application.Converters;

public static class ToDoItemConverter
{
    // Yordamchi metod: Kind = Unspecified/Local bo'lgan DateTime'ni
    // Postgres "timestamp with time zone" uchun Utc qilib beradi.
    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };
    }

    public static ToDoItem ToEntity(this ToDoItemCreateDto dto, long userId)
    {
        var now = DateTime.UtcNow;
        return new ToDoItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            DueDate = ToUtc(dto.DueDate),
            ReminderAt = ToUtc(dto.ReminderAt),
            UserId = userId,
            IsCompleted = false,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static void ApplyTo(this ToDoItemUpdateDto dto, ToDoItem entity)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.Priority = dto.Priority;
        entity.DueDate = ToUtc(dto.DueDate);
        entity.ReminderAt = ToUtc(dto.ReminderAt);

        if (dto.IsCompleted && !entity.IsCompleted)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }
        else if (!dto.IsCompleted && entity.IsCompleted)
        {
            entity.CompletedAt = null;
        }

        entity.IsCompleted = dto.IsCompleted;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // ToGetDto() o'zgarishsiz qoladi
    public static ToDoItemGetDto ToGetDto(this ToDoItem entity)
    {
        return new ToDoItemGetDto
        {
            ToDoItemId = entity.ToDoItemId,
            Title = entity.Title,
            Description = entity.Description,
            IsCompleted = entity.IsCompleted,
            IsDeleted = entity.IsDeleted,
            Priority = entity.Priority,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            DueDate = entity.DueDate,
            CompletedAt = entity.CompletedAt,
            DeletedAt = entity.DeletedAt,
            ReminderAt = entity.ReminderAt
        };
    }
}