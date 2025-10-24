namespace ClipBeam.Domain.Shared
{
    public class DomainException(string message) 
        : Exception(message);
}
