namespace Xenoh.Application.Common.Exceptions;

public sealed class SupplementConflictException(string message) : InvalidOperationException(message);
